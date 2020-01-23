//using Rewired.Platforms.Switch;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlatformManager : MonoBehaviour
{

    private static PlatformManager _instance;
    public static PlatformManager instance
    {
        get
        {
            if (_instance == null && !applicationExiting)
            {
                // if no instance of object, create it
                GameObject obj = new GameObject("Platform Manager");
                _instance = obj.AddComponent<PlatformManager>();
            }
            return _instance;
        }
    }

    private static bool applicationExiting = false;
    private bool isInitialized = false;

    private PlatformDataScriptObj platformData = null;
    private Dictionary<int, Dictionary<int, Sprite>> categoryActionImages = new Dictionary<int, Dictionary<int, Sprite>>();

    private GameObject lastSelected = null;

    public void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // if obj is a duplicate, destroy it
            Destroy(gameObject);
            return;
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // init save data manager
        SaveDataHandler.instance.Initialize();

        Debug.Log("platform manager awake");

        if (platformData == null)
            platformData = AssetBundleManager.LoadObjectFromBundle<PlatformDataScriptObj>("pc_platform/prefabs", "scriptobj_platform_pc", false);

        if (SceneManager.GetActiveScene().name != "_Intro_Scene")
            Init();
    }

    public void Init()
    {
        if (isInitialized) return;

        // load correct platform data for game

        AssetBundleManager.UnloadAssetBundle("pc_platform/prefabs", true);
        platformData = AssetBundleManager.LoadObjectFromBundle<PlatformDataScriptObj>("pc_platform/prefabs", "scriptobj_platform_pc");

		if (platformData == null)
		{
			Debug.LogError("Platform data bundle was not loaded!");
			return;
		}

        // load default action images into dicionary
        foreach (var categoryData in platformData.categoryActionImages)
        {
            Dictionary<int, Sprite> actionImages = new Dictionary<int, Sprite>();
            foreach (var actionImg in categoryData.actionImages)
            {
                if (!actionImages.ContainsKey(actionImg.actionID))
                    actionImages[actionImg.actionID] = actionImg.actionSprite;
            }
            if (!categoryActionImages.ContainsKey(categoryData.categoryID))
                categoryActionImages[categoryData.categoryID] = actionImages;
        }

        isInitialized = true;
    }

    public Sprite GetDefaultActionImage(int categoryID, int actionID)
    {
        Dictionary<int, Sprite> actionImages = null;
        if (categoryActionImages.TryGetValue(categoryID, out actionImages))
        {
            Sprite actionImg = null;
            if (actionImages.TryGetValue(actionID, out actionImg))
				return actionImg;
        }
        return null;
    }

    public GameObject GetSettingsControlsPrefab()
    {
        return platformData.settingsControlsLoader.LoadObject<GameObject>();
    }

    public GameObject GetControlSheetPrefab()
    {
        return platformData.controlSheetLoader.LoadObject<GameObject>();
    }
	
    public void Update()
    {
        var eventSystem = EventSystem.current;
        if(eventSystem != null)
        {
            if(eventSystem.currentSelectedGameObject == null && lastSelected != null)
            {
                eventSystem.SetSelectedGameObject(lastSelected);
            }
            else if(eventSystem.currentSelectedGameObject != null)
            {
                lastSelected = eventSystem.currentSelectedGameObject;
            }
        }
    }

    public void OnApplicationFocus(bool focus)
    {
        var eventSystem = EventSystem.current;
        if (focus)
        {
            // hide mouse whenever game is focused
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
			
            // select last select ui element on focus
            if (lastSelected != null && eventSystem != null)
                eventSystem.SetSelectedGameObject(lastSelected);
        }
    }
}
