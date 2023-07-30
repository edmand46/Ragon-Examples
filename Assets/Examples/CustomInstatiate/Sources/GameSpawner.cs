using Examples.Sources.Payload;
using Ragon.Client;
using Ragon.Client.Unity;
using UnityEngine;

namespace Examples.Sources
{
  public class GameSpawner: IRagonPrefabSpawner
  {
    public GameObject InstantiateEntityGameObject(RagonEntity entity, GameObject prefab)
    {
      if (prefab.TryGetComponent<Character>(out _))
      {
        var characterPayload = entity.GetAttachPayload<CharacterPayload>();
        var characterPosition = new Vector3(characterPayload.X, 0, characterPayload.Z);
        var characterGameObject = Object.Instantiate(prefab, characterPosition , Quaternion.identity);
        return characterGameObject;
      }

      return Object.Instantiate(prefab);
    }
  }
}