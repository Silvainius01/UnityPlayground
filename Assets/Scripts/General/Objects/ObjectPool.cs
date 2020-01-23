using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObjPool
{
    void ObjPoolObjDestroyed(GameObject obj);
    bool IsObjPoolDeallocating();
    void Reset();
}

public class ObjectPool<T> : IObjPool where T : Component
{

    public class ObjectPoolNode<V> where V : Component
    {
        public ObjectPoolNode<V> next;
        public GameObject obj;
        public V comp;
        public float disableStartTime;
        public float disableTotalTime;
        public Coroutine timedDisableCoroutine = null;
        public bool poolAutoDestroyEnabled = true;

        public ObjectPoolNode(GameObject obj, ObjectPoolNode<V> next)
        {
            this.obj = obj;
            this.next = next;
            comp = obj.GetComponent<V>();
        }
    }

    private ObjectPoolNode<T> nextFreeNode;
    private Dictionary<int, ObjectPoolNode<T>> activeNodes = new Dictionary<int, ObjectPoolNode<T>>();
    public GameObject poolGameObj;
    private GameObject objPrefab;
    public ObjectPoolObj objPoolObj { get; private set; }
    private bool canExpand;
    public bool initialized { get; private set; }

    public ObjectPool(string poolName, GameObject objPrefab, int defaultPoolSize, bool canExpand, bool initObjs = true)
    {
        this.objPrefab = objPrefab;
        this.canExpand = canExpand;

        activeNodes = new Dictionary<int, ObjectPoolNode<T>>();
        poolGameObj = new GameObject(poolName);
        objPoolObj = poolGameObj.AddComponent<ObjectPoolObj>();
        objPoolObj.Init(this);

        if (initObjs)
            CreateInitialObjs(defaultPoolSize);
    }

    protected void CreateInitialObjs(int num)
    {
        initialized = true;
        for (int i = 0; i < num; ++i)
        {
            var node = CreateObjectPoolNode();
            nextFreeNode = node;
        }
    }

    public void DestroyPool()
    {
        // destroy objs in use
        foreach (var activeNode in activeNodes)
        {
            if (activeNode.Value.obj == null) continue;

            if (activeNode.Value.timedDisableCoroutine == null && activeNode.Value.poolAutoDestroyEnabled)
            {
                // if not a time disable and pool has destroy rights, destroy the object
                GameObject.Destroy(activeNode.Value.obj);
            }
            else if (activeNode.Value.timedDisableCoroutine != null)
            {
                // if it is a timed disable, stop disable coroutine and instead tell game to destroy obj in time remaining
                objPoolObj.StopCoroutine(activeNode.Value.timedDisableCoroutine);
                float timeLeft = activeNode.Value.disableTotalTime - (Time.time - activeNode.Value.disableStartTime);
                GameObject.Destroy(activeNode.Value.obj, Mathf.Max(0.0f, timeLeft));
            }
        }
        activeNodes.Clear();

        if (poolGameObj != null)
        {
            GameObject.Destroy(poolGameObj);
        }
        initialized = false;
    }

   public void SetInRootObj(string objPoolsName, string topLevelObjPoolName = "Obj Pools")
    {
        var rootObj = GameObject.Find(topLevelObjPoolName);
        if (rootObj == null) rootObj = new GameObject(topLevelObjPoolName);

        var internalObj = GameObject.Find(objPoolsName);
        if (internalObj == null) internalObj = new GameObject(objPoolsName);
        internalObj.transform.SetParent(rootObj.transform);
        poolGameObj.transform.SetParent(internalObj.transform);
    }

    private ObjectPoolNode<T> GetNode(float disableTime)
    {
        var node = GetNode();
        if (node != null)
        {
            node.timedDisableCoroutine = objPoolObj.StartCoroutine(ReturnObjCoroutine(node, disableTime));
            return node;
        }
        else return null;
    }

    private ObjectPoolNode<T> GetNode()
    {
        if (nextFreeNode != null)
        {
            // if there is a free node, return it
            ObjectPoolNode<T> node = nextFreeNode;
            activeNodes.Add(node.obj.GetInstanceID(), nextFreeNode);
            nextFreeNode = nextFreeNode.next;
            node.obj.SetActive(true);
            return node;
        }
        else if (canExpand)
        {
            // if there is not a free node, make one and return it
            ObjectPoolNode<T> newNode = CreateObjectPoolNode();
            activeNodes.Add(newNode.obj.GetInstanceID(), newNode);
            newNode.obj.gameObject.SetActive(true);
            return newNode;
        }
        return null;
    }

    public GameObject GetObj(bool autoDestroy = true)
    {
        var node = GetNode();
        node.poolAutoDestroyEnabled = autoDestroy;
        if (node != null) return node.obj;
        else return null;
    }

    public GameObject GetObj(float disableTime, bool autoDestroy = true)
    {
        var node = GetNode(disableTime);
        node.poolAutoDestroyEnabled = autoDestroy;
        if (node != null) return node.obj;
        else return null;
    }

    public T GetObjComp()
    {
        var node = GetNode();
        if (node != null) return node.comp;
        else return null;
    }

    public T GetObjComp(float disableTime)
    {
        var node = GetNode(disableTime);
        if (node != null) return node.comp;
        else return null;
    }

    public void ReturnPoolObjNextFrame(GameObject obj)
    {
        if (objPoolObj == null) return;
        ObjectPoolNode<T> foundNode = null;
        if (activeNodes.TryGetValue(obj.GetInstanceID(), out foundNode))
        {
            MiscUnityFunctions.StopCoroutine(objPoolObj, ref foundNode.timedDisableCoroutine);
            foundNode.timedDisableCoroutine = objPoolObj.StartCoroutine(ReturnObjNextFrameCoroutine(foundNode));
        }
    }

    public void DisablePoolObj(GameObject obj)
    {
        ObjectPoolNode<T> foundNode = null;
        if (activeNodes.TryGetValue(obj.GetInstanceID(), out foundNode))
        {
            foundNode.next = nextFreeNode;
            foundNode.obj.gameObject.SetActive(false);
            foundNode.obj.gameObject.transform.SetParent(poolGameObj.transform);
            foundNode.poolAutoDestroyEnabled = true;
            if (foundNode.timedDisableCoroutine != null)
            {
                foundNode.disableStartTime = foundNode.disableTotalTime = 0.0f;
                objPoolObj.StopCoroutine(foundNode.timedDisableCoroutine);
                foundNode.timedDisableCoroutine = null;
            }
            nextFreeNode = foundNode;
            activeNodes.Remove(foundNode.obj.GetInstanceID());
        }
    }

    public void DisablePoolObj(T comp)
    {
        DisablePoolObj(comp.gameObject);
    }

    protected virtual ObjectPoolNode<T> CreateObjectPoolNode()
    {
        if (!initialized) return null;

        // instantiate a new node and set it as the next free node
        GameObject objNode = GameObject.Instantiate(objPrefab) as GameObject;
        objNode.SetActive(false);
        objNode.transform.SetParent(poolGameObj.transform);
        objNode.transform.localPosition = new Vector3();
        ObjectPoolNode<T> node = new ObjectPoolNode<T>(objNode, nextFreeNode);
        ObjPoolNodeDestroyedSignal destroyedComp = objNode.AddComponent<ObjPoolNodeDestroyedSignal>();
        destroyedComp.Init(this);
        return node;
    }

    public List<T> GetEntirePoolList()
    {
        List<T> dataList = new List<T>(activeNodes.Count);
        foreach (var node in activeNodes)
            dataList.Add(node.Value.comp);
        ObjectPoolNode<T> currNode = nextFreeNode;
        while (currNode != null)
        {
            dataList.Add(currNode.comp);
            currNode = currNode.next;
        }
        return dataList;
    }

    public void Reset()
    {
        // mark all nodes as free and unactive
        foreach (var node in activeNodes)
        {
            node.Value.next = nextFreeNode;
            node.Value.obj.gameObject.SetActive(false);
            nextFreeNode = node.Value;
        }
        activeNodes.Clear();
    }

    public int NumActive()
    {
        return activeNodes.Count;
    }

    IEnumerator ReturnObjCoroutine(ObjectPoolNode<T> node, float time)
    {
        node.disableStartTime = Time.time;
        node.disableTotalTime = time;
        yield return new WaitForSeconds(time);
        DisablePoolObj(node.obj);
    }

    IEnumerator ReturnObjNextFrameCoroutine(ObjectPoolNode<T> node)
    {
        node.disableStartTime = Time.time;
        node.disableTotalTime = 0.0f;
        yield return null;
        DisablePoolObj(node.obj);
    }

    public void ObjPoolObjDestroyed(GameObject obj)
    {
        ObjectPoolNode<T> node = null;
        if (activeNodes.TryGetValue(obj.GetInstanceID(), out node))
        {
            if (node.timedDisableCoroutine != null && objPoolObj != null)
            {
                objPoolObj.StopCoroutine(node.timedDisableCoroutine);
                node.timedDisableCoroutine = null;
            }
            activeNodes.Remove(node.obj.GetInstanceID());
        }
    }

    public bool IsObjPoolDeallocating()
    {
        return !initialized;
    }
}

public class ObjectPool : IObjPool
{

    public class ObjectPoolNode
    {
        public ObjectPoolNode next;
        public GameObject obj;
        public float disableStartTime;
        public float disableTotalTime;
        public Coroutine timedDisableCoroutine = null;
        public bool poolAutoDestroyEnabled = true;

        public ObjectPoolNode(GameObject obj, ObjectPoolNode next)
        {
            this.obj = obj;
            this.next = next;
        }
    }

    private ObjectPoolNode nextFreeNode;
    private Dictionary<int, ObjectPoolNode> activeNodesDict = new Dictionary<int, ObjectPoolNode>();
    public GameObject poolGameObj;
    public ObjectPoolObj objPoolObj { get; private set; }
    private GameObject objPrefab;
    private bool canExpand;
    public int numObjs { get; private set; }
    public bool initialized { get; private set; }

    public ObjectPool(string poolName, GameObject objPrefab, int defaultPoolSize, bool canExpand, bool initObjs = true)
    {
        this.objPrefab = objPrefab;
        this.canExpand = canExpand;
		
        activeNodesDict = new Dictionary<int, ObjectPoolNode>(defaultPoolSize);
        poolGameObj = new GameObject(poolName);
        objPoolObj = poolGameObj.AddComponent<ObjectPoolObj>();
        objPoolObj.Init(this);

        if (initObjs)
            CreateInitialObjs(defaultPoolSize);
    }

    public ObjectPool(ObjPoolRegister existingObjPool)
    {
        this.objPrefab = existingObjPool.prefab;
        this.canExpand = true;

        activeNodesDict = new Dictionary<int, ObjectPoolNode>(existingObjPool.objs.Length);
        poolGameObj = existingObjPool.gameObject;
        objPoolObj = poolGameObj.AddComponent<ObjectPoolObj>();
        objPoolObj.Init(this);

        // init objs in linked list
        initialized = true;
        for(int i = 0; i < existingObjPool.objs.Length; ++i)
        {
            var objNode = existingObjPool.objs[i];
            objNode.SetActive(false);
            ObjectPoolNode node = new ObjectPoolNode(objNode, nextFreeNode);
            ObjPoolNodeDestroyedSignal destroyedComp = objNode.GetComponent<ObjPoolNodeDestroyedSignal>();
            if(destroyedComp == null) destroyedComp = objNode.AddComponent<ObjPoolNodeDestroyedSignal>();
            destroyedComp.Init(this);
            ++numObjs;
            nextFreeNode = node;
        }
    }

    protected void CreateInitialObjs(int num)
    {
        initialized = true;
        for (int i = 0; i < num; ++i)
        {
            var node = CreateNewNode();
            nextFreeNode = node;
        }
    }

    public void DestroyPool()
    {
        initialized = false;

        // destroy objs in use
        foreach (var activeNode in activeNodesDict)
        {
            if (activeNode.Value.obj == null) continue;

            if (activeNode.Value.timedDisableCoroutine == null && activeNode.Value.poolAutoDestroyEnabled)
            {
                // if not a time disable and pool has destroy rights, destroy the object
                GameObject.Destroy(activeNode.Value.obj);
            }
            else if (activeNode.Value.timedDisableCoroutine != null)
            {
                // if not getting destroyed, make sure not a child of obj pool
                if (activeNode.Value.obj.transform.parent == poolGameObj.transform)
                    activeNode.Value.obj.transform.SetParent(null);

                // if it is a timed disable, stop disable coroutine and instead tell game to destroy obj in time remaining
                objPoolObj.StopCoroutine(activeNode.Value.timedDisableCoroutine);
                float timeLeft = activeNode.Value.disableTotalTime - (Time.time - activeNode.Value.disableStartTime);
                GameObject.Destroy(activeNode.Value.obj, timeLeft);
            }
            else
            {
                // if not getting destroyed, make sure not a child of obj pool
                if (activeNode.Value.obj.transform.parent == poolGameObj.transform)
                    activeNode.Value.obj.transform.SetParent(null);
            }
        }
        activeNodesDict.Clear();

        if (poolGameObj != null)
        {
            GameObject.Destroy(poolGameObj);
        }
    }

    public void SetInRootObj(string objPoolsName, string topLevelObjPoolName = "Obj Pools")
    {
        var rootObj = GameObject.Find(topLevelObjPoolName);
        if (rootObj == null) rootObj = new GameObject(topLevelObjPoolName);

        var internalObj = GameObject.Find(objPoolsName);
        if (internalObj == null) internalObj = new GameObject(objPoolsName);
        internalObj.transform.SetParent(rootObj.transform);
        poolGameObj.transform.SetParent(internalObj.transform);
    }

    private ObjectPoolNode GetNode(float disableTime)
    {
        var node = GetNode();
        if (node != null)
        {
            node.timedDisableCoroutine = objPoolObj.StartCoroutine(ReturnObjCoroutine(node, disableTime));
            return node;
        }
        else return null;
    }

    private ObjectPoolNode GetNode()
    {
        if (!initialized) return null;

        if (nextFreeNode != null)
        {
            // if there is a free node, return it
            ObjectPoolNode node = nextFreeNode;
            activeNodesDict.Add(node.obj.GetInstanceID(), nextFreeNode);
            nextFreeNode = nextFreeNode.next;
            node.obj.SetActive(true);
            return node;
        }
        else if (canExpand)
        {
            // if there is not a free node, make one and return it
            ObjectPoolNode newNode = CreateNewNode();
            activeNodesDict.Add(newNode.obj.GetInstanceID(), newNode);
            newNode.obj.gameObject.SetActive(true);
            return newNode;
        }
        return null;
    }

    public GameObject GetObj(bool autoDestroy = true)
    {
        var node = GetNode();
        node.poolAutoDestroyEnabled = autoDestroy;
        if (node != null) return node.obj;
        else return null;
    }

    public GameObject GetObj(float disableTime, bool autoDestroy = true)
    {
        var node = GetNode(disableTime);
        node.poolAutoDestroyEnabled = autoDestroy;
        if (node != null) return node.obj;
        else return null;
    }


    public void DisablePoolObj(GameObject obj)
    {
        ObjectPoolNode foundNode = null;
        if(activeNodesDict.TryGetValue(obj.GetInstanceID(), out foundNode))
        {
            foundNode.next = nextFreeNode;
            foundNode.obj.gameObject.SetActive(false);
            foundNode.obj.gameObject.transform.SetParent(poolGameObj.transform);
            foundNode.poolAutoDestroyEnabled = true;
            if (foundNode.timedDisableCoroutine != null)
            {
                foundNode.disableStartTime = foundNode.disableTotalTime = 0.0f;
                objPoolObj.StopCoroutine(foundNode.timedDisableCoroutine);
                foundNode.timedDisableCoroutine = null;
            }
            nextFreeNode = foundNode;
            activeNodesDict.Remove(foundNode.obj.GetInstanceID());
        }
    }

    protected virtual ObjectPoolNode CreateNewNode()
    {
        if (!initialized) return null;

        // instantiate a new node and set it as the next free node
        GameObject objNode = GameObject.Instantiate(objPrefab) as GameObject;
        objNode.SetActive(false);
        objNode.transform.SetParent(poolGameObj.transform);
        objNode.gameObject.transform.localPosition = new Vector3();
        ObjectPoolNode node = new ObjectPoolNode(objNode, nextFreeNode);
        ObjPoolNodeDestroyedSignal destroyedComp = objNode.AddComponent<ObjPoolNodeDestroyedSignal>();
        destroyedComp.Init(this);
        ++numObjs;
        return node;
    }

    public void Reset()
    {
        // mark all nodes as free and unactive
        foreach (var node in activeNodesDict)
        {
            node.Value.next = nextFreeNode;
            node.Value.obj.gameObject.SetActive(false);
            nextFreeNode = node.Value;
        }
        activeNodesDict.Clear();
    }

    public int NumActive()
    {
        return activeNodesDict.Count;
    }

    IEnumerator ReturnObjCoroutine(ObjectPoolNode node, float time)
    {
        node.disableStartTime = Time.time;
        node.disableTotalTime = time;
        yield return new WaitForSeconds(time);
        DisablePoolObj(node.obj);
    }

    public void ObjPoolObjDestroyed(GameObject obj)
    {
        ObjectPoolNode node = null;
        if(activeNodesDict.TryGetValue(obj.GetInstanceID(), out node))
        {
            if (node.timedDisableCoroutine != null)
            {
                if(objPoolObj != null)
                    objPoolObj.StopCoroutine(node.timedDisableCoroutine);
                node.timedDisableCoroutine = null;
            }
            activeNodesDict.Remove(node.obj.GetInstanceID());
            --numObjs;
        }
    }

    public bool IsObjPoolDeallocating()
    {
        return !initialized;
    }
}