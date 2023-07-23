using Ragon.Client;
using Ragon.Protocol;

namespace Examples.Sources.Property
{
  public class CustomProperty: RagonProperty
  {
    public CustomProperty(int priority, bool invokeLocal) : base(priority, invokeLocal)
    {
    }

    public override void Serialize(RagonBuffer buffer)
    {
      buffer.WriteUShort(0);
      buffer.WriteUShort(10);
    }

    public override void Deserialize(RagonBuffer buffer)
    {
      buffer.ReadUShort();
    }
  }
}