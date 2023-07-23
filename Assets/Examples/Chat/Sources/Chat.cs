using Ragon.Client;
using Ragon.Client.Unity;
using TMPro;
using UnityEngine;

public class Chat : RagonBehaviour
{
  [SerializeField] private TMP_InputField chatInputField;
  [SerializeField] private GameObject chatMessagePrefab;
  [SerializeField] private Transform chatContent;

  public override void OnAttachedEntity()
  {
    Entity.OnEvent<ChatMessageEvent>(OnChatMessage);
  }

  private void OnChatMessage(RagonPlayer player, ChatMessageEvent chatMessage)
  {
    var playerName = player.Name;
    var messageText = chatMessage.Text;
    var formattedMessage = $"<color=#7CFFC7>{playerName}</color>: {messageText}";
    var messageGo = Instantiate(chatMessagePrefab, chatContent);
    messageGo.GetComponent<TMP_Text>().text = formattedMessage;
  }
  
  public void SubmitMessage()
  {
    var message = chatInputField.text;
    if (!string.IsNullOrEmpty(message))
    {
      Entity.ReplicateEvent(new ChatMessageEvent() { Text = message });
      chatInputField.text = "";
    }
  }
}
