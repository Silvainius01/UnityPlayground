using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjPoolNodeDestroyedSignal : MonoBehaviour
{
    private IObjPool pool;

    public void Init(IObjPool pool)
    {
        this.pool = pool;
    }

    public void OnDestroy()
    {
        if (pool != null)
        {
            if (!pool.IsObjPoolDeallocating())
            {
                pool.ObjPoolObjDestroyed(gameObject);
            }
        }
    }
}
