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

// FILE:Runtime/Sources/Sim/RagonSimWorldView.cs ROLE:Unity view over FrameworkWorldClient (voxel regions, chunk store, players) DEPS:Ragon.Framework.Client;RagonNetwork;RagonSimWorldConfig
// R:_world,_players W:_world,_players
// API:World ; Framework ; Store ; CurrentRegion ; RegionChanged ; BlockChanged ; StoreReverted ; RequestEditAt ; TryGetLocalPose
// WHY:DefaultExecutionOrder(-1400) pumps after RagonNetwork(-1500) and RagonSimWorldInput(-1450) but before user scripts, so the store and render poses are current
// WHY:SceneLoaded() runs from OnJoined for EVERY join, including migrations; a region JOIN_SUCCESS that is not reported leaves the player unready and no chunk stream starts
// WHY:the local player renders through PredictionRunner only; PlayerView.RenderAt would apply the correction twice, exactly as in RagonSimView
// WHY:block edits travel region-local on the wire; RequestEditAt is the only world-space entry point and translates through CurrentRegion

using System;
using System.Collections.Generic;
using Ragon.Client;
using Ragon.Framework;
using Ragon.Framework.Client;
using Ragon.Protocol;
using UnityEngine;

namespace Ragon.Client.Unity
{
  /// <summary>
  /// Thin Unity view over the G2 world client. The component owns one
  /// <see cref="FrameworkWorldClient"/> built on the existing <see cref="RagonNetwork"/> connection,
  /// joins the configured initial region after authorization, ticks the runtime once per frame, and
  /// renders players in world space (region origin + region-local pose).
  ///
  /// <para>Building is intent, not netcode: <see cref="RequestEditAt"/> translates a world-space cell
  /// to region-local coordinates and forwards it to <see cref="FrameworkWorldClient.RequestEdit"/>,
  /// which owns prediction and the input tail. Terrain is not part of this component; a game reads
  /// <see cref="Store"/> and rebuilds meshes from the chunk grid.</para>
  ///
  /// <para>Changes are published as plain events: <see cref="RegionChanged"/> once per migration,
  /// <see cref="BlockChanged"/> for predicted and remote/authoritative edits, and
  /// <see cref="StoreReverted"/> when the store had to undo a rejected prediction (a single int poll
  /// so a renderer can invalidate its meshes without diffing the grid).</para>
  /// </summary>
  [DefaultExecutionOrder(-1400)]
  [DisallowMultipleComponent]
  public sealed class RagonSimWorldView : MonoBehaviour, IRagonListener
  {
    [SerializeField] private RagonSimWorldConfig _config;

    private readonly Dictionary<byte, GameObject> _players = new Dictionary<byte, GameObject>();
    private readonly Dictionary<byte, bool> _localPrefabSlots = new Dictionary<byte, bool>();

    private FrameworkWorldClient _world;
    private bool _contentFailed;
    private bool _createFailed;
    private bool _listening;
    private int _lastReverted;

    /// <summary>Region the local runtime just left and the one it switched to.</summary>
    public event Action<RagonWorldRegion, RagonWorldRegion> RegionChanged;

    /// <summary>World-space cell of a predicted, remote or authoritative block change.</summary>
    public event Action<int, int, int> BlockChanged;

    /// <summary>A rejected local prediction was undone; every chunk may now be stale.</summary>
    public event Action StoreReverted;

    /// <summary>The live world runtime, or null before the config is valid and RagonNetwork woke up.</summary>
    public FrameworkWorldClient World => _world;

    /// <summary>The underlying G1 client runtime (prediction, clock, view, content).</summary>
    public FrameworkGameClient Framework => _world != null ? _world.Framework : null;

    /// <summary>The region-local chunk store of the current region.</summary>
    public WorldStore Store => _world != null ? _world.Store : null;

    /// <summary>The region the runtime believes it is in; flips as soon as a crossing is sent.</summary>
    public RagonWorldRegion CurrentRegion => _world != null ? _world.CurrentRegion : null;

    /// <summary>True between sending the migration join and its JOIN_SUCCESS.</summary>
    public bool IsMigrating => _world != null && _world.IsMigrating;

    /// <summary>True once the world runtime exists (it exists before the room join).</summary>
    public bool IsRunning => _world != null;

    public RagonSimWorldConfig Config
    {
      get => _config;
      set
      {
        if (_config == value)
          return;

        DisposeWorld();
        _config = value;
        _contentFailed = false;
        _createFailed = false;
      }
    }

    private void Start()
    {
      if (!_listening && RagonNetwork.IsInitialized)
      {
        RagonNetwork.AddListener((IRagonListener)this);
        _listening = true;
      }

      EnsureWorld();
    }

    private void Update()
    {
      if (!_listening && RagonNetwork.IsInitialized)
      {
        RagonNetwork.AddListener((IRagonListener)this);
        _listening = true;
      }

      EnsureWorld();

      if (_world == null)
        return;

      _world.Tick(Time.deltaTime);
      ApplyPlayers();
      PollReverts();
    }

    private void OnDestroy()
    {
      if (_listening && RagonNetwork.IsInitialized)
        RagonNetwork.RemoveListener((IRagonListener)this);

      _listening = false;
      DisposeWorld();
    }

    /// <summary>
    /// Requests a block edit at a world-space cell. The runtime consumes region-local coordinates,
    /// so this translates through <see cref="CurrentRegion"/> and rejects cells outside it. Returns
    /// true when the local store predicted the edit immediately.
    /// </summary>
    public bool RequestEditAt(int worldX, int y, int worldZ, ushort blockId, bool remove = false)
    {
      if (_world == null || _world.IsMigrating)
        return false;

      var region = _world.CurrentRegion;
      if (region == null || y < 0 || y >= WorldChunk.Height)
        return false;

      var localX = worldX - region.OriginX;
      var localZ = worldZ - region.OriginZ;

      if (localX < 0 || localX >= region.Width || localZ < 0 || localZ >= region.Depth)
        return false;

      if (!remove && blockId == 0)
        return false;

      var predicted = _world.RequestEdit(localX, (ushort)y, localZ, blockId, remove);
      if (predicted)
        BlockChanged?.Invoke(worldX, y, worldZ);

      return predicted;
    }

    /// <summary>
    /// Current local render pose in world space, with the region origin applied. False until the
    /// local identity and prediction are initialized.
    /// </summary>
    public bool TryGetLocalPose(out Vector3 worldPosition, out float yawDegrees, out float pitchDegrees)
    {
      worldPosition = Vector3.zero;
      yawDegrees = 0f;
      pitchDegrees = 0f;

      if (_world == null)
        return false;

      var framework = _world.Framework;
      if (!framework.Prediction.Initialized)
        return false;

      var state = framework.Prediction.RenderState(framework.PredictionAlpha);
      worldPosition = ToWorld(state.Pos);
      yawDegrees = (float)(state.Yaw * Mathf.Rad2Deg);
      pitchDegrees = (float)(state.Pitch * Mathf.Rad2Deg);
      return true;
    }

    private void EnsureWorld()
    {
      if (_world != null || _contentFailed || _createFailed || _config == null || !RagonNetwork.IsInitialized)
        return;

      GameContent content;
      try
      {
        content = _config.Content;
      }
      catch (Exception exception)
      {
        _contentFailed = true;
        Debug.LogError($"RagonSimWorldView: cannot parse the world game content: {exception.Message}", this);
        return;
      }

      if (content == null)
      {
        _contentFailed = true;
        Debug.LogError("RagonSimWorldView: assign the world game.json TextAsset on the config", this);
        return;
      }

      try
      {
        _world = new FrameworkWorldClient(
          content,
          RagonNetwork.Network,
          RagonWorldManifest.Default,
          _config.InitialRegion,
          (ushort)_config.TickRate);
      }
      catch (Exception exception)
      {
        _createFailed = true;
        Debug.LogError($"RagonSimWorldView: cannot create the world runtime: {exception.Message}", this);
        return;
      }

      _world.RegionChanged += OnRegionChanged;
      _world.Framework.View.PlayerSpawned += OnPlayerSpawned;
      _world.Framework.View.PlayerRemoved += OnPlayerRemoved;
      _world.Framework.View.BlockEdited += OnBlockEdited;

      _lastReverted = _world.Store.RevertedEditCount;
    }

    private void DisposeWorld()
    {
      if (_world == null)
        return;

      _world.RegionChanged -= OnRegionChanged;
      _world.Framework.View.PlayerSpawned -= OnPlayerSpawned;
      _world.Framework.View.PlayerRemoved -= OnPlayerRemoved;
      _world.Framework.View.BlockEdited -= OnBlockEdited;
      _world.Dispose();
      _world = null;

      foreach (var pair in _players)
      {
        if (pair.Value != null)
          Destroy(pair.Value);
      }

      _players.Clear();
      _localPrefabSlots.Clear();
    }

    public void OnAuthorizationSuccess(RagonClient client, string playerId, string playerName)
    {
      // Authorization is the earliest point the SDK lets a session join a room; the region id is
      // already inside the world runtime, so every migration after this is the runtime's job.
      if (_world != null)
        _world.Join();
    }

    public void OnJoined(RagonClient client)
    {
      // SIM regions only stream to ready players. Every JOIN_SUCCESS, including a migration's,
      // needs this or its chunk stream never starts.
      client.Room.SceneLoaded();
    }

    private void OnRegionChanged(RagonWorldRegion previous, RagonWorldRegion target)
    {
      // Framework.Reset drops the view's players without raising PlayerRemoved; rebuild the render
      // objects on the new identity so a slot change cannot leave a ghost capsule behind.
      foreach (var pair in _players)
      {
        if (pair.Value != null)
          Destroy(pair.Value);
      }

      _players.Clear();
      _localPrefabSlots.Clear();
      _lastReverted = _world != null ? _world.Store.RevertedEditCount : 0;

      RegionChanged?.Invoke(previous, target);
    }

    private void OnBlockEdited(BlockEditEvent evnt)
    {
      var region = _world != null ? _world.CurrentRegion : null;
      if (region == null)
        return;

      BlockChanged?.Invoke(region.OriginX + evnt.X, evnt.Y, region.OriginZ + evnt.Z);
    }

    private void PollReverts()
    {
      var reverted = _world.Store.RevertedEditCount;
      if (reverted == _lastReverted)
        return;

      _lastReverted = reverted;
      StoreReverted?.Invoke();
    }

    private void OnPlayerSpawned(PlayerView view)
    {
      EnsurePlayerPrefab(view.Slot);
    }

    private void OnPlayerRemoved(PlayerView view)
    {
      DestroyPlayer(view.Slot);
    }

    private void ApplyPlayers()
    {
      foreach (var pair in _world.Framework.View.Players)
      {
        var slot = pair.Key;
        var view = pair.Value;

        EnsurePlayerPrefab(slot);

        if (!_players.TryGetValue(slot, out var go) || go == null)
          continue;

        if (IsLocal(slot) && _world.Framework.Prediction.Initialized)
        {
          var state = _world.Framework.Prediction.RenderState(_world.Framework.PredictionAlpha);
          ApplyPose(go.transform, state.Pos, state.Yaw, state.Pitch);
          if (go.activeSelf != _world.Framework.Prediction.Alive)
            go.SetActive(_world.Framework.Prediction.Alive);
          continue;
        }

        var remote = view.RenderAt(_world.Framework.Clock.RenderTick);
        ApplyPose(go.transform, remote.Position, remote.Yaw, remote.Pitch);
        if (go.activeSelf != remote.Alive)
          go.SetActive(remote.Alive);
      }
    }

    private void EnsurePlayerPrefab(byte slot)
    {
      var wantsLocal = IsLocal(slot) && _config.LocalPlayerPrefab != null;
      var exists = _players.TryGetValue(slot, out var go) && go != null;
      var isLocalPrefab = _localPrefabSlots.TryGetValue(slot, out var flag) && flag;

      if (exists && isLocalPrefab == wantsLocal)
        return;

      if (exists)
        DestroyPlayer(slot);

      var prefab = wantsLocal ? _config.LocalPlayerPrefab : _config.PlayerPrefab;
      if (prefab == null)
        return;

      go = Instantiate(prefab, transform);
      go.name = wantsLocal ? $"LocalPlayer {slot}" : $"Player {slot}";
      _players[slot] = go;
      _localPrefabSlots[slot] = wantsLocal;
    }

    private void DestroyPlayer(byte slot)
    {
      if (!_players.TryGetValue(slot, out var go))
        return;

      _players.Remove(slot);
      _localPrefabSlots.Remove(slot);

      if (go != null)
        Destroy(go);
    }

    private bool IsLocal(byte slot)
    {
      var localSlot = _world.Framework.View.LocalSlot;
      return localSlot.HasValue && localSlot.Value == slot;
    }

    private void ApplyPose(Transform target, Vec3 local, double yaw, double pitch)
    {
      target.position = ToWorld(local);
      target.rotation = Quaternion.Euler(
        (float)(-pitch * Mathf.Rad2Deg),
        (float)(yaw * Mathf.Rad2Deg),
        0f);
    }

    private Vector3 ToWorld(Vec3 local)
    {
      var region = _world.CurrentRegion;
      return new Vector3(
        (float)(region.OriginX + local.X),
        (float)local.Y,
        (float)(region.OriginZ + local.Z));
    }

    // IRagonListener: the world view only owns authorization and join readiness. Everything else is
    // the application's business; these stubs keep the facade's listener surface explicit.
    public void OnConnected(RagonClient client) { }
    public void OnAuthorizationFailed(RagonClient client, string message) { }
    public void OnDisconnected(RagonClient client, RagonDisconnect reason) { }
    public void OnFailed(RagonClient client, string message) { }
    public void OnLeft(RagonClient client) { }
    public void OnOwnershipChanged(RagonClient client, RagonPlayer player) { }
    public void OnPlayerJoined(RagonClient client, RagonPlayer player) { }
    public void OnPlayerLeft(RagonClient client, RagonPlayer player) { }
    public void OnSceneLoaded(RagonClient client) { }
    public void OnRoomListUpdate(RagonClient client, IReadOnlyList<RagonRoomInformation> roomsInfos) { }
    public void OnUserDataUpdated(RagonClient client) { }
    public void OnUserDataUpdated(RagonClient client, IReadOnlyList<string> changes) { }
    public void OnPlayerUserDataUpdated(RagonClient client, RagonPlayer player) { }
    public void OnPlayerUserDataUpdated(RagonClient client, RagonPlayer player, IReadOnlyList<string> changes) { }
  }
}
