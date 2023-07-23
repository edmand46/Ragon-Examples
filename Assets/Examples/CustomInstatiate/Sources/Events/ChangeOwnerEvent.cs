using Ragon.Client;
using Ragon.Protocol;

namespace Examples.Sources.Events
{
  public class ChangeOwnerEvent: IRagonEvent
  {
    public ushort PlayerPeerId;
    public void Serialize(RagonBuffer buffer)
    {
      buffer.WriteUShort(PlayerPeerId);
    }

    public void Deserialize(RagonBuffer buffer)
    {
      PlayerPeerId = buffer.ReadUShort();
    }
  }
}