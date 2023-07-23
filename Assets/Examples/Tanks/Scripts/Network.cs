using System;
using System.Collections.Generic;
using Ragon.Client;
using Ragon.Client.Unity;
using Ragon.Examples.Tanks;
using Ragon.Protocol;
using Tanks.Scripts.Events;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Tanks.Scripts
{
  public class Network : MonoBehaviour, IRagonListener
  {
    [SerializeField] private GameObject _tankPrefab;
    [SerializeField] private List<Transform> _spawnPoints;

    private void Start()
    {
      RagonNetwork.Event.Register<FireEvent>();
       
      RagonNetwork.AddListener(this);
      RagonNetwork.Connect();
    }

    public void OnAuthorizationSuccess(RagonClient client, string playerId, string playerName)
    {
      Debug.Log("Authorized!");
      RagonNetwork.Session.CreateOrJoin("Example", 1, 2); 
    }

    public void OnJoined(RagonClient client)
    {
      var randomPoint = _spawnPoints[Random.Range(0, _spawnPoints.Count - 1)];
      RagonNetwork.Create(_tankPrefab, new TankPayload() { Position = randomPoint.position });
    }

    public void OnFailed(RagonClient client, string message)
    {
    }
    
    public void OnConnected(RagonClient client)
    {
      Debug.Log("Connected!");
      var randomName = $"Player {Random.Range(100, 999)}";
      RagonNetwork.Session.AuthorizeWithKey("defaultkey", randomName);
    }

    public void OnDisconnected(RagonClient client, RagonDisconnect reason)
    {
    }


    public void OnPlayerJoined(RagonClient client, RagonPlayer player)
    {
    }

    public void OnPlayerLeft(RagonClient client, RagonPlayer player)
    {
    }

    public void OnOwnershipChanged(RagonClient client, RagonPlayer player)
    {
    }

    public void OnLevel(RagonClient client, string sceneName)
    {
      Debug.Log("Level " + sceneName);
      RagonNetwork.Room.SceneLoaded();
    }
    

    public void OnAuthorizationFailed(RagonClient client, string message)
    {
   
    }

    public void OnLeft(RagonClient client)
    {
   
    }
  }
}