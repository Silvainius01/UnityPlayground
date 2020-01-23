using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum OBJ_POOL_TYPE
{
    STUN_SAVE_FX_SUCCESS, STUN_SAVE_FX_FAIL, DRIFT_HIT_FX, TESLA_LIGHTNING, TESLA_CHARING_FX, LANCE_HIT_FX,
    LIGHTNING_HIT_FX, GLORY_FX, COLL_FX, FORCE_SLAM_CHAIN_FX, FORCE_SLAM_BURN_FX, BOOST_FX, SHIP_EXPLOSION_FX,
    WARP_EXPLOSION_FX, PAINT_EXPLOSION_FX, SHIP_DEATH_FX, ASSIST_ORB_FX, SHIP_HEAL_FX, SHOCKWAVE_CARCASS, WORMHOLE_CARCASS, 
    LANCE_CARCASS, EASING_CAMERA, PLAYER_DEATH_MARKER, CIRCLE_SPAWN_FLASH, LIGHTNING_BALL_FX, LIGHTNING_BALL_DEATH_FX, LIGHTNING_BALL_SPAWN_FX,
    POINT_POPUP_FX, SHIP_DAMAGED_FX, THORN_PROJECTILE
}

public struct ObjPoolTypeEnumComparer : IEqualityComparer<OBJ_POOL_TYPE>
{
    public bool Equals(OBJ_POOL_TYPE x, OBJ_POOL_TYPE y)
    {
        return x == y;
    }

    public int GetHashCode(OBJ_POOL_TYPE obj)
    {
        // you need to do some thinking here,
        return (int)obj;
    }
}

public class ObjPoolManager : MonoBehaviour {

    private static ObjPoolManager _instance;
    public static ObjPoolManager instance
    {
        get
        {
            if (_instance == null && !applicationExiting)
            {
                // if no instance of object, create it
                GameObject obj = new GameObject("Obj Pools Manager");
                _instance = obj.AddComponent<ObjPoolManager>();
            }
            return _instance;
        }
    }

    private class ObjPoolInfo
    {
        public OBJ_POOL_TYPE type;
        public ObjectPool objPool;

        public ObjPoolInfo(OBJ_POOL_TYPE type, ObjectPool objPool, int minNum)
        {
            this.type = type;
            this.objPool = objPool;
        }
    }

    private static bool applicationExiting = false;
    private Dictionary<OBJ_POOL_TYPE, ObjectPool> objPoolDict = new Dictionary<OBJ_POOL_TYPE, ObjectPool>(new ObjPoolTypeEnumComparer());
    private Dictionary<OBJ_POOL_TYPE, ObjectPoolObj> objPoolWithCompDict = new Dictionary<OBJ_POOL_TYPE, ObjectPoolObj>(new ObjPoolTypeEnumComparer());

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
        }
    }

    public bool RegisterObjPool(ObjPoolRegister objPoolRegister)
    {
        ObjectPool objPool = null;
        if (!objPoolDict.TryGetValue(objPoolRegister.type, out objPool))
        {
            // if obj pool does not exist, create a new one and give it these objs
            objPool = new ObjectPool(objPoolRegister);
            objPool.poolGameObj.transform.SetParent(transform);
            objPoolDict.Add(objPoolRegister.type, objPool);
            return true;
        }
        else
        {
            // if object pool does exist give it the objs
            return false;
        }
    }

    public ObjectPool LoadObjPool(OBJ_POOL_TYPE type, string poolName, GameObject prefab, int minNum)
    {
        ObjectPool objPool = null;
        if (!objPoolDict.TryGetValue(type, out objPool))
        {
            // if no pool for the type, create one
            objPool = new ObjectPool(poolName, prefab, minNum, true, true);
            objPool.poolGameObj.transform.SetParent(transform);
            objPoolDict.Add(type, objPool);
        }
        return objPool;
    }

    public ObjectPool GetObjPool(OBJ_POOL_TYPE type)
    {
        ObjectPool objPool = null;
        if (objPoolDict.TryGetValue(type, out objPool))
        {
            return objPool;
        }
        return objPool;
    }

    public ObjectPool<T> LoadObjPoolWithType<T>(OBJ_POOL_TYPE type, string poolName, GameObject prefab, int minNum) where T : Component
    {
        ObjectPoolObj obj = null;
        ObjectPool<T> objPool = null;
        if (!objPoolWithCompDict.TryGetValue(type, out obj))
        {
            // if no pool for the type, create one
            objPool = new ObjectPool<T>(poolName, prefab, minNum, true, true);
            objPool.poolGameObj.transform.SetParent(transform);
            objPoolWithCompDict.Add(type, objPool.objPoolObj);
        }
        else
        {
            objPool = (ObjectPool<T>) obj.GetComponent<ObjectPoolObj>().objPool;
        }
        return objPool;
    }

    public ObjectPool<T> GetObjPoolWithType<T>(OBJ_POOL_TYPE type) where T : Component
    {
        ObjectPoolObj obj = null;
        ObjectPool<T> objPool = null;
        if (objPoolWithCompDict.TryGetValue(type, out obj))
        {
            objPool = (ObjectPool<T>)obj.GetComponent<ObjectPoolObj>().objPool;
            return objPool;
        }
        return objPool;
    }

    public void OnApplicationQuit()
    {
        applicationExiting = true;
    }
}
