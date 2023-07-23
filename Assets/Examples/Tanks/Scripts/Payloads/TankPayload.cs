using Ragon.Client;
using Ragon.Protocol;
using UnityEngine;

namespace Ragon.Examples.Tanks
{
  public class TankPayload: IRagonPayload
  {
    public Vector3 Position;
    
    public void Serialize(RagonBuffer serializer)
    {
      serializer.WriteFloat(Position.x, -1024, 1024.0f, 0.1f);
      serializer.WriteFloat(Position.y, -1024, 1024.0f, 0.1f);
      serializer.WriteFloat(Position.z, -1024, 1024.0f, 0.1f);
    }

    public void Deserialize(RagonBuffer serializer)
    {
      Position.x = serializer.ReadFloat(-1024, 1024.0f, 0.1f);
      Position.y = serializer.ReadFloat(-1024, 1024.0f, 0.1f);
      Position.z = serializer.ReadFloat(-1024, 1024.0f, 0.1f);
    }
  }
}