/*
 * Copyright 2023-2024 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// FILE:Assets/Examples/SimWorld/SimWorldTerrain.cs ROLE:face-culled voxel chunk meshes over WorldStore DEPS:RagonSimWorldView;WorldStore;UnityEngine
// R:_view.Store,_view.CurrentRegion W:_view.RegionChanged/_view.BlockChanged subscribers ; per-chunk Mesh
// API:None (scene component)
// WHY:one Mesh per 32x32x48 chunk with a world-space transform; only faces adjacent to non-solid cells are emitted, so no greedy meshing or textures are needed
// WHY:rebuilds are event-driven per edited chunk (plus its x/z neighbour when the edit is on a chunk seam); reverts invalidate everything because the store does not expose which cell it rolled back
// WHY:chunks are meshed as they stream in, not after IsComplete, so terrain fills progressively and a migration visibly re-streams

using System;
using System.Collections.Generic;
using Ragon.Client.Unity;
using Ragon.Framework;
using Ragon.Framework.Client;
using UnityEngine;
using UnityEngine.Rendering;

namespace Examples.SimWorld
{
  /// <summary>
  /// Renders the streamed voxel regions with one mesh per chunk. Every solid cell emits a quad only
  /// for a face that touches air (<see cref="WorldStore.IsSolidCell"/> is the neighbour test), which
  /// keeps a 128x128x48 region at a few tens of thousands of triangles without greedy meshing.
  ///
  /// <para>Material slots follow the content block table, so stone and wood (and any future block)
  /// get their authored colour without a texture. The edited chunk is the only one rebuilt on a
  /// block change; a seam edit also rebuilds the neighbour whose boundary face changed.</para>
  /// </summary>
  public sealed class SimWorldTerrain : MonoBehaviour
  {
    private const int BuildsPerFrame = 2;

    private static readonly Vector3[][] Faces =
    {
      // +X
      new[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1) },
      // -X
      new[] { new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(0, 0, 0) },
      // +Y
      new[] { new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(0, 1, 0) },
      // -Y
      new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1) },
      // +Z
      new[] { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1) },
      // -Z
      new[] { new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0) },
    };

    private static readonly Vector3Int[] FaceNeighbours =
    {
      new Vector3Int(1, 0, 0),
      new Vector3Int(-1, 0, 0),
      new Vector3Int(0, 1, 0),
      new Vector3Int(0, -1, 0),
      new Vector3Int(0, 0, 1),
      new Vector3Int(0, 0, -1),
    };

    [SerializeField] private RagonSimWorldView _view;

    private ChunkVisual[] _chunks = Array.Empty<ChunkVisual>();
    private bool[] _dirty = Array.Empty<bool>();
    private Material[] _materials = Array.Empty<Material>();
    private readonly Dictionary<ushort, int> _slotByBlock = new Dictionary<ushort, int>();

    private RagonWorldRegion _region;
    private int _chunksX;
    private int _chunksZ;

    private void OnEnable()
    {
      if (_view == null)
        return;

      _view.BlockChanged += OnBlockChanged;
      _view.StoreReverted += OnStoreReverted;
    }

    private void OnDisable()
    {
      if (_view == null)
        return;

      _view.BlockChanged -= OnBlockChanged;
      _view.StoreReverted -= OnStoreReverted;
    }

    private void Start()
    {
      BuildMaterials();
    }

    private void OnDestroy()
    {
      DestroyAllChunks();

      for (var i = 0; i < _materials.Length; i++)
      {
        if (_materials[i] != null)
          Destroy(_materials[i]);
      }
    }

    private void Update()
    {
      var view = _view;
      var store = view != null ? view.Store : null;
      var region = view != null ? view.CurrentRegion : null;

      if (store == null || region == null)
      {
        DestroyAllChunks();
        _region = null;
        return;
      }

      if (!ReferenceEquals(region, _region) || _chunks.Length != store.ChunkCount)
        ResetForRegion(store, region);

      if (_materials.Length == 0)
        return;

      MarkNewlyStreamed(store);
      RebuildDirty(store);
    }

    private void ResetForRegion(WorldStore store, RagonWorldRegion region)
    {
      DestroyAllChunks();

      _region = region;
      _chunksX = store.ChunksX;
      _chunksZ = store.ChunksZ;
      _chunks = new ChunkVisual[store.ChunkCount];
      _dirty = new bool[store.ChunkCount];

      for (var i = 0; i < _chunks.Length; i++)
        _chunks[i] = new ChunkVisual();
    }

    private void MarkNewlyStreamed(WorldStore store)
    {
      for (var i = 0; i < _chunks.Length; i++)
      {
        if (_chunks[i].Built)
          continue;

        if (store.TryGetChunk(i, out _))
          _dirty[i] = true;
      }
    }

    private void RebuildDirty(WorldStore store)
    {
      var built = 0;

      for (var i = 0; i < _dirty.Length && built < BuildsPerFrame; i++)
      {
        if (!_dirty[i])
          continue;

        _dirty[i] = false;

        if (BuildChunk(store, i))
          built++;
      }
    }

    private bool BuildChunk(WorldStore store, int index)
    {
      if (!store.TryGetChunk(index, out var chunk))
        return false;

      var chunkX = index % _chunksX;
      var chunkZ = index / _chunksX;
      var baseX = chunkX * WorldChunk.Width;
      var baseZ = chunkZ * WorldChunk.Depth;

      var vertices = new List<Vector3>(4096);
      var triangles = new List<int>[_materials.Length];
      for (var i = 0; i < triangles.Length; i++)
        triangles[i] = new List<int>(2048);

      for (var x = 0; x < WorldChunk.Width; x++)
      {
        for (var z = 0; z < WorldChunk.Depth; z++)
        {
          var top = TopSolid(chunk, x, z);

          for (var y = 0; y <= top; y++)
          {
            var block = chunk.Get(x, y, z);
            if (block == 0 || !_slotByBlock.TryGetValue(block, out var slot))
              continue;

            for (var face = 0; face < Faces.Length; face++)
            {
              var neighbour = FaceNeighbours[face];
              if (store.IsSolidCell(baseX + x + neighbour.x, y + neighbour.y, baseZ + z + neighbour.z))
                continue;

              AddFace(vertices, triangles[slot], Faces[face], x, y, z);
            }
          }
        }
      }

      var visual = _chunks[index];
      if (visual.GameObject == null)
        CreateVisual(visual, chunkX, chunkZ);

      var mesh = visual.Mesh;
      mesh.Clear();
      mesh.indexFormat = IndexFormat.UInt32;
      mesh.SetVertices(vertices);
      mesh.subMeshCount = _materials.Length;

      for (var i = 0; i < triangles.Length; i++)
        mesh.SetTriangles(triangles[i], i);

      // Vertices are duplicated per face, so RecalculateNormals produces flat faceted lighting.
      mesh.RecalculateNormals();
      mesh.RecalculateBounds();

      visual.Filter.sharedMesh = mesh;
      visual.Renderer.sharedMaterials = _materials;
      visual.Built = true;
      return true;
    }

    private void CreateVisual(ChunkVisual visual, int chunkX, int chunkZ)
    {
      var go = new GameObject($"Chunk {chunkX},{chunkZ}");
      go.transform.SetParent(transform, false);
      go.transform.position = new Vector3(
        _region.OriginX + chunkX * WorldChunk.Width,
        0f,
        _region.OriginZ + chunkZ * WorldChunk.Depth);

      var filter = go.AddComponent<MeshFilter>();
      var renderer = go.AddComponent<MeshRenderer>();

      visual.GameObject = go;
      visual.Mesh = new Mesh { name = $"SimWorld Chunk {chunkX},{chunkZ}" };
      visual.Filter = filter;
      visual.Renderer = renderer;
    }

    private static void AddFace(List<Vector3> vertices, List<int> triangles, Vector3[] corners, int x, int y, int z)
    {
      var start = vertices.Count;

      for (var i = 0; i < 4; i++)
      {
        var corner = corners[i];
        vertices.Add(new Vector3(x + corner.x, y + corner.y, z + corner.z));
      }

      triangles.Add(start);
      triangles.Add(start + 1);
      triangles.Add(start + 2);
      triangles.Add(start);
      triangles.Add(start + 2);
      triangles.Add(start + 3);
    }

    private static int TopSolid(WorldChunk chunk, int x, int z)
    {
      for (var y = WorldChunk.Height - 1; y >= 0; y--)
      {
        if (chunk.Get(x, y, z) != 0)
          return y;
      }

      return -1;
    }

    private void OnBlockChanged(int worldX, int y, int worldZ)
    {
      var region = _region;
      if (region == null)
        return;

      var localX = worldX - region.OriginX;
      var localZ = worldZ - region.OriginZ;

      if (localX < 0 || localX >= region.Width || localZ < 0 || localZ >= region.Depth)
        return;

      MarkDirty(localX / WorldChunk.Width, localZ / WorldChunk.Depth);

      // The neighbouring chunk culls the shared face using this cell, so a seam edit is two chunks.
      if (localX % WorldChunk.Width == 0 && localX > 0)
        MarkDirty((localX - 1) / WorldChunk.Width, localZ / WorldChunk.Depth);
      else if (localX % WorldChunk.Width == WorldChunk.Width - 1 && localX < region.Width - 1)
        MarkDirty((localX + 1) / WorldChunk.Width, localZ / WorldChunk.Depth);

      if (localZ % WorldChunk.Depth == 0 && localZ > 0)
        MarkDirty(localX / WorldChunk.Width, (localZ - 1) / WorldChunk.Depth);
      else if (localZ % WorldChunk.Depth == WorldChunk.Depth - 1 && localZ < region.Depth - 1)
        MarkDirty(localX / WorldChunk.Width, (localZ + 1) / WorldChunk.Depth);
    }

    private void MarkDirty(int chunkX, int chunkZ)
    {
      if (chunkX < 0 || chunkX >= _chunksX || chunkZ < 0 || chunkZ >= _chunksZ)
        return;

      _dirty[chunkZ * _chunksX + chunkX] = true;
    }

    private void OnStoreReverted()
    {
      for (var i = 0; i < _dirty.Length; i++)
        _dirty[i] = true;
    }

    private void DestroyAllChunks()
    {
      for (var i = 0; i < _chunks.Length; i++)
      {
        var visual = _chunks[i];
        if (visual == null)
          continue;

        if (visual.GameObject != null)
          Destroy(visual.GameObject);

        if (visual.Mesh != null)
          Destroy(visual.Mesh);
      }

      _chunks = Array.Empty<ChunkVisual>();
      _dirty = Array.Empty<bool>();
    }

    private void BuildMaterials()
    {
      if (_view == null || _view.Config == null)
        return;

      GameContent content;
      try
      {
        content = _view.Config.Content;
      }
      catch (Exception exception)
      {
        Debug.LogError($"SimWorldTerrain: cannot parse the world game content: {exception.Message}", this);
        return;
      }

      if (content == null)
        return;

      var shader = Shader.Find("Standard");
      if (shader == null)
      {
        Debug.LogError("SimWorldTerrain: the built-in Standard shader is missing", this);
        return;
      }

      _slotByBlock.Clear();
      _materials = new Material[content.Blocks.Length];

      for (var i = 0; i < content.Blocks.Length; i++)
      {
        var block = content.Blocks[i];
        if (block.Id == 0 || _slotByBlock.ContainsKey(block.Id))
          continue;

        // BlockDef.Color is authored as 0xRRGGBBAA.
        var color = new Color(
          ((block.Color >> 24) & 0xFF) / 255f,
          ((block.Color >> 16) & 0xFF) / 255f,
          ((block.Color >> 8) & 0xFF) / 255f,
          (block.Color & 0xFF) / 255f);

        var material = new Material(shader)
        {
          name = $"SimWorld {block.Name}",
          color = color,
        };
        material.SetFloat("_Glossiness", 0.05f);

        _slotByBlock[block.Id] = i;
        _materials[i] = material;
      }
    }

    private sealed class ChunkVisual
    {
      public GameObject GameObject;
      public Mesh Mesh;
      public MeshFilter Filter;
      public MeshRenderer Renderer;
      public bool Built;
    }
  }
}
