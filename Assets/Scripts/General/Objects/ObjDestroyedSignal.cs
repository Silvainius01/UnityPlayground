using UnityEngine;
using System.Collections;

public class ObjDestroyedSignal : MonoBehaviour, IResettable {
    void OnDestroy()
    {
        EventManager.instance.handlerObjDestroyed.data.SetData(gameObject);
        EventManager.instance.handlerObjDestroyed.Trigger();
    }

    public void ExecuteReset()
    {
        EventManager.instance.handlerObjDestroyed.data.SetData(gameObject);
        EventManager.instance.handlerObjDestroyed.Trigger();
    }
    public virtual void ReInit() { }
}
