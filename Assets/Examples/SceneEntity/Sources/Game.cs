using System;
using System.Collections.Generic;
using Ragon.Client;
using Ragon.Client.Unity;
using Ragon.Protocol;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Examples.SceneEntity
{
  public class Game : MonoBehaviour, IRagonListener, IRagonDataListener
  {
    private void OnDestroy()
    {
      RagonNetwork.Disconnect();
    }

    private void Start()
    {
      Application.targetFrameRate = 60;
      
      RagonNetwork.Event.Register<SceneEntityEvent>();
      RagonNetwork.AutoSceneLoading = false;
      RagonNetwork.AddListener((IRagonListener)this);
      RagonNetwork.AddListener((IRagonDataListener)this);
      RagonNetwork.Connect();
    }

    private void Update()
    {
      if (Input.GetKeyDown(KeyCode.Space))
      {
        RagonNetwork.Room.UserData["color"] = new byte[] { 100, 200, 250 };
      }
    }

    public void OnConnected(RagonClient network)
    {
      var randomName = $"Player {Random.Range(100, 999)}";
      network.Session.AuthorizeWithKey("defaultkey", randomName, "{ \"itemId\": 123 }");
    }

    public void OnAuthorizationSuccess(RagonClient network, string playerId, string playerName)
    {
      network.Session.CreateOrJoin("SceneEntityExample", 1, 4);
    }

    public void OnAuthorizationFailed(RagonClient network, string message)
    {
    }

    public void OnJoined(RagonClient network)
    {
      Debug.Log($"Room Property [\"color\"] {string.Join(", ", RagonNetwork.Room.UserData["color"])}");
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

    public void OnSceneLoaded(RagonClient client)
    {
    }

    public void OnRequestScene(RagonClient client, string sceneName)
    {
    }

    public void OnData(RagonClient client, RagonPlayer player, byte[] data)
    {
    }

    public void OnRoomListUpdate(RagonClient client, IReadOnlyList<RagonRoomInformation> roomsInfos)
    {
      throw new NotImplementedException();
    }

    public void OnRoomPropertiesUpdated(RagonClient client)
    {
      throw new NotImplementedException();
    }

    public void OnUserDataUpdated(RagonClient client)
    {
      
    }

    public void OnPlayerUserDataUpdated(RagonClient client, RagonPlayer player)
    {
      
    }

    public void OnUserDataUpdated(RagonClient client, IReadOnlyList<string> changes)
    {
      
    }

    public void OnPlayerUserDataUpdated(RagonClient client, RagonPlayer player, IReadOnlyList<string> changes)
    {
      
    }
  }
}