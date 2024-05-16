using System;
using System.Collections.Generic;
using Examples.Sources;
using Examples.Sources.Events;
using Examples.Sources.Payload;
using Ragon.Client;
using Ragon.Client.Unity;
using Ragon.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Example.Game
{
  public class GameNetwork : MonoBehaviour, IRagonListener, IRagonSceneRequestListener
  {
    [SerializeField] private GameObject CharacterPrefab;

    private void OnDestroy()
    {
      RagonNetwork.Disconnect();
    }

    private void Start()
    {
      Application.targetFrameRate = 60;
      
      RagonNetwork.AutoSceneLoading = false;
      RagonNetwork.Event.Register<ChatMessageEvent>();
      RagonNetwork.Event.Register<ChangeOwnerEvent>();

      RagonNetwork.Configure(null, new GameSpawner());
      RagonNetwork.AddListener((IRagonListener)this);
      RagonNetwork.AddListener((IRagonSceneRequestListener)this);
      RagonNetwork.Connect();
    }

    private void Update()
    {
      if (Input.GetKeyDown(KeyCode.Space))
      {
        RagonNetwork.Room.LoadScene("Level");
      }
    }

    public void OnConnected(RagonClient network)
    {
      var randomName = $"Player {Random.Range(100, 999)}";
      network.Session.AuthorizeWithKey("defaultkey", randomName, "{ \"itemId\": 123 }");
    }

    public void OnAuthorizationSuccess(RagonClient network, string playerId, string playerName)
    {
      network.Session.CreateOrJoin("Game", 1, 4);
    }

    public void OnAuthorizationFailed(RagonClient network, string message)
    {
    }

    public void OnJoined(RagonClient network)
    {
      Debug.Log("On joined");
    }

    public void OnFailed(RagonClient network, string message)
    {
      // Debug.LogError("Failed with " + message);
    }

    public void OnLeft(RagonClient network)
    {
      // Debug.Log("Leaved");
    }

    public void OnDisconnected(RagonClient network, RagonDisconnect reason)
    {
      Debug.Log("Disconnected: " + reason);
    }

    public void OnPlayerJoined(RagonClient network, RagonPlayer player)
    {
      // Debug.Log($"On Joined {player.Name} Players: {RagonNetwork.Room.Players.Count}");
      // NotificationsCanvas.Notifications.AddNotification($"Player joined {player.Name}");
    }

    public void OnPlayerLeft(RagonClient network, RagonPlayer player)
    {
      // Debug.Log($"On Left {player.Name} Players: {RagonNetwork.Room.Players.Count}");
      // NotificationsCanvas.Notifications.AddNotification($"Player left {player.Name}");
    }

    public void OnOwnershipChanged(RagonClient network, RagonPlayer player)
    {
      // Debug.Log("New room owner " + player.Name);
    }

    public void OnSceneLoaded(RagonClient client)
    { 
      Debug.Log("Scene loaded");
      var payload = new CharacterPayload() { X = 2.0f, Z = 2.0f };
      RagonNetwork.Create(CharacterPrefab, payload);
    }

    public void OnRequestScene(RagonClient client, string sceneName)
    {
      client.Room.SceneLoaded();
    }

    public void OnRoomListUpdate(RagonClient client, IReadOnlyList<RagonRoomInformation> roomsInfos)
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