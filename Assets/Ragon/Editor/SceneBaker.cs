// FILE:Editor/SceneBaker.cs ROLE:assigns static entity ids to scene RagonLinks before a scene is saved DEPS:RagonLink;UnityEditor
// API:OnWillSaveAssets() ; GenerateSceneIds()
// WHY:scene entity ids are the server-side static entity keys; regenerating them on every save is intentional so ordering stays deterministic

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ragon.Client.Unity
{
  public class SceneBaker : AssetModificationProcessor
  {
    public static string[] OnWillSaveAssets(string[] paths)
    {
      string sceneName = string.Empty;
      foreach (string path in paths)
      {
        if (path.Contains(".unity"))
        {
          sceneName = Path.GetFileNameWithoutExtension(path);
          break;
        }
      }

      if (sceneName.Length == 0)
        return paths;

      GenerateSceneIds();

      return paths;
    }

    public static void GenerateSceneIds()
    {
      var gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
      var objs = new List<RagonLink>();
      foreach (var go in gameObjects)
      {
        var entities = go.GetComponentsInChildren<RagonLink>();
        objs.AddRange(entities);
      }

      Debug.Log("Found scene entities: " + objs.Count);

      ushort sceneId = 1;
      foreach (var entity in objs)
      {
        sceneId += 1;
        entity.SetStatic(sceneId);
        EditorUtility.SetDirty(entity);
      }
    }
  }
}