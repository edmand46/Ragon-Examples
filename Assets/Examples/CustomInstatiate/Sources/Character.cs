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
      Entity.OnEvent<ChatMessageEvent>(OnChatMessage);
    }

    private void OnChatMessage(RagonPlayer arg1, ChatMessageEvent chatMessage)
    {
      Debug.Log(chatMessage.Text);
    }

    public override void OnUpdateEntity()
    {
      var direction = Vector3.zero;

      if (Input.GetKeyDown(KeyCode.Space))
        Entity.ReplicateEvent(new ChatMessageEvent() { Text = "Test 1" }, RagonTarget.Owner);

      if (Input.GetKey(KeyCode.W))
        direction += Vector3.forward;

      if (Input.GetKey(KeyCode.S))
        direction -= Vector3.forward;

      if (Input.GetKey(KeyCode.D))
        direction += Vector3.right;

      if (Input.GetKey(KeyCode.A))
        direction -= Vector3.right;

      if (Input.GetKey(KeyCode.Q))
        transform.Rotate(0, 30 * Time.deltaTime, 0);

      if (Input.GetKey(KeyCode.E))
        transform.Rotate(0, -30 * Time.deltaTime, 0);

      transform.position += direction * Time.deltaTime;
    }
  }
}