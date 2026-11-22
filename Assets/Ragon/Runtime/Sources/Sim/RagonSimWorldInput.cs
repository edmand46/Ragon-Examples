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

// FILE:Runtime/Sources/Sim/RagonSimWorldInput.cs ROLE:minimal legacy-input world demo source (move, look, build/remove, highlight) DEPS:RagonSimWorldView;Ragon.Framework;UnityEngine.Input
// R:_view W:_view.Framework.PendingInput,_view.RequestEditAt,_highlight
// API:View ; Highlight ; Yaw ; Pitch ; BlockId
// WHY:DefaultExecutionOrder(-1450) runs after RagonNetwork(-1500) and before RagonSimWorldView(-1400), so this frame's intent reaches the tick
// WHY:aiming marches the WorldStore heightmap instead of Physics.Raycast; the terrain is a mesh over streamed data and has no colliders to depend on
// WHY:the click actions are press-only; the server accepts at most a few edits per second and held buttons would queue rejected edits

using Ragon.Framework;
using Ragon.Framework.Client;
using UnityEngine;

namespace Ragon.Client.Unity
{
  /// <summary>
  /// Dependency-free starter input for the world view: legacy <c>Input</c> only, no Input System
  /// package. WASD drive movement, mouse-look feeds yaw/pitch (cursor locked by default), left mouse
  /// places the configured block on the aimed face, right mouse removes the aimed block, and an
  /// optional transform marks the target cell.
  ///
  /// <para>Aiming is analytic: a voxel DDA marches the camera-forward ray through
  /// <see cref="WorldStore"/> sampling <see cref="WorldStore.IsSolidCell"/>. The face-adjacent air
  /// cell is the placement target, the solid cell itself is the removal target. No collider and no
  /// <c>Physics.Raycast</c> is involved, so the demo needs no MeshCollider bake.</para>
  /// </summary>
  [DefaultExecutionOrder(-1450)]
  [DisallowMultipleComponent]
  public sealed class RagonSimWorldInput : MonoBehaviour
  {
    [SerializeField] private RagonSimWorldView _view;
    [SerializeField] private Transform _highlight;
    [SerializeField] private ushort _blockId;
    [SerializeField] private float _sensitivity = 2.5f;
    [SerializeField] private float _reach = 12f;
    [SerializeField] private bool _lockCursor = true;

    private float _yaw;
    private float _pitch;

    public RagonSimWorldView View
    {
      get => _view;
      set => _view = value;
    }

    /// <summary>Optional marker moved to the cell a click would affect.</summary>
    public Transform Highlight
    {
      get => _highlight;
      set => _highlight = value;
    }

    /// <summary>Current yaw in degrees (framework convention: +yaw turns toward +x).</summary>
    public float Yaw => _yaw;

    /// <summary>Current pitch in degrees (positive looks up).</summary>
    public float Pitch => _pitch;

    /// <summary>Block placed by left mouse; 0 falls back to the config's default block id.</summary>
    public ushort BlockId
    {
      get => _blockId;
      set => _blockId = value;
    }

    private void OnEnable()
    {
      if (_lockCursor)
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnDisable()
    {
      if (_lockCursor)
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
      var framework = _view != null ? _view.Framework : null;
      if (framework == null)
        return;

      var locked = Cursor.lockState == CursorLockMode.Locked;
      if (_lockCursor && !locked && Input.GetMouseButtonDown(0))
      {
        // The first click only captures the cursor; otherwise every alt-tab would build a block.
        Cursor.lockState = CursorLockMode.Locked;
        return;
      }

      if (Input.GetKeyDown(KeyCode.Escape) && _lockCursor)
        Cursor.lockState = CursorLockMode.None;

      if (!_lockCursor || locked)
      {
        _yaw += Input.GetAxisRaw("Mouse X") * _sensitivity;
        // Mouse Y is positive moving up; framework pitch is positive looking up.
        _pitch += Input.GetAxisRaw("Mouse Y") * _sensitivity;
        _pitch = Mathf.Clamp(_pitch, -80f, 80f);
      }

      ushort buttons = 0;
      if (Input.GetKey(KeyCode.Space))
        buttons |= InputButtons.Jump;
      if (Input.GetKey(KeyCode.LeftShift))
        buttons |= InputButtons.Sprint;

      framework.PendingInput = new FrameworkInput
      {
        MoveX = InputComposer.QuantizeMove(Input.GetAxisRaw("Horizontal")),
        MoveZ = InputComposer.QuantizeMove(Input.GetAxisRaw("Vertical")),
        Yaw = _yaw * Mathf.Deg2Rad,
        Pitch = _pitch * Mathf.Deg2Rad,
        Buttons = buttons,
      };

      UpdateAim();
    }

    private void UpdateAim()
    {
      var store = _view.Store;
      var region = _view.CurrentRegion;
      if (store == null || region == null)
      {
        HideHighlight();
        return;
      }

      if (!_view.TryGetLocalPose(out var position, out _, out _))
      {
        HideHighlight();
        return;
      }

      var height = 1.8;
      var content = _view.Config != null ? _view.Config.Content : null;
      if (content != null)
        height = content.Player.Height;

      // Matches the server's eye convention (eye height is 90% of the capsule height).
      var eye = position + Vector3.up * (float)(height * 0.9);
      var aim = Raymarch(store, region, eye, LookDirection(), _reach);

      if (!aim.Hit)
      {
        HideHighlight();
        return;
      }

      var targetX = aim.HasPlace ? aim.PlaceX : aim.X;
      var targetY = aim.HasPlace ? aim.PlaceY : aim.Y;
      var targetZ = aim.HasPlace ? aim.PlaceZ : aim.Z;

      if (_highlight != null)
      {
        _highlight.position = new Vector3(
          region.OriginX + targetX + 0.5f,
          targetY + 0.5f,
          region.OriginZ + targetZ + 0.5f);
        _highlight.gameObject.SetActive(true);
      }

      if (_lockCursor && Cursor.lockState != CursorLockMode.Locked)
        return;

      var block = ResolveBlockId();
      if (Input.GetMouseButtonDown(0) && aim.HasPlace)
      {
        _view.RequestEditAt(
          region.OriginX + aim.PlaceX,
          aim.PlaceY,
          region.OriginZ + aim.PlaceZ,
          block);
      }

      if (Input.GetMouseButtonDown(1))
      {
        _view.RequestEditAt(
          region.OriginX + aim.X,
          aim.Y,
          region.OriginZ + aim.Z,
          0,
          true);
      }
    }

    private ushort ResolveBlockId()
    {
      if (_blockId != 0)
        return _blockId;

      return _view.Config != null ? _view.Config.DefaultBlockId : (ushort)1;
    }

    private void HideHighlight()
    {
      if (_highlight != null && _highlight.gameObject.activeSelf)
        _highlight.gameObject.SetActive(false);
    }

    private Vector3 LookDirection()
    {
      var yaw = _yaw * Mathf.Deg2Rad;
      var pitch = _pitch * Mathf.Deg2Rad;
      var cosPitch = Mathf.Cos(pitch);

      return new Vector3(
        Mathf.Sin(yaw) * cosPitch,
        Mathf.Sin(pitch),
        Mathf.Cos(yaw) * cosPitch);
    }

    /// <summary>
    /// Voxel DDA over the current region. Returns the first solid cell and the air cell the ray
    /// crossed immediately before it (the face the player would build against).
    /// </summary>
    private static VoxelAim Raymarch(
      WorldStore store,
      RagonWorldRegion region,
      Vector3 worldOrigin,
      Vector3 direction,
      float maxDistance)
    {
      if (direction.sqrMagnitude < 1e-6f)
        return default;

      direction.Normalize();

      var originX = worldOrigin.x - region.OriginX;
      var originY = worldOrigin.y;
      var originZ = worldOrigin.z - region.OriginZ;

      var x = Mathf.FloorToInt(originX);
      var y = Mathf.FloorToInt(originY);
      var z = Mathf.FloorToInt(originZ);

      var stepX = direction.x >= 0f ? 1 : -1;
      var stepY = direction.y >= 0f ? 1 : -1;
      var stepZ = direction.z >= 0f ? 1 : -1;

      var deltaX = Mathf.Abs(direction.x) > 1e-6f ? Mathf.Abs(1f / direction.x) : float.PositiveInfinity;
      var deltaY = Mathf.Abs(direction.y) > 1e-6f ? Mathf.Abs(1f / direction.y) : float.PositiveInfinity;
      var deltaZ = Mathf.Abs(direction.z) > 1e-6f ? Mathf.Abs(1f / direction.z) : float.PositiveInfinity;

      var nextX = float.IsInfinity(deltaX)
        ? float.PositiveInfinity
        : (stepX > 0 ? (x + 1 - originX) : (originX - x)) * deltaX;
      var nextY = float.IsInfinity(deltaY)
        ? float.PositiveInfinity
        : (stepY > 0 ? (y + 1 - originY) : (originY - y)) * deltaY;
      var nextZ = float.IsInfinity(deltaZ)
        ? float.PositiveInfinity
        : (stepZ > 0 ? (z + 1 - originZ) : (originZ - z)) * deltaZ;

      var previousX = x;
      var previousY = y;
      var previousZ = z;

      // 256 steps is far more than a 12-block reach can cross even at grazing angles.
      for (var guard = 0; guard < 256; guard++)
      {
        if (IsInside(region, x, y, z) && store.IsSolidCell(x, y, z))
        {
          var hasPlace = IsInside(region, previousX, previousY, previousZ)
                         && !store.IsSolidCell(previousX, previousY, previousZ);

          return new VoxelAim(true, hasPlace, x, y, z, previousX, previousY, previousZ);
        }

        previousX = x;
        previousY = y;
        previousZ = z;

        if (nextX < nextY && nextX < nextZ)
        {
          if (nextX > maxDistance)
            break;

          x += stepX;
          nextX += deltaX;
        }
        else if (nextY < nextZ)
        {
          if (nextY > maxDistance)
            break;

          y += stepY;
          nextY += deltaY;
        }
        else
        {
          if (nextZ > maxDistance)
            break;

          z += stepZ;
          nextZ += deltaZ;
        }

        if (y < 0 || y >= WorldChunk.Height)
          break;

        if (x < 0 || x >= region.Width || z < 0 || z >= region.Depth)
          break;
      }

      return default;
    }

    private static bool IsInside(RagonWorldRegion region, int x, int y, int z)
    {
      return x >= 0 && x < region.Width
             && y >= 0 && y < WorldChunk.Height
             && z >= 0 && z < region.Depth;
    }

    private readonly struct VoxelAim
    {
      public readonly bool Hit;
      public readonly bool HasPlace;
      public readonly int X;
      public readonly int Y;
      public readonly int Z;
      public readonly int PlaceX;
      public readonly int PlaceY;
      public readonly int PlaceZ;

      public VoxelAim(
        bool hit,
        bool hasPlace,
        int x,
        int y,
        int z,
        int placeX,
        int placeY,
        int placeZ)
      {
        Hit = hit;
        HasPlace = hasPlace;
        X = x;
        Y = y;
        Z = z;
        PlaceX = placeX;
        PlaceY = placeY;
        PlaceZ = placeZ;
      }
    }
  }
}
