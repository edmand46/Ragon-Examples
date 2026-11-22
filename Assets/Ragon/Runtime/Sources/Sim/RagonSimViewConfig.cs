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

// FILE:Runtime/Sources/Sim/RagonSimViewConfig.cs ROLE:prefab + content bindings for the framework sim view DEPS:Ragon.Framework.GameContent;UnityEngine
// API:SimScene ; TickRate ; Content ; PlayerPrefab ; LocalPlayerPrefab ; NpcPrefab ; GetProjectilePrefab()
// WHY:the client must parse the SAME game.json the relay simulates with, or prediction reconciles against a different step
// WHY:projectile prefabs are indexed by weapon index (ProjectileRecord.Kind is the firing weapon's content index)

using System.Collections.Generic;
using Ragon.Framework;
using UnityEngine;

namespace Ragon.Client.Unity
{
  /// <summary>
  /// Designer-facing bindings for <see cref="RagonSimView"/>: which room is a framework SIM room,
  /// the content it simulates, and the prefabs used to render players, projectiles and NPCs.
  ///
  /// <para>This is a view-only asset. It never carries game logic and it is deliberately a flat
  /// list of object references so it stays usable without a custom inspector.</para>
  /// </summary>
  [CreateAssetMenu(fileName = "RagonSimViewConfig", menuName = "Ragon/Sim View Config")]
  public sealed class RagonSimViewConfig : ScriptableObject
  {
    [SerializeField] private string _simScene = "framework-arena";
    [SerializeField] private int _tickRate = 30;
    [SerializeField] private TextAsset _gameContent;

    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _localPlayerPrefab;
    [SerializeField] private GameObject _npcPrefab;
    [SerializeField] private List<GameObject> _projectilePrefabs = new List<GameObject>();

    private GameContent _content;

    /// <summary>Room scene name hosted by the relay as a framework sim room.</summary>
    public string SimScene => _simScene;

    /// <summary>Framework tick rate; must equal the relay's <c>ServerTickRate</c>.</summary>
    public int TickRate => _tickRate > 0 ? _tickRate : 30;

    /// <summary>Prefab for every non-local player slot.</summary>
    public GameObject PlayerPrefab => _playerPrefab;

    /// <summary>Optional distinct prefab for the local player (null falls back to <see cref="PlayerPrefab"/>).</summary>
    public GameObject LocalPlayerPrefab => _localPlayerPrefab != null ? _localPlayerPrefab : _playerPrefab;

    /// <summary>Optional NPC prefab.</summary>
    public GameObject NpcPrefab => _npcPrefab;

    /// <summary>Parsed game content, cached per asset instance. Null until a game.json is assigned.</summary>
    public GameContent Content
    {
      get
      {
        if (_content == null && _gameContent != null)
          _content = GameContent.Parse(_gameContent.text);

        return _content;
      }
    }

    /// <summary>Projectile prefab for a weapon content index, or null when no prefab is bound.</summary>
    public GameObject GetProjectilePrefab(byte kind)
    {
      if (kind >= _projectilePrefabs.Count)
        return null;

      return _projectilePrefabs[kind];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
      // Editing the JSON in the inspector must re-parse; the cached content is otherwise stale.
      _content = null;
    }
#endif
  }
}
