using Ragon.Client;
using Ragon.Protocol;

namespace Examples.SceneEntity
{
  public class SceneEntityEvent : IRagonEvent
  {
    public string Message;

    public void Serialize(RagonBuffer buffer)
    {
      buffer.WriteString(Message);
    }

    public void Deserialize(RagonBuffer buffer)
    {
      Message = buffer.ReadString();
    }
  }
}