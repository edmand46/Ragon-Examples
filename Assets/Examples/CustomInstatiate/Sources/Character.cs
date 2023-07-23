using Examples.Sources.Events;
using Ragon.Client;
using Ragon.Client.Unity;
using Ragon.Protocol;
using UnityEngine;

namespace Examples.Sources
{
  public class Character : RagonBehaviour
  {
    [SerializeField] private RagonFloat _health = new RagonFloat(true);

    public override void OnAttachedEntity()
    {
      Debug.Log("Player attached!");
      Entity.OnEvent<ChangeOwnerEvent>(RequestChangeOwner);
    }

    public override void OnOwnershipChanged(RagonPlayer player)
    {
      Debug.Log("I'm Owner!");
    }

    private void RequestChangeOwner(RagonPlayer requester, ChangeOwnerEvent @event)
    {
      RagonNetwork.Transfer(gameObject, requester);
    }

    public override void OnUpdateEntity()
    {
      var direction = Vector3.zero;

      if (Input.GetKey(KeyCode.W))
        direction += Vector3.forward;

      if (Input.GetKey(KeyCode.S))
        direction -= Vector3.forward;

      if (Input.GetKey(KeyCode.D))
        direction += Vector3.right;

      if (Input.GetKey(KeyCode.A))
        direction -= Vector3.right;

      transform.position += direction * Time.deltaTime;
    }

    public override void OnUpdateProxy()
    {
      if (Input.GetKeyDown(KeyCode.Space))
        Entity.ReplicateEvent(new ChangeOwnerEvent(), RagonTarget.Owner);
    }
  }
}