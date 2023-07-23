using Ragon.Client;
using Ragon.Protocol;

namespace Examples.Sources.Payload
{
  public class CharacterPayload : IRagonPayload
  {
    public float X;
    public float Z;
    
    public void Serialize(RagonBuffer buffer)
    {
      buffer.WriteFloat(X, -1024f, 1024f, 0.1f);  
      buffer.WriteFloat(Z, -1024f, 1024f, 0.1f);  
    }

    public void Deserialize(RagonBuffer buffer)
    {
      X = buffer.ReadFloat(-1024f, 1024f, 0.1f);
      Z = buffer.ReadFloat(-1024f, 1024f, 0.1f);
    }
  }
}