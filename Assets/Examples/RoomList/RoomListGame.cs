using System;
using System.Collections.Generic;
using Ragon.Client;
using Ragon.Client.Unity;
using Ragon.Protocol;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Examples.SceneEntity
{
  public class RoomListGame : MonoBehaviour, IRagonListener, IRagonDataListener, IRagonRoomListListener
  {
    private void OnDestroy()
    {
      RagonNetwork.Disconnect();
    }

    private void Start()
    {
      Application.targetFrameRate = 60;
      // RagonNetwork.AutoSceneLoading = false;
      RagonNetwork.Event.Register<DemoEvent>();
      RagonNetwork.Event.Register<SceneEntityEvent>();
      RagonNetwork.AddListener((IRagonListener)this);
      RagonNetwork.AddListener((IRagonDataListener)this);
      RagonNetwork.Connect();
    }

    private void Update()
    {
      if (Input.GetKeyDown(KeyCode.Space))
      {
        RagonNetwork.Room.ReplicateData(new byte[] { 100, 200, 250 }, true);
      }
    }

    public void OnData(RagonClient client, RagonPlayer player, byte[] data)
    {
      var playerName = "null";
      if (player == null)
        playerName = "Server";
      else
        playerName = player.Name;
          
      Debug.Log($"Data: [{playerName}]: {string.Join(", ", data)}");
    }

    public void OnUserDataUpdated(RagonClient client, IReadOnlyList<string> changes)
    {
      Debug.Log("Room user data updated");
      Debug.Log(string.Join(",", changes));
    }

    public void OnPlayerUserDataUpdated(RagonClient client, RagonPlayer player, IReadOnlyList<string> changes)
    {
      Debug.Log($"Player {player.Name}|{player.IsLocal} user data updated");
      Debug.Log(string.Join(",", changes));
    }

    public void OnConnected(RagonClient network)
    {
      var randomName = $"Player {Random.Range(100, 999)}";
      network.Session.AuthorizeWithKey("defaultkey", randomName, "{ \"itemId\": 123 }");
    }

    public void OnAuthorizationSuccess(RagonClient network, string playerId, string playerName)
    {
      RagonNetwork.Session.CreateOrJoin("RoomListExample", 1, 4);
    }

    public void OnAuthorizationFailed(RagonClient network, string message)
    {
    }

    public void OnJoined(RagonClient network)
    {
      RagonNetwork.Room.OnEvent<DemoEvent>((invoker, ev) => Debug.Log($"Demo event received {ev.Id}"));

      // Debug.Log($"Room Property [\"room_color\"] {string.Join(", ", RagonNetwork.Room.UserData["room_color"])}");
      // Debug.Log($"Room Property [\"match_started\"] {string.Join(", ", RagonNetwork.Room.UserData["match_started"])}");
    }

    public void OnFailed(RagonClient network, string message)
    {
    }

    public void OnLeft(RagonClient network)
    {
    }

    public void OnDisconnected(RagonClient network, RagonDisconnect reason)
    {
    }

    public void OnPlayerJoined(RagonClient network, RagonPlayer player)
    {
    }

    public void OnPlayerLeft(RagonClient network, RagonPlayer player)
    {
    }

    public void OnOwnershipChanged(RagonClient network, RagonPlayer player)
    {
    }

    public void OnSceneLoaded(RagonClient network)
    {
    }

    public void OnRoomListUpdate(RagonClient client, IReadOnlyList<RagonRoomInformation> roomsInfos)
    {
      Debug.Log("Rooms:");
      foreach (var info in roomsInfos)
      {
        Debug.Log($"Room: {info.Id} {info.Scene} {info.PlayerCount}/{info.PlayerMax}");
      }
    }
  }
}