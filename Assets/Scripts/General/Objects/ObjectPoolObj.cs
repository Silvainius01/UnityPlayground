using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolObj : MonoBehaviour {
    public IObjPool objPool { get; private set; }
    public void Init(IObjPool objPool)
    {
        this.objPool = objPool;
    }
}
