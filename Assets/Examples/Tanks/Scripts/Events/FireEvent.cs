using Ragon.Client;
using Ragon.Protocol;

namespace Tanks.Scripts.Events
{
  public class FireEvent: IRagonEvent
  {
    public ushort Damage;
    
    public void Serialize(RagonBuffer serializer)
    {
      serializer.WriteUShort(Damage);
    }

    public void Deserialize(RagonBuffer serializer)
    {
      Damage = serializer.ReadUShort();
    }
  }
}