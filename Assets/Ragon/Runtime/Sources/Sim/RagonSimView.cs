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

// FILE:Runtime/Sources/Sim/RagonSimView.cs ROLE:Unity view adapter over FrameworkGameClient (players, projectiles, NPCs, events) DEPS:Ragon.Framework.Client;RagonNetwork;RagonSimViewConfig
// R:_framework,_players,_projectiles,_npcs W:_framework,_players,_projectiles,_npcs
// API:Framework ; OnSimExplosion ; OnSimHit ; OnSimKill
// WHY:DefaultExecutionOrder(-1400) pumps after RagonNetwork(-1500) and RagonSimPlayerInput(-1450) but before user scripts, so poses are current when gameplay reads them
// WHY:the local player renders only through PredictionRunner; using PlayerView.RenderAt for the local slot would apply the correction twice
// WHY:the local pose uses PredictionAlpha (fraction into the prediction tick); RenderState(1.0) advanced only on 30 Hz prediction ticks and stepped visibly at frame rate

using System;
using System.Collections.Generic;
using Ragon.Client;
using Ragon.Framework;
using Ragon.Framework.Client;
using UnityEngine;

namespace Ragon.Client.Unity
{
  /// <summary>
  /// Thin Unity view over the G1 client runtime. The adapter owns one
  /// <see cref="FrameworkGameClient"/> built on the existing <see cref="RagonNetwork"/> connection,
  /// ticks it once per frame, maps <see cref="SimView"/> records onto prefabs, and forwards the
  /// snapshot event queue as plain C# events.
  ///
  /// <para>Nothing here simulates: movement prediction is the framework's, remote poses are
  /// interpolated from each entity's retained record ring, projectiles are drawn from the closed-form
  /// spawn record, and hp/weapon/death come from server records. Game code consumes
  /// <see cref="Framework"/> (or the events) and adds no netcode.</para>
  /// </summary>
  [DefaultExecutionOrder(-1400)]
  [DisallowMultipleComponent]
  public sealed class RagonSimView : MonoBehaviour
  {
    [SerializeField] private RagonSimViewConfig _config;

    private readonly Dictionary<byte, GameObject> _players = new Dictionary<byte, GameObject>();
    private readonly Dictionary<byte, bool> _localPrefabSlots = new Dictionary<byte, bool>();
    private readonly Dictionary<ushort, GameObject> _projectiles = new Dictionary<ushort, GameObject>();
    private readonly Dictionary<ushort, GameObject> _npcs = new Dictionary<ushort, GameObject>();

    private FrameworkGameClient _framework;
    private bool _contentFailed;

    /// <summary>Explosion world position and the firing weapon content index.</summary>
    public event Action<Vector3, byte> OnSimExplosion;

    /// <summary>Damaged slot and the damage amount.</summary>
    public event Action<byte, ushort> OnSimHit;

    /// <summary>Killer slot, victim slot and weapon content index.</summary>
    public event Action<byte, byte, byte> OnSimKill;

    /// <summary>The live framework client, or null while the room is not a configured sim room.</summary>
    public FrameworkGameClient Framework => _framework;

    /// <summary>True once the adapter has built the framework client for this room.</summary>
    public bool IsRunning => _framework != null;

    public RagonSimViewConfig Config
    {
      get => _config;
      set
      {
        _config = value;
        _contentFailed = false;
      }
    }

    private void Update()
    {
      if (_config == null)
      {
        Teardown();
        return;
      }

      if (!RagonNetwork.IsInitialized)
      {
        Teardown();
        return;
      }

      var client = RagonNetwork.Network;
      if (client == null
          || client.Status != RagonStatus.ROOM
          || client.Room == null
          || client.Room.Scene != _config.SimScene)
      {
        Teardown();
        return;
      }

      if (_framework == null && !TryCreate(client))
        return;

      _framework.Tick(Time.deltaTime);
      ApplyPlayers();
      ApplyProjectiles();
      ApplyNpcs();
      DrainEvents();
    }

    private void OnDisable()
    {
      Teardown();
    }

    private bool TryCreate(RagonClient client)
    {
      if (_contentFailed)
        return false;

      GameContent content;
      try
      {
        content = _config.Content;
      }
      catch (Exception exception)
      {
        _contentFailed = true;
        Debug.LogError($"RagonSimView: cannot parse the sim game content: {exception.Message}", this);
        return false;
      }

      if (content == null)
      {
        _contentFailed = true;
        Debug.LogError("RagonSimView: assign the sim game.json TextAsset on the config", this);
        return false;
      }

      _framework = new FrameworkGameClient(content, client, (ushort)_config.TickRate);

      _framework.View.PlayerSpawned += OnPlayerSpawned;
      _framework.View.PlayerRemoved += OnPlayerRemoved;
      _framework.View.ProjectileSpawned += OnProjectileSpawned;
      _framework.View.ProjectileRemoved += OnProjectileRemoved;
      _framework.View.NpcSpawned += OnNpcSpawned;
      _framework.View.NpcRemoved += OnNpcRemoved;

      return true;
    }

    private void OnPlayerSpawned(PlayerView view)
    {
      EnsurePlayerPrefab(view.Slot);
    }

    private void OnPlayerRemoved(PlayerView view)
    {
      DestroyPlayer(view.Slot);
    }

    private void OnProjectileSpawned(ProjectileView view)
    {
      var prefab = _config.GetProjectilePrefab(view.Record.Kind);
      if (prefab == null)
        return;

      var go = Instantiate(prefab, transform);
      go.name = $"Projectile {view.Record.Id}";
      _projectiles[view.Record.Id] = go;
    }

    private void OnProjectileRemoved(ProjectileView view)
    {
      DestroyProjectile(view.Record.Id);
    }

    private void OnNpcSpawned(NpcView view)
    {
      if (_config.NpcPrefab == null)
        return;

      var go = Instantiate(_config.NpcPrefab, transform);
      go.name = $"Npc {view.Id}";
      _npcs[view.Id] = go;
    }

    private void OnNpcRemoved(NpcView view)
    {
      DestroyNpc(view.Id);
    }

    private void ApplyPlayers()
    {
      foreach (var pair in _framework.View.Players)
      {
        var slot = pair.Key;
        var view = pair.Value;

        EnsurePlayerPrefab(slot);

        if (!_players.TryGetValue(slot, out var go) || go == null)
          continue;

        if (IsLocal(slot) && _framework.Prediction.Initialized)
        {
          // Alpha is progress into the current prediction tick; the correction is already inside
          // PredictionRunner, so passing 0..1 here interpolates between Previous and Current every
          // frame instead of stepping once per 30 Hz tick.
          var state = _framework.Prediction.RenderState(_framework.PredictionAlpha);
          ApplyPose(go.transform, state.Pos, state.Yaw, state.Pitch);
          if (go.activeSelf != _framework.Prediction.Alive)
            go.SetActive(_framework.Prediction.Alive);
          continue;
        }

        var remote = view.RenderAt(_framework.Clock.RenderTick);
        ApplyPose(go.transform, remote.Position, remote.Yaw, remote.Pitch);
        if (go.activeSelf != remote.Alive)
          go.SetActive(remote.Alive);
      }
    }

    private void ApplyProjectiles()
    {
      if (_projectiles.Count == 0)
        return;

      var renderTick = _framework.Clock.RenderTick;
      var step = _framework.Clock.StepSeconds;

      foreach (var pair in _framework.View.Projectiles)
      {
        if (!_projectiles.TryGetValue(pair.Key, out var go) || go == null)
          continue;

        var view = pair.Value;
        var age = (renderTick - view.SpawnTick) * step;
        if (age < 0)
          age = 0;

        var position = view.PositionAt(age, ProjectileGravity(view.Record.Kind));
        go.transform.position = ToVector3(position);
      }
    }

    private void ApplyNpcs()
    {
      if (_npcs.Count == 0)
        return;

      foreach (var pair in _framework.View.Npcs)
      {
        if (!_npcs.TryGetValue(pair.Key, out var go) || go == null)
          continue;

        var record = pair.Value.RenderAt(_framework.Clock.RenderTick);
        ApplyPose(go.transform, record.Pos, record.Yaw, 0);
      }
    }

    private void DrainEvents()
    {
      var events = _framework.View.DrainEvents();
      for (var i = 0; i < events.Length; i++)
      {
        var evnt = events[i];
        switch (evnt.Kind)
        {
          case SimEventKind.Explosion:
            OnSimExplosion?.Invoke(ToVector3(evnt.Explosion.Pos), evnt.Explosion.Kind);
            break;
          case SimEventKind.Hit:
            OnSimHit?.Invoke(evnt.Hit.Slot, evnt.Hit.Damage);
            break;
          case SimEventKind.Kill:
            OnSimKill?.Invoke(evnt.Kill.Killer, evnt.Kill.Victim, evnt.Kill.Weapon);
            break;
        }
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

    private void DestroyProjectile(ushort id)
    {
      if (!_projectiles.TryGetValue(id, out var go))
        return;

      _projectiles.Remove(id);
      if (go != null)
        Destroy(go);
    }

    private void DestroyNpc(ushort id)
    {
      if (!_npcs.TryGetValue(id, out var go))
        return;

      _npcs.Remove(id);
      if (go != null)
        Destroy(go);
    }

    private bool IsLocal(byte slot)
    {
      var localSlot = _framework.View.LocalSlot;
      return localSlot.HasValue && localSlot.Value == slot;
    }

    private double ProjectileGravity(byte kind)
    {
      var content = _config.Content;
      if (content == null || kind >= content.Weapons.Length)
        return 0;

      return content.Weapons[kind].ProjectileGravity;
    }

    private static void ApplyPose(Transform target, Vec3 position, double yaw, double pitch)
    {
      target.position = ToVector3(position);
      target.rotation = Quaternion.Euler(
        (float)(-pitch * Mathf.Rad2Deg),
        (float)(yaw * Mathf.Rad2Deg),
        0f);
    }

    private static Vector3 ToVector3(Vec3 value)
    {
      return new Vector3((float)value.X, (float)value.Y, (float)value.Z);
    }

    private void Teardown()
    {
      if (_framework != null)
      {
        _framework.View.PlayerSpawned -= OnPlayerSpawned;
        _framework.View.PlayerRemoved -= OnPlayerRemoved;
        _framework.View.ProjectileSpawned -= OnProjectileSpawned;
        _framework.View.ProjectileRemoved -= OnProjectileRemoved;
        _framework.View.NpcSpawned -= OnNpcSpawned;
        _framework.View.NpcRemoved -= OnNpcRemoved;
        _framework.Dispose();
        _framework = null;
      }

      foreach (var pair in _players)
      {
        if (pair.Value != null)
          Destroy(pair.Value);
      }

      foreach (var pair in _projectiles)
      {
        if (pair.Value != null)
          Destroy(pair.Value);
      }

      foreach (var pair in _npcs)
      {
        if (pair.Value != null)
          Destroy(pair.Value);
      }

      _players.Clear();
      _localPrefabSlots.Clear();
      _projectiles.Clear();
      _npcs.Clear();
    }
  }
}
