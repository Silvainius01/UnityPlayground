using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NpadSpecificImage
{
    public EditorNpadStyle[] npadStyles;
    public Sprite actionSprite;
}

[System.Serializable]
public struct NpadSpecificActionImages
{
    public string name;
    public int actionID;
    public NpadSpecificImage[] npadStyleImages;
}

[System.Serializable]
public struct NpadStyleCategoryActionImages
{
    public string name;
    public int categoryID;
    public NpadSpecificActionImages[] actionImages;
}

[System.Serializable]
public enum EditorNpadStyle
{
    None = 0,
    FullKey = 1,
    Handheld = 2,
    JoyConDual = 4,
    JoyConLeft = 8,
    JoyConRight = 16,
    Invalid = 32
}

[CreateAssetMenu(fileName = "Platform_Data_Switch", menuName = "Platform_Data/Switch", order = 2)]
public class PlatformDataScriptObjSwitch : PlatformDataScriptObj
{
    public NpadStyleCategoryActionImages[] npadStyleCategoryActionImages;
}
