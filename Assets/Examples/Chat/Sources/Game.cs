using System;
using Ragon.Client;
using Ragon.Client.Unity;
using Ragon.Protocol;
using UnityEngine;
using Random = UnityEngine.Random;

public class Game : MonoBehaviour, IRagonListener
{
  private void Start()
  {
    RagonNetwork.Event.Register<ChatMessageEvent>();
    
    RagonNetwork.AddListener(this);
    RagonNetwork.Connect();
  }

  public void OnConnected(RagonClient client)
  {
    var playerName = $"Player {Random.Range(1000, 2000)}";
    RagonNetwork.Session.AuthorizeWithKey("defaultkey", playerName, "");
  }

  public void OnDisconnected(RagonClient client, RagonDisconnect reason)
  {
    
  }

  public void OnDisconnected(RagonClient client)
  {
  
  }
  
  public void OnAuthorizationSuccess(RagonClient client, string playerId, string playerName)
  {
    RagonNetwork.Session.CreateOrJoin("ChatRoom", 1, 20);
  }
  
  public void OnLevel(RagonClient client, string sceneName)
  {
    RagonNetwork.Room.SceneLoaded();
  }

  public void OnAuthorizationFailed(RagonClient client, string message)
  {
    
  }

  public void OnJoined(RagonClient client)
  {
    
  }
  
  public void OnFailed(RagonClient client, string message)
  {
  
  }

  public void OnLeft(RagonClient client)
  {
    
  }

  public void OnOwnershipChanged(RagonClient client, RagonPlayer player)
  {
    
  }

  public void OnPlayerJoined(RagonClient client, RagonPlayer player)
  {
    
  }

  public void OnPlayerLeft(RagonClient client, RagonPlayer player)
  {
    
  }
}