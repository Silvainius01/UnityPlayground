//using Rewired.Platforms.Switch;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public struct ActionImages
{
    public string name;
    public int actionID;
    public Sprite actionSprite;
}

[System.Serializable]
[CreateAssetMenu(fileName = "Platform_Data_Default", menuName = "Platform_Data/Default", order = 1)]
public class PlatformDataScriptObj : ScriptableObject {

    [System.Serializable]
    public struct CategoryActionImages
    {

        public string name;
        public int categoryID;
        public ActionImages[] actionImages;
    }

    public CategoryActionImages[] categoryActionImages;
    public AssetBundlePathLoader settingsControlsLoader;
    public AssetBundlePathLoader controlSheetLoader;
}