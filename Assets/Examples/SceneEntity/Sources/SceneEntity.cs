using System;
using Ragon.Client;
using Ragon.Client.Unity;
using Ragon.Protocol;
using UnityEngine;

namespace Examples.SceneEntity
{
  public class SceneEntity : RagonBehaviour
  {
    [SerializeField] private RagonFloat _health = new RagonFloat(true, 0);
    [SerializeField] private RagonString _name = new RagonString(32, true, 0);
    
    private IDisposable _sub;
    
    public override void OnAttachedEntity()
    {
      if (HasAuthority)
      {
        _sub = Entity.OnEvent<SceneEntityEvent>(OnTestSceneEvent);
      }

      _name.Changed += () => Debug.Log(_name.Value);
      _health.Changed += () => Debug.Log(_health.Value);
    }

    private void OnTestSceneEvent(RagonPlayer player, SceneEntityEvent evnt)
    {
      if (RagonNetwork.TryGetLink(Entity.Id, out var link))
      {
        Debug.Log($"Link: {link.name}");
      }
      
      Debug.Log($"Event executed {evnt.Message}");
    }

    public override void OnUpdateEntity()
    {
      if (Input.GetKeyDown(KeyCode.Space))
      {
        _health.Value += 10;
        _name.Value = $"Eduard";
        // Entity.ReplicateEvent(new SceneEntityEvent() { Message = "Eduard" });
      }
    }
  }
}