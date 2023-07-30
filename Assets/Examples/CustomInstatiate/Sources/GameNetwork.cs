using System;
using Examples.Sources;
using Examples.Sources.Events;
using Examples.Sources.Payload;
using Ragon.Client;
using Ragon.Client.Unity;
using Ragon.Protocol;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Example.Game
{
  public class GameNetwork : MonoBehaviour, IRagonListener
  {
    [SerializeField] private GameObject CharacterPrefab;

    private void OnDestroy()
    {
      RagonNetwork.Disconnect();
    }

    private void Start()
    {
      Application.targetFrameRate = 60;

      RagonNetwork.Event.Register<ChatMessageEvent>();
      RagonNetwork.Event.Register<ChangeOwnerEvent>();

      RagonNetwork.Configure(null, new GameSpawner());
      RagonNetwork.AddListener(this);
      RagonNetwork.Connect();
    }

    public void OnConnected(RagonClient network)
    {
      var randomName = $"Player {Random.Range(100, 999)}";
      network.Session.AuthorizeWithKey("defaultkey", randomName, "{ \"itemId\": 123 }");
    }

    public void OnAuthorizationSuccess(RagonClient network, string playerId, string playerName)
    {
      network.Session.CreateOrJoin("Spawn", 1, 4);
    }

    public void OnAuthorizationFailed(RagonClient network, string message)
    {
    }

    public void OnJoined(RagonClient network)
    {
      // for (var x = 0; x < 4; x++)
      // for (var y = 0; y < 4; y++)
      {
        var payload = new CharacterPayload() { X = 2.0f, Z = 2.0f };
        RagonNetwork.Create(CharacterPrefab, payload);
      }
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

    public void OnLevel(RagonClient network, string sceneName)
    {
      // NotificationsCanvas.Notifications.AddNotification($"Current map {sceneName}");

      // if (SceneManager.GetActiveScene().name != sceneName)
      //   SceneManager.LoadScene(sceneName);
      // else 
      network.Room.SceneLoaded();
    }

    // private void SceneLoaded(Scene arg0, LoadSceneMode arg1)
    // {
    //   _ragonInstance.Room.SceneLoaded();
    // }
  }
}