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

using System.Collections.Generic;
using Ragon.Client;
using Ragon.Client.Unity;
using Ragon.Protocol;
using UnityEngine;

namespace Examples.SimFPS
{
  /// <summary>
  /// The only game-side script in this demo: connect, authorize and join-or-create the scene the
  /// relay is configured to host as a framework sim room. Everything after that is
  /// <see cref="Ragon.Client.Unity.RagonSimView"/>: movement, prediction and rendering are the
  /// framework's, and this file contains no netcode.
  ///
  /// <para><c>AutoSceneLoading</c> is off because the sim room has no Unity scene to load; the
  /// client reports the scene ready itself so the server can start sending snapshots.</para>
  /// </summary>
  public sealed class SimFPSBootstrap : MonoBehaviour, IRagonListener
  {
    [SerializeField] private string _scene = "framework-arena";
    [SerializeField] private string _serverKey = "defaultkey";
    [SerializeField] private string _playerName = "SimFPS Player";

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

    public void OnAuthorizationSuccess(RagonClient client, string playerId, string playerName)
    {
      client.Session.CreateOrJoin(_scene, 1, 8);
    }

    public void OnJoined(RagonClient client)
    {
      client.Room.SceneLoaded();
    }

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
