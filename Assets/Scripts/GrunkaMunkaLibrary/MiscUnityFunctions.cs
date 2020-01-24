using UnityEngine;
using System.Collections.Generic;

public static class MiscUnityFunctions
{
    public static List<GameObject> AddFromHierarchy(string baseName)
    {
        int count = 0;
        GameObject obj = GameObject.Find(baseName + " (0)");
        List<GameObject> retval = new List<GameObject>();

        while (obj != null)
        {
            retval.Add(obj);
            obj = GameObject.Find(baseName + " (" + (++count).ToString() + ")");
        }

        return retval;
    }

    public static List<GameObject> AddFromHierarchy(string baseName, Transform parent)
    {
		if (parent == null)
		{
			Debug.LogWarning("Passed parent is null! Aborting.");
			return new List<GameObject>();
		}

		int count = 0;
        Transform obj = parent.Find(baseName + " (0)");
        List<GameObject> retval = new List<GameObject>();

        while (obj != null)
        {
            retval.Add(obj.gameObject);
            obj = parent.Find(baseName + " (" + (++count).ToString() + ")");
        }

        if (retval.Count == 0)
            Debug.LogWarning("'AddFromHierarchy' could not find any numbered instance of '" + baseName + "'!");

        return retval;
    }

	public static Vector2 GetScreenSpaceCoord(Vector2 worldSpaceCoord, Canvas targetCanvas)
	{
		RectTransform cRect = targetCanvas.GetComponent<RectTransform>();
		Vector2 uiOffset = new Vector2(cRect.sizeDelta.x / 2f, cRect.sizeDelta.y / 2f);
		Vector2 viewportPos = Camera.main.WorldToViewportPoint(worldSpaceCoord);
		Vector2 proportionalPosition = new Vector2(viewportPos.x * cRect.sizeDelta.x, viewportPos.y * cRect.sizeDelta.y);
		
		return proportionalPosition - uiOffset;
	}

    public static float GetColorBrightness(Color color)
    {
        return color.r * 0.3f + color.g * 0.59f + color.b * 0.11f;
    }

    public static void SetColorBrightness(ref Color color, float brightness)
    {
        float currBrightness = GetColorBrightness(color);
        float precentChange = brightness / currBrightness;
        color.r = color.r * precentChange;
        color.g = color.g * precentChange;
        color.b = color.b * precentChange;
    }

    public static void StopCoroutine(MonoBehaviour obj, ref Coroutine coroutine)
    {
        if(coroutine != null)
        {
            if(obj.isActiveAndEnabled)
                obj.StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    public static System.Guid GetFMODSoundEventID(string soundEvent)
    {
		return new System.Guid();// FMODUnity.RuntimeManager.PathToGUID(soundEvent);
    }

    public static System.Guid GetFMODSoundEventIDAndClear(ref string soundEvent)
    {
        //System.Guid id = FMODUnity.RuntimeManager.PathToGUID(soundEvent);
        //soundEvent = null;
		return new System.Guid();// id;
    }
	
}
