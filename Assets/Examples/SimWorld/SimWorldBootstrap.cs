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

// FILE:Assets/Examples/SimWorld/SimWorldBootstrap.cs ROLE:connect and authorize the demo session DEPS:RagonNetwork
// API:None (scene component)
// WHY:the bootstrap never joins or reports SceneLoaded; RagonSimWorldView owns the region join and the per-JOIN_SUCCESS readiness rule

using System.Collections.Generic;
using Ragon.Client;
using Ragon.Client.Unity;
using Ragon.Protocol;
using UnityEngine;

namespace Examples.SimWorld
{
  /// <summary>
  /// The only connection script in this demo: connect, authorize, and let
  /// <see cref="Ragon.Client.Unity.RagonSimWorldView"/> join the initial region. There is no world
  /// logic and no netcode here.
  /// </summary>
  public sealed class SimWorldBootstrap : MonoBehaviour, IRagonListener
  {
    [SerializeField] private string _serverKey = "defaultkey";
    [SerializeField] private string _playerName = "SimWorld Player";

    private void Start()
    {
      RagonNetwork.AutoSceneLoading = false;
      RagonNetwork.AddListener((IRagonListener)this);
      RagonNetwork.Connect();
    }

    private void OnDestroy()
    {
      RagonNetwork.Disconnect();
    }

    public void OnConnected(RagonClient client)
    {
      client.Session.AuthorizeWithKey(_serverKey, _playerName, "");
    }

    public void OnAuthorizationSuccess(RagonClient client, string playerId, string playerName) { }
    public void OnAuthorizationFailed(RagonClient client, string message) { }
    public void OnDisconnected(RagonClient client, RagonDisconnect reason) { }
    public void OnFailed(RagonClient client, string message) { }
    public void OnJoined(RagonClient client) { }
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
