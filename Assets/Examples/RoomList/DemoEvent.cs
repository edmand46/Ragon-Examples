using Ragon.Client;
using Ragon.Protocol;

namespace Examples.SceneEntity
{
  public class DemoEvent : IRagonEvent
  {
    public int Id;

    public void Serialize(RagonBuffer buffer)
    {
      buffer.WriteInt(Id);
    }

    public void Deserialize(RagonBuffer buffer)
    {
      Id = buffer.ReadInt();
    }
  }
}