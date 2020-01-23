using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using System.Collections.Generic;
using System;

public class EventHandler<T> where T : new()
{
    public T data = new T();
    public delegate void EventDelegate(T data);
    private List<EventDelegate> callBacks = new List<EventDelegate>();

    public void StartListening(EventDelegate callBack)
    {
        callBacks.Add(callBack);
    }

    public void StopListening(EventDelegate callBack)
    {
        if (callBacks.Count == 0) return;
        callBacks.Remove(callBack);
    }

    public void Trigger(T data)
    {
        for (int i = 0; i < callBacks.Count; ++i) callBacks[i](data);
    }

    public void Trigger()
    {
        for (int i = 0; i < callBacks.Count; ++i) callBacks[i](data);
    }
}

public class EventManager : MonoBehaviour
{
	// General Object Events
	#region
	public class EDObjDestroyed
	{
		public GameObject obj;

        public EDObjDestroyed()
        {
        }

        public EDObjDestroyed(GameObject obj)
		{
            SetData(obj);
		}

        public void SetData(GameObject obj)
        {
            this.obj = obj;
        }
    }

	public class EDColliders2DChanged
	{
        public InteractionController interactionCont;
        public List<Collider2D> addedColliders;
        public List<Collider2D> removedColliders;
        public bool wereTriggers;

        public EDColliders2DChanged()
        {
            addedColliders = new List<Collider2D>();
            removedColliders = new List<Collider2D>();
            wereTriggers = false;
        }

        public EDColliders2DChanged(InteractionController interactionCont, List<Collider2D> addedColliders, List<Collider2D> removedColliders, bool wereTriggers)
		{
            SetData(interactionCont, addedColliders, removedColliders, wereTriggers);
		}

        public void SetData(InteractionController interactionCont, List<Collider2D> addedColliders, List<Collider2D> removedColliders, bool wereTriggers)
        {
            this.interactionCont = interactionCont;
            this.addedColliders = addedColliders == null ? new List<Collider2D>() : addedColliders;
            this.removedColliders = removedColliders == null ? new List<Collider2D>() : removedColliders;
            this.wereTriggers = wereTriggers;
        }
    }

	public class EDCollisionEnter
	{
		public Collider2D collider; // the collider that was hit
		public Collision2D collision2D; // the collision data from unity
		public RbodyCollController collisionCont; // saved collision info about vel, angular vel, and mass
		public InteractionController interactionCont; // interaction controller for recieving callbacks
		public InteractionController otherInteractionCont; // the interaction controller of the obj that was hit
		public CollisionEnterEventCB collisionCallback; // callback for the collision after basic handling is done
		public bool momentumCalcRequired;
		public bool flippedColl = false;
	}

	public class EDCollisionStay
	{
		public Collider2D collider; // the collider that was hit
		public Collision2D collision2D; // the collision data from unity
		public RbodyCollController collisionCont; // saved collision info about vel, angular vel, and mass
		public InteractionController interactionCont; // interaction controller for recieving callbacks
		public InteractionController otherInteractionCont; // the interaction controller of the obj that was hit
		public CollisionEventCB collisionCallback; // callback for the collision after basic handling is done
		public bool flippedColl = false;
	}

	public class EDCollisionExit
	{
		public Collider2D collider; // the collider that was hit
		public Collision2D collision2D; // the collision data from unity
		public RbodyCollController collisionCont; // saved collision info about vel, angular vel, and mass
		public InteractionController interactionCont; // interaction controller for recieving callbacks
		public InteractionController otherInteractionCont; // the interaction controller of the obj that was hit
		public CollisionEventCB collisionCallback; // callback for the collision after basic handling is done
		public bool flippedColl = false;
	}

	public class EDTriggerEnter
	{
		public Collider2D collider; // the collider that was hit
		public Collider2D eventCollider; // the collision data from unity
		public RbodyCollController collisionCont; // saved collision info about vel, angular vel, and mass
		public InteractionController interactionCont; // interaction controller for recieving callbacks
		public InteractionController otherInteractionCont; // the interaction controller of the obj that was hit
		public TriggerEnterEventCB triggerCallback; // callback for the collision after basic handling is done
        public bool wasCheckedOnEnable = false;
    }

	public class EDTriggerStay
	{
		public Collider2D collider; // the collider that was hit
		public Collider2D eventCollider; // the collision data from unity
		public RbodyCollController collisionCont; // saved collision info about vel, angular vel, and mass
		public InteractionController interactionCont; // interaction controller for recieving callbacks
		public InteractionController otherInteractionCont; // the interaction controller of the obj that was hit
		public TriggerEventCB triggerCallback; // callback for the collision after basic handling is done
	}

	public class EDTriggerExit
	{
		public Collider2D collider; // the collider that was hit
		public Collider2D eventCollider; // the collision data from unity
		public RbodyCollController collisionCont; // saved collision info about vel, angular vel, and mass
		public InteractionController interactionCont; // interaction controller for recieving callbacks
		public InteractionController otherInteractionCont; // the interaction controller of the obj that was hit
		public TriggerEventCB triggerCallback; // callback for the collision after basic handling is done
	}

	#endregion

	// event callback handlers    
	public EventHandler<EDObjDestroyed> handlerObjDestroyed { get; private set; }

	public EventHandler<EDColliders2DChanged> handlerColliders2DChanged { get; private set; }
	public EventHandler<EDCollisionEnter> handlerCollisionEnter { get; private set; }
	public EventHandler<EDCollisionStay> handlerCollisionStay { get; private set; }
	public EventHandler<EDCollisionExit> handlerCollisionExit { get; private set; }

	public EventHandler<EDTriggerEnter> handlerTriggerEnter { get; private set; }
	public EventHandler<EDTriggerStay> handlerTriggerStay { get; private set; }
	public EventHandler<EDTriggerExit> handlerTriggerExit { get; private set; }

	// private variables
	private bool initialized = false;
    public static bool applicationExiting = false;

    private static EventManager _instance;
    public static EventManager instance
    {
        get
        {
            if (_instance == null && !applicationExiting)
            {
                // if no instance of object, create it
                GameObject obj = new GameObject("EventManager");
                _instance = obj.AddComponent<EventManager>();
            }
            return _instance;
        }
    }

	private void Awake()
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
            Init();
            DontDestroyOnLoad(gameObject);
        }
    }

	void Init()
	{
		if (!initialized)
		{
			initialized = true;
			handlerObjDestroyed = new EventHandler<EDObjDestroyed>();

			handlerColliders2DChanged = new EventHandler<EDColliders2DChanged>();
			handlerCollisionEnter = new EventHandler<EDCollisionEnter>();
			handlerCollisionStay = new EventHandler<EDCollisionStay>();
			handlerCollisionExit = new EventHandler<EDCollisionExit>();

			handlerTriggerEnter = new EventHandler<EDTriggerEnter>();
			handlerTriggerStay = new EventHandler<EDTriggerStay>();
			handlerTriggerExit = new EventHandler<EDTriggerExit>();
		}
	}

    public void OnApplicationQuit()
    {
        applicationExiting = true;
    }
}
