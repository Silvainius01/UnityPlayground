using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjPoolRegister : MonoBehaviour {

    public OBJ_POOL_TYPE type;
    public GameObject prefab;
    public GameObject[] objs { get; private set; }
    //public 

    public void Awake()
    {
        // get all objs
        objs = new GameObject[transform.childCount];
        for(int i = 0; i < transform.childCount; ++i)
            objs[i] = transform.GetChild(i).gameObject;

        //var comp = objs[0].GetType();

        // inform obj pool manager that the pool exists
        bool success = ObjPoolManager.instance.RegisterObjPool(this);
        if (!success)
			Destroy(gameObject);
    }
}
