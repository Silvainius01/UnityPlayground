using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct AssetBundlePathLoader
{
    public string assetBundlePath;
    public string assetName;

    public T LoadObject<T>() where T : Object
    {
        return AssetBundleManager.LoadObjectFromBundle<T>(assetBundlePath, assetName, true);
    }
}

public class AssetBundleManager : MonoBehaviour
{
	static AssetBundleManager instance;

	public static string bundlePath;
	public delegate void ReceiveObjectDelegate<T>(T obj, int assetKey);
	AssetBundleManifest manifest;
	Dictionary<string, AssetBundle> bundleDict = new Dictionary<string, AssetBundle>();
	Dictionary<string, Coroutine> loadingBundles = new Dictionary<string, Coroutine>();

	void Awake()
	{
        if (instance == null || instance == this)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

		string platform = "";

		switch (Application.platform)
		{
			case RuntimePlatform.Switch:
				platform = "Switch"; break;
			default:
				platform = "Windows"; break;
		}

		bundlePath = Application.streamingAssetsPath + "/AssetBundles/" + platform + "/";
		manifest = LoadObjectFromBundle<AssetBundleManifest>(platform, "AssetBundleManifest", false);

        SceneManager.sceneLoaded += OnSceneLoaded;
        UpdateLoadedAssetBundleTracking();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateLoadedAssetBundleTracking();
    }

	/// <summary> Loads an asset bundle. Returns true if succesfull or already loaded. Otherwise false. </summary>
    public static bool LoadAssetBundle(string bundleName, bool loadAllDependencies = true)
	{
		if (instance.bundleDict.ContainsKey(bundleName))
			return true;
		try
		{
			instance.bundleDict.Add(bundleName, AssetBundle.LoadFromFile(bundlePath + bundleName));

			if (instance.manifest != null)
			{
				if (loadAllDependencies)
				{
					foreach (var dep in instance.manifest.GetAllDependencies(bundleName))
					{
						LoadAssetBundle(dep);
					}
				}
			}
			return true;
		}
		catch(System.Exception e)
		{
			Debug.Log(e);
			return false;
		}
	}

    public static void LoadAssetBundlesInDirectoryAsync(string directory, bool lockLoadingScreen, out Coroutine cr, bool loadAllDependencies = true)
    {
        cr = instance.StartCoroutine(instance.LoadAllAssetBundlesInDirectoryCR(directory, lockLoadingScreen, loadAllDependencies));
    }

    IEnumerator LoadAllAssetBundlesInDirectoryCR(string directory, bool lockLoadingScreen, bool loadAllDependencies = true)
    {
        var allBundles = GetAllAssetBundleNames();
        foreach (var bundleName in allBundles)
        {
            if (bundleName.StartsWith(directory))
            {
                Coroutine cr;
                LoadAssetBundleAsync(bundleName, lockLoadingScreen, out cr, loadAllDependencies);
                if(cr != null)
                {
                    yield return cr;
                }
            }
        }
    }


	public static void LoadAssetBundleAsync(string bundleName, bool lockLoadingScreen, out Coroutine cr, bool loadAllDependencies = true)
	{
		cr = null;

        // if bundle already loaded, return
		if (instance.bundleDict.ContainsKey(bundleName))
			return;

        // if bundle already loading async, wait on current async load
		if (instance.loadingBundles.ContainsKey(bundleName))
		{
			cr = instance.loadingBundles[bundleName];
			return;
		}

        // if not already loaded or  loading, start bundle load coroutine
        string key = "";
		//if (lockLoadingScreen)
		//{
		//	var loadingScreen = LoadingScreen.GetActiveLoadingScreen();
		//	if (loadingScreen != null)
		//	{
		//		key = bundleName + instance.GetInstanceID();
		//		loadingScreen.SetLoadingLocker(key);
		//		cr = instance.StartCoroutine(instance.LoadAssetBundleCR(bundleName, key, lockLoadingScreen, loadAllDependencies));
		//	}
		//}
		//else
			cr = instance.StartCoroutine(instance.LoadAssetBundleCR(bundleName, key, lockLoadingScreen, loadAllDependencies));

        // mark bundle as loading async
		instance.loadingBundles.Add(bundleName, cr);
	}
	IEnumerator LoadAssetBundleCR(string name, string key, bool lockLoadingScreen, bool loadAllDependencies = true)
	{
        // load the desired asset bundle and wait for it to complete
        string uri = "file:///" + bundlePath + name;
        UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(uri, 0);
		yield return request.SendWebRequest();
        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);

        // store the loaded asset bundle
		if (!bundleDict.ContainsKey(name))
			bundleDict.Add(name, bundle);
		else if (bundleDict[name] == null)
			bundleDict[name] = bundle;

		if (manifest != null)
        {
            if (loadAllDependencies)
            {
                // load all dependencies for asset bundle if requested
			    foreach (var dep in manifest.GetDirectDependencies(name))
			    {
				    Coroutine c = null;
				    LoadAssetBundleAsync(dep, lockLoadingScreen, out c);
				    yield return c;
                }
            }
        }

        // mark bundle as no longer loading
		if (loadingBundles.ContainsKey(name))
			loadingBundles.Remove(name);

        // if behind loading screen remove locker
		//if (lockLoadingScreen)
		//	LoadingScreen.GetActiveLoadingScreen().RemoveLoadingLocker(key);
	}

	public static T[] LoadAllObjectsInBundle<T>(string bundleName) where T : Object
	{
		if (!instance.bundleDict.ContainsKey(bundleName) && !LoadAssetBundle(bundleName))
			return null;
		return instance.bundleDict[bundleName].LoadAllAssets<T>();
	}
	public static T LoadObjectFromBundle<T>(string bundleName, string assetName, bool loadAllDependencies = true) where T : Object
	{
		if (!instance.bundleDict.ContainsKey(bundleName) && !LoadAssetBundle(bundleName, loadAllDependencies))
			return null;
		return instance.bundleDict[bundleName].LoadAsset<T>(assetName);
	}
    public static Object LoadObjectFromBundle(string bundleName, string assetName, bool loadAllDependencies = true)
    {
        if (!instance.bundleDict.ContainsKey(bundleName))
            LoadAssetBundle(bundleName, loadAllDependencies);
        return instance.bundleDict[bundleName].LoadAsset(assetName);
    }
    public static Coroutine LoadAllObjectsInBundleAsync<T>(string bundleName, ReceiveObjectDelegate<T[]> receiver, bool lockLoadingScreen, int assetKey) where T : Object
	{
		string key = "";
		//if (lockLoadingScreen)
		//{
		//	var loadingScreen = LoadingScreen.GetActiveLoadingScreen();
		//	if (loadingScreen != null)
		//	{
		//		key = bundleName + instance.GetInstanceID();
		//		loadingScreen.SetLoadingLocker(key);
		//		return instance.StartCoroutine(instance.LoadAllAssetsFromBundleCR(bundleName, receiver, key, lockLoadingScreen, assetKey));
		//	}
		//}
		return instance.StartCoroutine(instance.LoadAllAssetsFromBundleCR(bundleName, receiver, key, lockLoadingScreen, assetKey));
	}
	/// <summary> Loads an asset from a bundle. Sends a callback w/ loaded object. </summary>
	/// <param name="lockLoadingScreen">Requires and locks an active loading screen if true. Will simply load async if false.</param>
	public static Coroutine LoadObjectFromBundleAsync<T>(string bundleName, string assetName, ReceiveObjectDelegate<T> receiver, int assetKey, bool lockLoadingScreen) where T : Object
	{
		string key = "";

		//if (lockLoadingScreen)
		//{
		//	var loadingScreen = LoadingScreen.GetActiveLoadingScreen();
		//	if (loadingScreen != null)
		//	{
		//		key = bundleName + instance.GetInstanceID();
		//		loadingScreen.SetLoadingLocker(key);
		//		return instance.StartCoroutine(instance.LoadAssetFromBundleCR(bundleName, assetName, receiver, key, lockLoadingScreen, assetKey));
		//	}
		//}
		return instance.StartCoroutine(instance.LoadAssetFromBundleCR(bundleName, assetName, receiver, key, lockLoadingScreen, assetKey));
	}

    public static Coroutine LoadAllObjectsInBundleAsync(string bundleName, bool lockLoadingScreen)
    {
        string key = "";
        //if (lockLoadingScreen)
        //{
        //    var loadingScreen = LoadingScreen.GetActiveLoadingScreen();
        //    if (loadingScreen != null)
        //    {
        //        key = bundleName + instance.GetInstanceID();
        //        loadingScreen.SetLoadingLocker(key);
        //        return instance.StartCoroutine(instance.LoadAllAssetsFromBundleCR(bundleName, key, lockLoadingScreen));
        //    }
        //}
        return instance.StartCoroutine(instance.LoadAllAssetsFromBundleCR(bundleName, key, lockLoadingScreen));
    }

    IEnumerator LoadAssetFromBundleCR<T>(string bundleName, string assetName, ReceiveObjectDelegate<T> sendEvent, string key, bool lockLoadingScreen, int assetKey) where T : Object
	{
		bool load = true;
		if (!bundleDict.ContainsKey(bundleName))
		{
			Coroutine cr = null;
			LoadAssetBundleAsync(bundleName, lockLoadingScreen, out cr);
			load = cr != null;
			yield return cr;
		}

		if (load)
		{
			var req = bundleDict[bundleName].LoadAssetAsync<T>(assetName);
			yield return req; // Wait for request to finish

			// Send object out
			T obj = req.asset as T;
			sendEvent(obj, assetKey);
		}
		else Debug.LogError("Could not load '" + assetName + "' from bundle '" + bundleName);

		//if (lockLoadingScreen)
		//	LoadingScreen.GetActiveLoadingScreen().RemoveLoadingLocker(key);
	}
	IEnumerator LoadAllAssetsFromBundleCR<T>(string bundleName, ReceiveObjectDelegate<T[]> sendEvent, string key, bool lockLoadingScreen, int assetKey) where T : Object
	{
		bool load = true;
		if (!bundleDict.ContainsKey(bundleName))
		{
			Coroutine cr = null;
			LoadAssetBundleAsync(bundleName, lockLoadingScreen, out cr);
			load = cr != null;
			yield return cr;
		}

		if (load)
		{
			var req = bundleDict[bundleName].LoadAllAssetsAsync<T>();
			yield return req; // Wait for request to finish

			// Send object out
			var obj = req.allAssets as T[];
            if(sendEvent != null)
			    sendEvent(obj, assetKey);
		}
		else Debug.LogError("Could not load assets from bundle '" + bundleName);

		//if (lockLoadingScreen)
		//	LoadingScreen.GetActiveLoadingScreen().RemoveLoadingLocker(key);
	}
    IEnumerator LoadAllAssetsFromBundleCR(string bundleName, string key, bool lockLoadingScreen)
    {
        bool load = true;
        if (!bundleDict.ContainsKey(bundleName))
        {
            Coroutine cr = null;
            LoadAssetBundleAsync(bundleName, lockLoadingScreen, out cr);
            load = cr != null;
            yield return cr;
        }

        if (load)
        {
            var req = bundleDict[bundleName].LoadAllAssetsAsync();
            yield return req; // Wait for request to finish
        }
        else Debug.LogError("Could not load assets from bundle '" + bundleName);

        //if (lockLoadingScreen)
        //    LoadingScreen.GetActiveLoadingScreen().RemoveLoadingLocker(key);
    }

    /// <summary> Unloads a specific bundle. </summary>
    /// <param name="bundleName">Name of bundle.</param>
    /// <param name="unloadObjects">Do we also unload all objects we've loaded?</param>
    public static void UnloadAssetBundle(string bundleName, bool unloadObjects)
	{
		if (instance.bundleDict.ContainsKey(bundleName))
        {
            instance.bundleDict[bundleName].Unload(unloadObjects);
            instance.bundleDict.Remove(bundleName);
        }
	}

    public static void UnloadAssetBundlesInDirectory(string directory, bool unloadObjects)
    {
        var allBundles = GetAllAssetBundleNames();
        foreach(var bundle in allBundles)
        {
            if (bundle.StartsWith(directory))
            {              
                UnloadAssetBundle(bundle, unloadObjects);
            }
        }
    }

	/// <summary> Unloads all asset bundles AND all objects associated with them. USE WITH CAUTION </summary>
	public static void UnloadAllBundles()
	{
		foreach (var kvp in instance.bundleDict)
			kvp.Value.Unload(true);
		instance.bundleDict.Clear();
	}

    public static void UpdateLoadedAssetBundleTracking()
    {
        var allLoadedAssetBundles = AssetBundle.GetAllLoadedAssetBundles();
        foreach(var loadedBundle in allLoadedAssetBundles)
        {
            if (!instance.bundleDict.ContainsKey(loadedBundle.name) && loadedBundle.name != "")
            {
                instance.bundleDict.Add(loadedBundle.name, loadedBundle);
            }
        }
    }

    public static string[] GetAllAssetBundleNames()
    {
        return instance.manifest.GetAllAssetBundles();
    }

    public static bool IsAssetBundleLoaded(string bundleName)
    {
        return instance.bundleDict.ContainsKey(bundleName);
    }

    public void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}