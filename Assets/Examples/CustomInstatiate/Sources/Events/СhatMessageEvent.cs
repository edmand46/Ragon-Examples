using Ragon.Client;
using Ragon.Protocol;

namespace Examples.Sources.Events
{
  public class ChatMessageEvent: IRagonEvent
  {
    public string Text; 
    public void Serialize(RagonBuffer buffer)
    {
      buffer.WriteString(Text);
    }

    public void Deserialize(RagonBuffer buffer)
    {
      Text = buffer.ReadString();
    }
  }
}