using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class UIUtiliity {

    public static Vector2 GetImageSize(Image img, bool scaled = false)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        if (scaled)
            return new Vector2(rect.rect.width * rect.transform.lossyScale.x, rect.rect.height * rect.transform.lossyScale.y);
        return rect.rect.size;
    }

    public static Vector2 GetUISize(GameObject ui, bool scaled = false)
    {
        RectTransform rect = ui.GetComponent<RectTransform>();
        if (scaled)
            return new Vector2(rect.rect.width * rect.transform.lossyScale.x, rect.rect.height * rect.transform.lossyScale.y);
        return rect.rect.size;
    }

    public static Vector2 GetImagePos(Image img)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        return rect.anchoredPosition;
    }

    public static void SetImagePos(Image img, Vector2 pos)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;
    }

    public static void SetImageSize(Image img, Vector2 size)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        rect.sizeDelta = size;
    }

    public static void SetUIAnchoredPos(GameObject obj, Vector2 pos)
    {
        obj.GetComponent<RectTransform>().anchoredPosition = pos;
    }

    public static Vector2 GetUIAnchoredPos(GameObject obj)
    {
        return obj.GetComponent<RectTransform>().anchoredPosition;
    }

    public static Vector2 GetUIPos(GameObject obj)
    {
        return obj.GetComponent<RectTransform>().position;
    }

    public static Coroutine SelectUIAfterFrame(MonoBehaviour obj, Selectable selectable)
    {
        return obj.StartCoroutine(SelectUIAfterFrameCoroutine(selectable));
    }

    public static IEnumerator SelectUIAfterFrameCoroutine(Selectable selectable)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        yield return null;
        selectable.Select();
    }
}
