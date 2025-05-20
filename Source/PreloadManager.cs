using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace InnerEigong;

/// <summary>
/// Handles preloading game objects from other scenes and fetching them.
/// </summary>
internal class PreloadManager {
    private static readonly Dictionary<string, ValueTuple<string, string>> PreloadedObjectMap = new() {
        ["Sniper"] = ("A5_S4_CastleMid_Remake_5wei", "A5_S4/Room/SniperTeleportGroup (1)/StealthGameMonster_Sniper (1)/MonsterCore/Animator(Proxy)/Animator")
    };

    private static Dictionary<string, GameObject> _preloadedObjects = new();

    /// <summary>
    /// Initialize the preload manager.
    /// </summary>
    internal static async UniTask Initialize() {
        await Preload();
    }
    
    /// <summary>
    /// Fetch a preloaded game object.
    /// </summary>
    /// <param name="key">The key of the preloaded game object.</param>
    /// <param name="obj">The output game object.</param>
    /// <returns>Whether the fetch operation was successful.</returns>
    internal static bool TryGet(string key, out GameObject? obj) {
        if (_preloadedObjects.TryGetValue(key, out obj)) {
            return true;
        }
        Log.Error($"Failed to fetch preloaded game object from key \"{key}\"!");
        return false;
    }

    private static async UniTask<bool> PreloadScene(string sceneName, Dictionary<string, string> objMap) {
        var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (loadOp == null) {
            Log.Error($"Error loading scene: {sceneName}");
            return false;
        }
        await loadOp;

        var scene = SceneManager.GetSceneByName(sceneName);
        var success = true;
        try {
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObj in rootObjects) {
                rootObj.SetActive(false);
            }

            foreach (var (key, objPath) in objMap) {
                var srcObj = GetGameObjectFromArray(rootObjects, objPath);
                if (srcObj != null) {
                    var objCopy = Object.Instantiate(srcObj);
                    objCopy.name = objCopy.name.Replace("(Clone)", "");
                    objCopy.SetActive(false);
                    Object.DontDestroyOnLoad(objCopy);
                    AutoAttributeManager.AutoReferenceAllChildren(objCopy);
                    if (!_preloadedObjects.TryAdd(key, objCopy)) {
                        Log.Error($"Failed to key {key} to preloaded game object {objCopy.name}!");
                    }
                } else {
                    Log.Error($"Failed to find game object in scene {sceneName} at path {objPath}!");
                    success = false;
                }
            }
        } catch (Exception e) {
            Log.Error(e);
        }

        var unloadOp = SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
        if (unloadOp == null) {
            Log.Error($"Error unloading scene: {sceneName}");
            return false;
        }
        return success;
    }

    private static async UniTask Preload() {
        DestroyAllGameObjects.DestroyingAll = true;
        
        Dictionary<string, Dictionary<string, string>> scenesToPaths = new();
        foreach (var (key, (sceneName, objPath)) in PreloadedObjectMap) {
            if (scenesToPaths.ContainsKey(sceneName)) {
                scenesToPaths[sceneName].Add(key, objPath);
            } else {
                scenesToPaths.Add(sceneName, new Dictionary<string, string> { [key] = objPath});
            }
        }
        foreach (var (sceneName, objMap) in scenesToPaths) {
            await PreloadScene(sceneName, objMap);
            await Resources.UnloadUnusedAssets();
        }
        DestroyAllGameObjects.DestroyingAll = false;
    }

    internal static void Unload() {
        foreach (var preloadedObj in _preloadedObjects.Values) {
            Object.DestroyImmediate(preloadedObj);
        }
        _preloadedObjects.Clear();
    }

    private static GameObject? GetGameObjectFromArray(GameObject[] objects, string objPath) {
        // Split object name into root and hcild names based on '/'
        string rootName;
        string? childName = null;

        var slashIndex = objPath.IndexOf('/');
        if (slashIndex == -1)
            rootName = objPath;
        else if (slashIndex == 0 || slashIndex == objPath.Length - 1)
            throw new ArgumentException($"Invalid GameObject path {objPath}!");
        else {
            rootName = objPath[..slashIndex];
            childName = objPath[(slashIndex + 1)..];
        }

        // Get root object
        var obj = objects.FirstOrDefault(o => o.name == rootName);
        if (obj is null) {
            return null;
        }

        // Get child object
        if (childName == null) return obj;


        var t = obj.transform.Find(childName);
        return !t ? null : t.gameObject;
    }
}