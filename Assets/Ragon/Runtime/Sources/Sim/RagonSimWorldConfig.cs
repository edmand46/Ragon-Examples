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

// FILE:Runtime/Sources/Sim/RagonSimWorldConfig.cs ROLE:scene/content/prefab bindings for RagonSimWorldView DEPS:Ragon.Framework.GameContent;UnityEngine
// API:WorldScene ; InitialRegion ; TickRate ; Content ; PlayerPrefab ; LocalPlayerPrefab ; DefaultBlockId
// WHY:a separate asset from RagonSimViewConfig keeps the world's initial region and default block out of the arena demo
// WHY:the client must parse the SAME game.json the relay hosts with, or the world store decodes different block ids

using Ragon.Framework;
using UnityEngine;

namespace Ragon.Client.Unity
{
  /// <summary>
  /// Designer-facing bindings for <see cref="RagonSimWorldView"/>: the room scene shared by every
  /// manifest region, the region the local player joins first, the framework tick rate, the same
  /// <c>game.json</c> the relay world is configured with, and the player prefabs.
  ///
  /// <para>The world manifest itself is not serialized: the relay registers
  /// <see cref="RagonWorldManifest.Default"/> when <c>worldEnabled</c> is on, so the client must use
  /// that same embedded map rather than a hand-authored copy.</para>
  /// </summary>
  [CreateAssetMenu(fileName = "RagonSimWorldConfig", menuName = "Ragon/Sim World Config")]
  public sealed class RagonSimWorldConfig : ScriptableObject
  {
    [SerializeField] private string _worldScene = "world";
    [SerializeField] private string _initialRegion = RagonWorldManifest.DefaultRegionA;
    [SerializeField] private int _tickRate = 30;
    [SerializeField] private TextAsset _gameContent;

    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private GameObject _localPlayerPrefab;

    [SerializeField] private ushort _defaultBlockId = 1;

    private GameContent _content;

    /// <summary>Room scene reported by every region's JOIN_SUCCESS.</summary>
    public string WorldScene => _worldScene;

    /// <summary>Manifest region joined after authorization (default <c>world-a</c>).</summary>
    public string InitialRegion => _initialRegion;

    /// <summary>Framework tick rate; must equal the relay's <c>serverTickRate</c>.</summary>
    public int TickRate => _tickRate > 0 ? _tickRate : 30;

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

    /// <summary>Prefab for every non-local player slot.</summary>
    public GameObject PlayerPrefab => _playerPrefab;

    /// <summary>Optional distinct prefab for the local player (null falls back to <see cref="PlayerPrefab"/>).</summary>
    public GameObject LocalPlayerPrefab => _localPlayerPrefab != null ? _localPlayerPrefab : _playerPrefab;

    /// <summary>Solid block id placed by the starter input (must exist in the content block table).</summary>
    public ushort DefaultBlockId => _defaultBlockId;

#if UNITY_EDITOR
    private void OnValidate()
    {
      // Editing the JSON in the inspector must re-parse; the cached content is otherwise stale.
      _content = null;
    }
#endif
  }
}
