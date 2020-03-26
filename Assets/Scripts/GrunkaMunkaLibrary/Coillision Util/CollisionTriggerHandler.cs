using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CollisionAndTriggerInfoBundle
{
    // each interaction controller get info about collisions and triggers with each other interaction controller
    public InteractionController interactionCont;
    public Dictionary<InteractionController, CollisionAndTriggerInfo> objBasedInteractionsDict;
    // each interaction controller also get to know about all events by type
    public CollisionAndTriggerInfo eventBasedInteractions;

    public CollisionAndTriggerInfoBundle(InteractionController interactionCont)
    {
        this.interactionCont = interactionCont;
        objBasedInteractionsDict = new Dictionary<InteractionController, CollisionAndTriggerInfo>();
        eventBasedInteractions = new CollisionAndTriggerInfo();
    }
}

public class CollisionAndTriggerInfo
{
    // list of all different types of interactions
    public List<CollisionEnterData> collisionEnterData;
    public List<CollisionData> collisionStayData;
    public List<CollisionData> collisionExitData;
    public List<TriggerEnterData> triggerEnterData;
    public List<TriggerStayExitData> triggerStayData;
    public List<TriggerStayExitData> triggerExitData;

    public CollisionAndTriggerInfo()
    {
        collisionEnterData = new List<CollisionEnterData>();
        collisionStayData = new List<CollisionData>();
        collisionExitData = new List<CollisionData>();
        triggerEnterData = new List<TriggerEnterData>();
        triggerStayData = new List<TriggerStayExitData>();
        triggerExitData = new List<TriggerStayExitData>();
    }
}

// phsyics event callbacks
public delegate void CollisionEnterEventCB(CollisionEnterData collisionEnterData, CollisionAndTriggerInfoBundle allCollisionTriggerInfo);
public delegate void CollisionEventCB(CollisionData collisionData, CollisionAndTriggerInfoBundle allCollisionTriggerInfo);
public delegate void TriggerEnterEventCB(TriggerEnterData triggerData, CollisionAndTriggerInfoBundle allCollisionTriggerInfo);
public delegate void TriggerEventCB(TriggerStayExitData triggerData, CollisionAndTriggerInfoBundle allCollisionTriggerInfo);

// class is basically the Collision2D class, but allows for flipped normals and relative velocity
public class Coll2DContactPoint
{
    public Collider2D collider { get { return flipped ? contactPoint.otherCollider : contactPoint.collider; } }
    public Collider2D otherCollider { get { return flipped ? contactPoint.collider : contactPoint.otherCollider; } }
    public Vector2 point { get { return contactPoint.point; } }
    public Vector2 normal { get { return flipped ? -contactPoint.normal : contactPoint.normal; } }

    private ContactPoint2D contactPoint;
    private bool flipped;

    public Coll2DContactPoint(ContactPoint2D contactPoint, bool flipped = false)
    {
        this.flipped = flipped;
        this.contactPoint = contactPoint;
    }
}

public class Coll2D
{
    public Coll2DContactPoint[] contacts { get; private set; }
    public Collider2D collider { get; private set; }
    public Vector2 relativeVelocity { get; private set; }

    public Coll2D(Collider2D hitCollider, Collision2D coll, bool flipped = false)
    {
        collider = hitCollider;
        relativeVelocity = flipped ? -coll.relativeVelocity : coll.relativeVelocity;
        // save contact points
        ContactPoint2D[] unityContacts = new ContactPoint2D[3];
        int num = coll.GetContacts(unityContacts);
        contacts = new Coll2DContactPoint[num];
        for(int i = 0; i < num; ++i)
        {
            contacts[i] = new Coll2DContactPoint(unityContacts[i], flipped);
        }
    }
}


// for collision enter events
#region EventDataTypes
public class EventData<I, D>
{
    public D myEventData, theirEventData;
    protected I internalData;

    public void Init(I internalData, D myEventData, D theirEventData)
    {
        this.internalData = internalData;
        this.myEventData = myEventData;
        this.theirEventData = theirEventData;
    }
}

public class CollEnterDataBundle
{
    public Coll2D collision;
    public RbodyCollController collisionCont;
    public InteractionController interactionCont;
    public CollisionEnterEventCB callback;
    public float collMomentum;

    public CollEnterDataBundle(EventManager.EDCollisionEnter collEvent){
        collision = new Coll2D(collEvent.flippedColl ? collEvent.collider : collEvent.collision2D.collider, collEvent.collision2D, collEvent.flippedColl);
        collisionCont = collEvent.collisionCont;
        callback = collEvent.collisionCallback;
        interactionCont = collEvent.interactionCont;
    }
}

public class CollisionEnterDataInternal
{
    private CollEnterDataBundle collEnterDataBundle1;
    private CollEnterDataBundle collEnterDataBundle2;
    public bool alreadyProcessed = false;

    public CollisionEnterDataInternal(CollEnterDataBundle collEnterDataBundle1, CollEnterDataBundle collEnterDataBundle2)
    {
        this.collEnterDataBundle1 = collEnterDataBundle1;
        this.collEnterDataBundle2 = collEnterDataBundle2;
    }
}

public class CollisionEnterData : EventData<CollisionEnterDataInternal, CollEnterDataBundle>
{
    // the same event data that unity gives
    public Coll2D unityEvent
    {
        get
        {
            return myEventData.collision;
        }
    }

    public float GetTotalMomentum()
    {
        return myEventData.collMomentum + theirEventData.collMomentum;
    }

    public float GetCollMomentumPercentDiff()
    {
        return Mathf.Abs(myEventData.collMomentum - theirEventData.collMomentum)
                / Mathf.Max(myEventData.collMomentum, theirEventData.collMomentum);
    }

    public bool IsProccessed()
    {
        return internalData.alreadyProcessed;
    }

    public void SetAsProcessed()
    {
        internalData.alreadyProcessed = true;
    }
}

// for collision stay and exit events
public class CollDataBundle
{
    public Coll2D collision;
    public RbodyCollController collisionCont;
    public InteractionController interactionCont;
    public CollisionEventCB callback;

    public CollDataBundle(EventManager.EDCollisionStay collEvent)
    {
        collision = new Coll2D(collEvent.flippedColl ? collEvent.collider : collEvent.collision2D.collider, collEvent.collision2D, collEvent.flippedColl);
        collisionCont = collEvent.collisionCont;
        callback = collEvent.collisionCallback;
        interactionCont = collEvent.interactionCont;
    }

    public CollDataBundle(EventManager.EDCollisionExit collEvent)
    {
        collision = new Coll2D(collEvent.flippedColl ? collEvent.collider : collEvent.collision2D.collider, collEvent.collision2D, collEvent.flippedColl);
        collisionCont = collEvent.collisionCont;
        callback = collEvent.collisionCallback;
        interactionCont = collEvent.interactionCont;
    }
}

public class CollisionDataInternal
{
    private CollDataBundle collDataBundle1;
    private CollDataBundle collDataBundle2;
    public bool alreadyProcessed = false;

    public CollisionDataInternal(CollDataBundle collDataBundle1, CollDataBundle collDataBundle2)
    {
        this.collDataBundle1 = collDataBundle1;
        this.collDataBundle2 = collDataBundle2;
    }
}

public class CollisionData : EventData<CollisionDataInternal, CollDataBundle>
{    
    // the same event data that unity gives
    public Coll2D unityEvent
    {
        get
        {
            return myEventData.collision;
        }
    }

    public bool IsProccessed()
    {
        return internalData.alreadyProcessed;
    }

    public void SetAsProcessed()
    {
        internalData.alreadyProcessed = true;
    }
}


// for all trigger events
public class TriggerDataBundle
{
    public Collider2D eventCollider;
    public RbodyCollController collisionCont;
    public InteractionController interactionCont;

    public TriggerDataBundle(EventManager.EDTriggerEnter triggerEvent)
    {
        eventCollider = triggerEvent.eventCollider;
        collisionCont = triggerEvent.collisionCont;
        interactionCont = triggerEvent.interactionCont;
    }

    public TriggerDataBundle(EventManager.EDTriggerStay triggerEvent)
    {
        eventCollider = triggerEvent.eventCollider;
        collisionCont = triggerEvent.collisionCont;
        interactionCont = triggerEvent.interactionCont;
    }

    public TriggerDataBundle(EventManager.EDTriggerExit triggerEvent)
    {
        eventCollider = triggerEvent.eventCollider;
        collisionCont = triggerEvent.collisionCont;
        interactionCont = triggerEvent.interactionCont;
    }
}

public class TriggerStayExitDataBundle : TriggerDataBundle
{
    public TriggerEventCB callback;

    public TriggerStayExitDataBundle(EventManager.EDTriggerStay triggerEvent) : base(triggerEvent)
    {
        this.callback = triggerEvent.triggerCallback;
    }
    public TriggerStayExitDataBundle(EventManager.EDTriggerExit triggerEvent) : base(triggerEvent)
    {
        this.callback = triggerEvent.triggerCallback;
    }
}

public class TriggerEnterDataBundle : TriggerDataBundle
{
    public bool wasCheckedOnEnable;
    public TriggerEnterEventCB callback;

    public TriggerEnterDataBundle(EventManager.EDTriggerEnter triggerEvent) : base(triggerEvent)
    {
        this.wasCheckedOnEnable = triggerEvent.wasCheckedOnEnable;
        this.callback = triggerEvent.triggerCallback;
    }
}

public class TriggerStayExitDataInternal
{
    private TriggerStayExitDataBundle triggerDataBundle1;
    private TriggerStayExitDataBundle triggerDataBundle2;
    public bool alreadyProcessed = false;

    public TriggerStayExitDataInternal(TriggerStayExitDataBundle dataBundle1, TriggerStayExitDataBundle dataBundle2)
    {
        this.triggerDataBundle1 = dataBundle1;
        this.triggerDataBundle2 = dataBundle2;
    }
}

public class TriggerEnterDataInternal
{
    private TriggerEnterDataBundle triggerDataBundle1;
    private TriggerEnterDataBundle triggerDataBundle2;
    public bool alreadyProcessed = false;

    public TriggerEnterDataInternal(TriggerEnterDataBundle dataBundle1, TriggerEnterDataBundle dataBundle2)
    {
        this.triggerDataBundle1 = dataBundle1;
        this.triggerDataBundle2 = dataBundle2;
    }
}

public class TriggerStayExitData : EventData<TriggerStayExitDataInternal, TriggerStayExitDataBundle>
{
    // the same event data that unity gives
    public Collider2D unityEvent {
        get{
            return myEventData.eventCollider;
        }
    }

    public bool IsProccessed()
    {
        return internalData.alreadyProcessed;
    }

    public void SetAsProcessed()
    {
        internalData.alreadyProcessed = true;
    }
}

public class TriggerEnterData : EventData<TriggerEnterDataInternal, TriggerEnterDataBundle>
{
    // the same event data that unity gives
    public Collider2D unityEvent
    {
        get
        {
            return myEventData.eventCollider;
        }
    }

    public bool IsProccessed()
    {
        return internalData.alreadyProcessed;
    }

    public void SetAsProcessed()
    {
        internalData.alreadyProcessed = true;
    }
}
#endregion

public class CollisionTriggerHandler : MonoBehaviour
{

	private static CollisionTriggerHandler _instance;
    public static CollisionTriggerHandler instance
	{
		get
		{
			if (_instance == null && !EventManager.applicationExiting)
			{
				// if no instance of object, create it
				GameObject obj = new GameObject("CollisionTriggerHandler");
				_instance = obj.AddComponent<CollisionTriggerHandler>();
            }
			return _instance;
		}
	}



    class Collider2DPair : System.IEquatable<Collider2DPair>
    {
        public Collider2D coll1, coll2;

        public Collider2DPair(Collider2D coll1, Collider2D coll2)
        {
            this.coll1 = coll1;
            this.coll2 = coll2;
        }

        public override int GetHashCode()
        {
            return coll1.GetHashCode() ^ coll2.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Collider2DPair);
        }

        public bool Equals(Collider2DPair obj)
        {
            return obj != null && ((coll1 == obj.coll1 && coll2 == obj.coll2) || (coll1 == obj.coll2 && coll2 == obj.coll1));
        }
    }

    // disabled collision tracker
    private Dictionary<Collider2DPair, LogicLocker> disabledColliders = new Dictionary<Collider2DPair, LogicLocker>();

    // list of internal data that is referenced to
    private List<CollisionEnterDataInternal> internalCollEnterData;
    private List<CollisionDataInternal> internalCollData;
    private List<TriggerEnterDataInternal> internalTriggerEnterData;
    private List<TriggerStayExitDataInternal> internalTriggerStayExitData;

    private Dictionary<int, List<EventManager.EDCollisionEnter>> collEnterEventData;
    private Dictionary<int, List<EventManager.EDCollisionStay>> collStayEventData;
    private Dictionary<int, List<EventManager.EDCollisionExit>> collExitEventData;
    private Dictionary<int, List<EventManager.EDTriggerEnter>> triggerEnterEventData;
    private Dictionary<int, List<EventManager.EDTriggerStay>> triggerStayEventData;
    private Dictionary<int, List<EventManager.EDTriggerExit>> triggerExitEventData;

    private Dictionary<int, CollisionAndTriggerInfoBundle> allCollisionAndTriggerInfo;
    private IEnumerator lateFixedUpdate;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            collEnterEventData = new Dictionary<int, List<EventManager.EDCollisionEnter>>();
            collStayEventData = new Dictionary<int, List<EventManager.EDCollisionStay>>();
            collExitEventData = new Dictionary<int, List<EventManager.EDCollisionExit>>();
            triggerEnterEventData = new Dictionary<int, List<EventManager.EDTriggerEnter>>();
            triggerStayEventData = new Dictionary<int, List<EventManager.EDTriggerStay>>();
            triggerExitEventData = new Dictionary<int, List<EventManager.EDTriggerExit>>();
            allCollisionAndTriggerInfo = new Dictionary<int, CollisionAndTriggerInfoBundle>();

            internalCollData = new List<CollisionDataInternal>();
            internalCollEnterData = new List<CollisionEnterDataInternal>();
            internalTriggerStayExitData = new List<TriggerStayExitDataInternal>();
            internalTriggerEnterData = new List<TriggerEnterDataInternal>();

            // start coroutine to send collision data via callbacks
            lateFixedUpdate = LateFixedUpdateCoroutine();
            StartCoroutine(lateFixedUpdate);
        }
    }

    void OnEnable()
    {
        EventManager.instance.handlerCollisionEnter.StartListening(OnCollisionEnterEvent);
        EventManager.instance.handlerCollisionStay.StartListening(OnCollisionStayEvent);
        EventManager.instance.handlerCollisionExit.StartListening(OnCollisionExitEvent);

        EventManager.instance.handlerTriggerEnter.StartListening(OnTriggerEnterEvent);
        EventManager.instance.handlerTriggerStay.StartListening(OnTriggerStayEvent);
        EventManager.instance.handlerTriggerExit.StartListening(OnTriggerExitEvent);

        EventManager.instance.handlerObjDestroyed.StartListening(OnObjDestroyedEvent);
    }

    void OnDisable()
    {
        EventManager.instance.handlerCollisionEnter.StopListening(OnCollisionEnterEvent);
        EventManager.instance.handlerCollisionStay.StopListening(OnCollisionStayEvent);
        EventManager.instance.handlerCollisionExit.StartListening(OnCollisionExitEvent);

        EventManager.instance.handlerTriggerEnter.StopListening(OnTriggerEnterEvent);
        EventManager.instance.handlerTriggerStay.StopListening(OnTriggerStayEvent);
        EventManager.instance.handlerTriggerExit.StopListening(OnTriggerExitEvent);

        EventManager.instance.handlerObjDestroyed.StartListening(OnObjDestroyedEvent);
    }

    IEnumerator LateFixedUpdateCoroutine()
    {
        while (true)
        {
            //if (!MultiplayerPauseMenu.isPaused)
            {
                CompleteUnmatchedEvents();

                // send all collision info to objs interaction controllers
                foreach (KeyValuePair<int, CollisionAndTriggerInfoBundle> collisionTriggerInfo in allCollisionAndTriggerInfo)
                {
                    if(collisionTriggerInfo.Value.interactionCont.isActiveAndEnabled)
                        collisionTriggerInfo.Value.interactionCont.OnCollisionAndTriggerEvents(collisionTriggerInfo.Value);
                }

                // clear list used to pair received enent manager events
                collEnterEventData.Clear();
                collStayEventData.Clear();
                collExitEventData.Clear();
                triggerEnterEventData.Clear();
                triggerStayEventData.Clear();
                triggerExitEventData.Clear();

                // clear lists to store internal data references
                internalCollData.Clear();
                internalCollEnterData.Clear();
                internalTriggerStayExitData.Clear();
                internalTriggerEnterData.Clear();

                // clear list of all collected and organized data
                allCollisionAndTriggerInfo.Clear();
            }
            yield return new WaitForFixedUpdate();
        }
    }

    private void CompleteUnmatchedEvents()
    {
        // report unmatched events
        // TODO : option to pass dummy data with unmatched event to complete it
        foreach (KeyValuePair<int, List<EventManager.EDCollisionEnter>> eventList in collEnterEventData)
        {
            foreach(EventManager.EDCollisionEnter eventData in eventList.Value)
            {
                EventManager.EDCollisionEnter flippedEventData = new EventManager.EDCollisionEnter();
                flippedEventData.collider = eventData.collider;
                flippedEventData.interactionCont = eventData.otherInteractionCont;
                flippedEventData.otherInteractionCont = eventData.interactionCont;
                flippedEventData.collisionCont = eventData.otherInteractionCont.GetRbodyCollCont();
                flippedEventData.collision2D = eventData.collision2D;
                flippedEventData.momentumCalcRequired = eventData.momentumCalcRequired;
                flippedEventData.flippedColl = true;

                // if either collider is null do not save
                if (eventData.collider == null || flippedEventData.collider == null) return;
                SaveCollisionEnterData(eventData, flippedEventData);
            }
        }
        foreach (KeyValuePair<int, List<EventManager.EDCollisionStay>> eventList in collStayEventData)
        {
            foreach (EventManager.EDCollisionStay eventData in eventList.Value)
            {
                EventManager.EDCollisionStay flippedEventData = new EventManager.EDCollisionStay();
                flippedEventData.collider = eventData.collider;
                flippedEventData.interactionCont = eventData.otherInteractionCont;
                flippedEventData.otherInteractionCont = eventData.interactionCont;
                flippedEventData.collisionCont = eventData.otherInteractionCont.GetRbodyCollCont();
                flippedEventData.collision2D = eventData.collision2D;
                flippedEventData.flippedColl = true;
                SaveCollisionStayData(eventData, flippedEventData);
            }
        }
        foreach (KeyValuePair<int, List<EventManager.EDCollisionExit>> eventList in collExitEventData)
        {
            foreach (EventManager.EDCollisionExit eventData in eventList.Value)
            {
                EventManager.EDCollisionExit flippedEventData = new EventManager.EDCollisionExit();
                flippedEventData.collider = eventData.collider;
                flippedEventData.interactionCont = eventData.otherInteractionCont;
                flippedEventData.otherInteractionCont = eventData.interactionCont;
                flippedEventData.collisionCont = eventData.otherInteractionCont.GetRbodyCollCont();
                flippedEventData.collision2D = eventData.collision2D;
                flippedEventData.flippedColl = true;
                SaveCollisionExitData(eventData, flippedEventData);
            }
        }

        foreach (KeyValuePair<int, List<EventManager.EDTriggerEnter>> eventList in triggerEnterEventData)
        {
            foreach (EventManager.EDTriggerEnter eventData in eventList.Value)
            {
                EventManager.EDTriggerEnter flippedEventData = new EventManager.EDTriggerEnter();
                flippedEventData.collider = eventData.eventCollider;
                flippedEventData.eventCollider = eventData.collider;
                flippedEventData.interactionCont = eventData.otherInteractionCont;
                flippedEventData.otherInteractionCont = eventData.interactionCont;
                flippedEventData.collisionCont = eventData.otherInteractionCont.GetRbodyCollCont();
                SaveTriggerEnterData(eventData, flippedEventData);
            }
        }
        foreach (KeyValuePair<int, List<EventManager.EDTriggerStay>> eventList in triggerStayEventData)
        {
            foreach (EventManager.EDTriggerStay eventData in eventList.Value)
            { 
                EventManager.EDTriggerStay flippedEventData = new EventManager.EDTriggerStay();
                flippedEventData.collider = eventData.eventCollider;
                flippedEventData.eventCollider = eventData.collider;
                flippedEventData.interactionCont = eventData.otherInteractionCont;
                flippedEventData.otherInteractionCont = eventData.interactionCont;
                flippedEventData.collisionCont = eventData.otherInteractionCont.GetRbodyCollCont();
                SaveTriggerStayData(eventData, flippedEventData);
            }
        }
        foreach (KeyValuePair<int, List<EventManager.EDTriggerExit>> eventList in triggerExitEventData)
        {
            foreach (EventManager.EDTriggerExit eventData in eventList.Value)
            {
                EventManager.EDTriggerExit flippedEventData = new EventManager.EDTriggerExit();
                flippedEventData.collider = eventData.eventCollider;
                flippedEventData.eventCollider = eventData.collider;
                flippedEventData.interactionCont = eventData.otherInteractionCont;
                flippedEventData.otherInteractionCont = eventData.interactionCont;
                flippedEventData.collisionCont = eventData.otherInteractionCont.GetRbodyCollCont();
                SaveTriggerExitData(eventData, flippedEventData);
            }
        }
    }

    #region SaveDataDelegates
    // fcn delegate to make storing events easier
    public delegate bool DataMatchesFcn<T>(T data1, T data2);
    private bool CollEnterMatches(EventManager.EDCollisionEnter coll1, EventManager.EDCollisionEnter coll2)
    {
        return (coll1.collider == coll2.collision2D.collider && coll2.collider == coll1.collision2D.collider);
    }
    private bool CollStayMatches(EventManager.EDCollisionStay coll1, EventManager.EDCollisionStay coll2)
    {
        return (coll1.collider == coll2.collision2D.collider && coll2.collider == coll1.collision2D.collider);
    }
    private bool CollExitMatches(EventManager.EDCollisionExit coll1, EventManager.EDCollisionExit coll2)
    {
        return (coll1.collider == coll2.collision2D.collider && coll2.collider == coll1.collision2D.collider);
    }
    private bool TriggerEnterMatches(EventManager.EDTriggerEnter coll1, EventManager.EDTriggerEnter coll2)
    {
        return (coll1.collider == coll2.eventCollider && coll2.collider == coll1.eventCollider);
    }
    private bool TriggerStayMatches(EventManager.EDTriggerStay coll1, EventManager.EDTriggerStay coll2)
    {
        return (coll1.collider == coll2.eventCollider && coll2.collider == coll1.eventCollider);
    }
    private bool TriggerExitMatches(EventManager.EDTriggerExit coll1, EventManager.EDTriggerExit coll2)
    {
        return (coll1.collider == coll2.eventCollider && coll2.collider == coll1.eventCollider);
    }

    public delegate void SaveDataFcn<T>(T data1, T data2);

    private delegate void AddEventTypeToInfoBundleFcn<I, D, E>(CollisionAndTriggerInfo info, E data) where E : EventData<I, D>;
    private void AddCollisionEnterDataToInfoBundle(CollisionAndTriggerInfo info, CollisionEnterData data)
    {
        info.collisionEnterData.Add(data);
    }
    private void AddCollisionStayDataToInfoBundle(CollisionAndTriggerInfo info, CollisionData data)
    {
        info.collisionStayData.Add(data);
    }
    private void AddCollisionExitDataToInfoBundle(CollisionAndTriggerInfo info, CollisionData data)
    {
        info.collisionExitData.Add(data);
    }
    private void AddTriggerEnterDataToInfoBundle(CollisionAndTriggerInfo info, TriggerEnterData data)
    {
        info.triggerEnterData.Add(data);
    }
    private void AddTriggerStayDataToInfoBundle(CollisionAndTriggerInfo info, TriggerStayExitData data)
    {
        info.triggerStayData.Add(data);
    }
    private void AddTriggerExitDataToInfoBundle(CollisionAndTriggerInfo info, TriggerStayExitData data)
    {
        info.triggerExitData.Add(data);
    }
    #endregion

    #region RegisteredCallbacks
    public void OnCollisionEnterEvent(EventManager.EDCollisionEnter eventData)
    {
        OrganizeEvent(eventData.interactionCont, eventData.otherInteractionCont, eventData, collEnterEventData, CollEnterMatches, SaveCollisionEnterData);
    }

    public void OnCollisionStayEvent(EventManager.EDCollisionStay eventData)
    {
        OrganizeEvent(eventData.interactionCont, eventData.otherInteractionCont, eventData, collStayEventData, CollStayMatches, SaveCollisionStayData);
    }
    
    public void OnCollisionExitEvent(EventManager.EDCollisionExit eventData)
    {
        OrganizeEvent(eventData.interactionCont, eventData.otherInteractionCont, eventData, collExitEventData, CollExitMatches, SaveCollisionExitData);
    }

    public void OnTriggerEnterEvent(EventManager.EDTriggerEnter eventData)
    {
        OrganizeEvent(eventData.interactionCont, eventData.otherInteractionCont, eventData, triggerEnterEventData, TriggerEnterMatches, SaveTriggerEnterData);
    }

    public void OnTriggerStayEvent(EventManager.EDTriggerStay eventData)
    {
        OrganizeEvent(eventData.interactionCont, eventData.otherInteractionCont, eventData, triggerStayEventData, TriggerStayMatches, SaveTriggerStayData);
    }

    public void OnTriggerExitEvent(EventManager.EDTriggerExit eventData)
    {
        OrganizeEvent(eventData.interactionCont, eventData.otherInteractionCont, eventData, triggerExitEventData, TriggerExitMatches, SaveTriggerExitData);
    }

    public void OrganizeEvent<T>(InteractionController interactionCont, InteractionController otherInteractionCont, T eventData, Dictionary<int, List<T>> events,
                                  DataMatchesFcn<T> dataMatchesFcn, SaveDataFcn<T> saveDataFcn)
    {
        // ignore events with objects that don't have interaction controllers
        if (otherInteractionCont == null)
        {
            return;
        }
        if (interactionCont == null)
        {
            return;
        }

        // check to see if the object that was collided with is already being tracked
        List<T> eventDataList = null;
        if (events.TryGetValue(otherInteractionCont.gameObject.GetInstanceID(), out eventDataList))
        {
            foreach (T data in eventDataList)
            {
                if (dataMatchesFcn(data, eventData))
                {
                    // found a collision between two colliders, create collision data for it
                    saveDataFcn(data, eventData);

                    // remove it from the list to prevent compared against again
                    eventDataList.Remove(data);
                    break;
                }
            }
        }
        else
        {
            // attempt to get obj's own list of collisions
            if (events.TryGetValue(interactionCont.gameObject.GetInstanceID(), out eventDataList))
            {
                eventDataList.Add(eventData);
            }
            else
            {
                // add the object to the list
                eventDataList = new List<T>();
                eventDataList.Add(eventData);
                events.Add(interactionCont.gameObject.GetInstanceID(), eventDataList);
            }
        }
    }

    #endregion

    #region SaveDataFunctions
    private void SaveCollisionEnterData(EventManager.EDCollisionEnter event1, EventManager.EDCollisionEnter event2)
    {
        // create collision data 
        CollEnterDataBundle collEnterData1 = new CollEnterDataBundle(event1);
        CollEnterDataBundle collEnterData2 = new CollEnterDataBundle(event2);

        // calculate collision momentum if desired
        if (event1.momentumCalcRequired || event2.momentumCalcRequired)
        {
            // calculate collision momentum for non static objects
            if(!event1.interactionCont.SupportsInteraction(INTERACTION.COLL_IS_STATIC))
                collEnterData1.collMomentum = GetCollMomentum(event1.collision2D, event1.collisionCont);
            if (!event2.interactionCont.SupportsInteraction(INTERACTION.COLL_IS_STATIC))
                collEnterData2.collMomentum = GetCollMomentum(event2.collision2D, event2.collisionCont);
        }

        // create the internal data
        CollisionEnterDataInternal internalData = new CollisionEnterDataInternal(collEnterData1, collEnterData2);
        internalCollEnterData.Add(internalData);

        // save collision enter data for both objects
        SaveDataHelper<CollisionEnterDataInternal, CollEnterDataBundle, CollisionEnterData>(event1.interactionCont, event2.interactionCont, 
                                                                    internalData, collEnterData1, collEnterData2, AddCollisionEnterDataToInfoBundle);
        SaveDataHelper<CollisionEnterDataInternal, CollEnterDataBundle, CollisionEnterData>(event2.interactionCont, event1.interactionCont,
                                                            internalData, collEnterData2, collEnterData1, AddCollisionEnterDataToInfoBundle);
    }


    private void SaveCollisionStayData(EventManager.EDCollisionStay event1, EventManager.EDCollisionStay event2)
    {
        // create collision data 
        CollDataBundle collData1 = new CollDataBundle(event1);
        CollDataBundle collData2 = new CollDataBundle(event2);

        // create the internal data
        CollisionDataInternal internalData = new CollisionDataInternal(collData1, collData2);
        internalCollData.Add(internalData);

        // save collision enter data for both objects
        SaveDataHelper<CollisionDataInternal, CollDataBundle, CollisionData>(event1.interactionCont, event2.interactionCont,
                                                            internalData, collData1, collData2, AddCollisionStayDataToInfoBundle);
        SaveDataHelper<CollisionDataInternal, CollDataBundle, CollisionData>(event2.interactionCont, event1.interactionCont,
                                                            internalData, collData2, collData1, AddCollisionStayDataToInfoBundle);
    }

    private void SaveCollisionExitData(EventManager.EDCollisionExit event1, EventManager.EDCollisionExit event2)
    {
        // create collision data 
        CollDataBundle collData1 = new CollDataBundle(event1);
        CollDataBundle collData2 = new CollDataBundle(event2);

        // create the internal data
        CollisionDataInternal internalData = new CollisionDataInternal(collData1, collData2);
        internalCollData.Add(internalData);

        // save collision enter data for both objects
        SaveDataHelper<CollisionDataInternal, CollDataBundle, CollisionData>(event1.interactionCont, event2.interactionCont,
                                                            internalData, collData1, collData2, AddCollisionExitDataToInfoBundle);
        SaveDataHelper<CollisionDataInternal, CollDataBundle, CollisionData>(event2.interactionCont, event1.interactionCont,
                                                            internalData, collData2, collData1, AddCollisionExitDataToInfoBundle);
    }

    private void SaveTriggerEnterData(EventManager.EDTriggerEnter event1, EventManager.EDTriggerEnter event2)
    {
        // create collision data 
        TriggerEnterDataBundle data1 = new TriggerEnterDataBundle(event1);
        TriggerEnterDataBundle data2 = new TriggerEnterDataBundle(event2);

        // create the internal data
        TriggerEnterDataInternal internalData = new TriggerEnterDataInternal(data1, data2);
        internalTriggerEnterData.Add(internalData);

        // save collision enter data for both objects
        SaveDataHelper<TriggerEnterDataInternal, TriggerEnterDataBundle, TriggerEnterData>(event1.interactionCont, event2.interactionCont,
                                                            internalData, data1, data2, AddTriggerEnterDataToInfoBundle);
        SaveDataHelper<TriggerEnterDataInternal, TriggerEnterDataBundle, TriggerEnterData>(event2.interactionCont, event1.interactionCont,
                                                            internalData, data2, data1, AddTriggerEnterDataToInfoBundle);
    }

    private void SaveTriggerStayData(EventManager.EDTriggerStay event1, EventManager.EDTriggerStay event2)
    {
        // create collision data 
        TriggerStayExitDataBundle data1 = new TriggerStayExitDataBundle(event1);
        TriggerStayExitDataBundle data2 = new TriggerStayExitDataBundle(event2);

        // create the internal data
        TriggerStayExitDataInternal internalData = new TriggerStayExitDataInternal(data1, data2);
        internalTriggerStayExitData.Add(internalData);

        // save collision enter data for both objects
        SaveDataHelper<TriggerStayExitDataInternal, TriggerStayExitDataBundle, TriggerStayExitData>(event1.interactionCont, event2.interactionCont,
                                                            internalData, data1, data2, AddTriggerStayDataToInfoBundle);
        SaveDataHelper<TriggerStayExitDataInternal, TriggerStayExitDataBundle, TriggerStayExitData>(event2.interactionCont, event1.interactionCont,
                                                            internalData, data2, data1, AddTriggerStayDataToInfoBundle);
    }

    private void SaveTriggerExitData(EventManager.EDTriggerExit event1, EventManager.EDTriggerExit event2)
    {
        // create collision data 
        TriggerStayExitDataBundle data1 = new TriggerStayExitDataBundle(event1);
        TriggerStayExitDataBundle data2 = new TriggerStayExitDataBundle(event2);

        // create the internal data
        TriggerStayExitDataInternal internalData = new TriggerStayExitDataInternal(data1, data2);
        internalTriggerStayExitData.Add(internalData);

        // save collision enter data for both objects
        SaveDataHelper<TriggerStayExitDataInternal, TriggerStayExitDataBundle, TriggerStayExitData>(event1.interactionCont, event2.interactionCont,
                                                            internalData, data1, data2, AddTriggerExitDataToInfoBundle);
        SaveDataHelper<TriggerStayExitDataInternal, TriggerStayExitDataBundle, TriggerStayExitData>(event2.interactionCont, event1.interactionCont,
                                                            internalData, data2, data1, AddTriggerExitDataToInfoBundle);
    }


    private void SaveDataHelper<I, D, E>(InteractionController interactionCont1, InteractionController interactionCont2,
                  I internalData, D myBundle, D theirBundle, AddEventTypeToInfoBundleFcn<I, D, E> addInfoFcn) where E : EventData<I, D>, new()
    {
        if(interactionCont1 == null || interactionCont2 == null) return;
        E data = new E();
        data.Init(internalData, myBundle, theirBundle);

        CollisionAndTriggerInfoBundle collisionTriggerInfoBundle = null;
        if (!allCollisionAndTriggerInfo.TryGetValue(interactionCont1.gameObject.GetInstanceID(), out collisionTriggerInfoBundle))
        {
            // if info for the object doesn't exist, create it
            collisionTriggerInfoBundle = new CollisionAndTriggerInfoBundle(interactionCont1);
            allCollisionAndTriggerInfo.Add(interactionCont1.gameObject.GetInstanceID(), collisionTriggerInfoBundle);
        }

        // add collision enter info the object paired with the interaction controller it interacted with
        CollisionAndTriggerInfo collisionTriggerInfo = null;
        if (!collisionTriggerInfoBundle.objBasedInteractionsDict.TryGetValue(interactionCont2, out collisionTriggerInfo))
        {

            // if collision info doesn't exist yet add it
            collisionTriggerInfo = new CollisionAndTriggerInfo();
            addInfoFcn(collisionTriggerInfo, data);
            collisionTriggerInfoBundle.objBasedInteractionsDict.Add(interactionCont2, collisionTriggerInfo);
        }
        else
        {
            addInfoFcn(collisionTriggerInfo, data);
        }

        // save interaction just based on type also
        addInfoFcn(collisionTriggerInfoBundle.eventBasedInteractions, data);
    }
    #endregion

    #region StaticHelperFunctions
    static public float GetCollMomentum(Collision2D coll, RbodyCollController collisionCont)
    {
        if (collisionCont == null) return 0.0f;


        // calculate momentum from ship
        float speedMomentum = 0.0f;
        float angularMomentum = 0.0f;
        ContactPoint2D[] contacts = new ContactPoint2D[3];
        int num = coll.GetContacts(contacts);

        for(int i = 0; i < num; ++i)
        {
            ContactPoint2D contactPoint = contacts[i];
            Vector2 dirToPoint = contactPoint.point - collisionCont.worldCenterOfMass;
            float circumference = 2.0f * Mathf.PI * dirToPoint.magnitude;
            float angularSpeed = (collisionCont.lastAngularVel / 360.0f) * circumference;
            Vector2 angularVel = new Vector2(-dirToPoint.y, dirToPoint.x).normalized * angularSpeed;

            speedMomentum += Vector2.Dot(-contactPoint.normal, collisionCont.lastVel);
            float angularFactor = Vector2.Dot(-contactPoint.normal, angularVel) * 0.5F; // Reduce significance of angular momentum
            if (angularFactor > 0.0f) angularMomentum += angularFactor;
        }
        speedMomentum /= num;
        angularMomentum /= num;
        return (speedMomentum + angularMomentum) * collisionCont.lastMass;
    }

    static public Vector2 GetRelativeVelocity(Vector2 vel1, Vector2 vel2)
    {
        return vel1 - vel2;
    }

    static public float GetColliderRadius(GameObject obj)
    {
        float range = -1.0f;
        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
        // loop over colliders and determine bounds distance from obj position
        foreach (Collider2D collider in colliders)
        {
            // ignore triggers
            if (collider.isTrigger) continue;

            // get the bounds and offset of the collider
            Vector2 colliderOffset = collider.bounds.center - obj.transform.position;

            // test against collider bounds for max distance from obj position
            // top right
            float dist = (colliderOffset + (Vector2)collider.bounds.extents).magnitude;
            if (dist > range) range = dist;
            // bottom left
            dist = (colliderOffset - (Vector2)collider.bounds.extents).magnitude;

            if (dist > range) range = dist;
            // top left
            dist = (colliderOffset + new Vector2(-collider.bounds.extents.x, collider.bounds.extents.y)).magnitude;

            if (dist > range) range = dist;
            // bottom right
            dist = (colliderOffset + new Vector2(collider.bounds.extents.x, -collider.bounds.extents.y)).magnitude;

            if (dist > range) range = dist;
        }
        return range;
    }
    #endregion

    private void PrintCollData(CollEnterDataBundle data)
    {
        Debug.Log("gameobject: " + data.interactionCont.gameObject.name);
        Debug.Log("collision cont: " + data.collisionCont.GetInstanceID());
        Debug.Log("collider: " + data.collision.collider);
        foreach (Coll2DContactPoint point in data.collision.contacts)
        {
            Debug.Log("contact point: " + point.point + " " + point.normal + " " + point.otherCollider.name + " " + point.collider.name);
        }
        Debug.Log("relative vel: " + data.collision.relativeVelocity);
    }

    #region DisabledCollidersHelpers
    public void DisableColliderCollPair(Collider2D coll1, Collider2D coll2, string key)
    {
        // only disable if both are colliders and are enabled
        if (coll1.isTrigger || coll2.isTrigger) return;

        Collider2DPair colliderPair = new Collider2DPair(coll1, coll2);
        LogicLocker collLocker = null;
        if(disabledColliders.TryGetValue(colliderPair, out collLocker))
        {
            collLocker.SetLocker(key);
        }
        else
        {
            collLocker = new LogicLocker();
            collLocker.SetLocker(key);
            disabledColliders.Add(colliderPair, collLocker);

            // make sure that objs have a destroy signal component
            if (coll1.GetComponent<ObjDestroyedSignal>() == null) coll1.gameObject.AddComponent<ObjDestroyedSignal>();
            if (coll2.GetComponent<ObjDestroyedSignal>() == null) coll2.gameObject.AddComponent<ObjDestroyedSignal>();
        }

        // tell the physics engine to ignore collision between the two colliders
        Physics2D.IgnoreCollision(coll1, coll2);
    }

    private void EnableColliderCollPair(Collider2DPair colliderPair, string key)
    {
        LogicLocker collLocker = null;
        if(disabledColliders.TryGetValue(colliderPair, out collLocker))
        {
            collLocker.RemoveLocker(key);
            if (!collLocker.IsLocked())
            {
                // tell the phsyics engine to enable collisions between the two colliders
                Physics2D.IgnoreCollision(colliderPair.coll1, colliderPair.coll2, false);

                // remove the pair from the dictionary
                disabledColliders.Remove(colliderPair);
            }
        }
    }

    public void EnableColliderCollPair(Collider2D coll1, Collider2D coll2, string key)
    {
        Collider2DPair colliderPair = new Collider2DPair(coll1, coll2);
        EnableColliderCollPair(colliderPair, key);
    }

    public void DisableAllObjCollisionsWithCollider(GameObject obj, Collider2D coll, string key)
    {
        Collider2D[] objColliders = obj.GetComponentsInChildren<Collider2D>();
        foreach(Collider2D objColl in objColliders)
        {
            DisableColliderCollPair(objColl, coll, key);
        }
    }

    public void EnableAllObjCollisionsWithCollider(GameObject obj, Collider2D coll, string key)
    {
        Collider2D[] objColliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D objColl in objColliders)
        {
            EnableColliderCollPair(objColl, coll, key);
        }
    }

    public void DisableAllArrayCollsWithArrayColls(Collider2D[] objColls1, Collider2D[] objColls2, string key)
    {
        foreach (Collider2D objColl1 in objColls1)
        {
            foreach (Collider2D objColl2 in objColls2)
            {
                DisableColliderCollPair(objColl1, objColl2, key);
            }
        }
    }

    public void EnableAllArrayCollsWithArrayColls(Collider2D[] objColls1, Collider2D[] objColls2, string key)
    {
        foreach (Collider2D objColl1 in objColls1)
        {
            foreach (Collider2D objColl2 in objColls2)
            {
                EnableColliderCollPair(objColl1, objColl2, key);
            }
        }
    }

    private void DisableAllArrayCollsWithCollider(Collider2D[] collArray, Collider2D objColl, string key)
    {
        foreach (Collider2D objColl1 in collArray)
        {
            DisableColliderCollPair(objColl1, objColl, key);
        }
    }

    private void EnableAllArrayCollsWithCollider(Collider2D[] collArray, Collider2D objColl, string key)
    {
        foreach (Collider2D objColl1 in collArray)
        {
            EnableColliderCollPair(objColl1, objColl, key);
        }
    }

    public void DisableAllObjCollisions(GameObject obj1, GameObject obj2, string key)
    {
        Collider2D[] objColliders1 = obj1.GetComponentsInChildren<Collider2D>();
        Collider2D[] objColliders2 = obj2.GetComponentsInChildren<Collider2D>();
        DisableAllArrayCollsWithArrayColls(objColliders1, objColliders2, key);
    }

    public void EnableAllObjCollisions(GameObject obj1, GameObject obj2, string key)
    {
        if (obj1 == null || obj2 == null) return;
        Collider2D[] objColliders1 = obj1.GetComponentsInChildren<Collider2D>();
        Collider2D[] objColliders2 = obj2.GetComponentsInChildren<Collider2D>();
        EnableAllArrayCollsWithArrayColls(objColliders1, objColliders2, key);
    }

    public void DisableAllObjCollsForced(GameObject obj1, GameObject obj2)
    {
        Collider2D[] objColliders1 = obj1.GetComponentsInChildren<Collider2D>();
        Collider2D[] objColliders2 = obj2.GetComponentsInChildren<Collider2D>();
        foreach(var coll1 in objColliders1)
        {
            foreach(var coll2 in objColliders2)
            {
                Physics2D.IgnoreCollision(coll1, coll2, true);
            }
        }
    }

    public void EnableAllObjCollsForced(GameObject obj1, GameObject obj2)
    {
        if (obj1 == null || obj2 == null) return;
        Collider2D[] objColliders1 = obj1.GetComponentsInChildren<Collider2D>();
        Collider2D[] objColliders2 = obj2.GetComponentsInChildren<Collider2D>();
        foreach (var coll1 in objColliders1)
        {
            foreach (var coll2 in objColliders2)
            {
                Physics2D.IgnoreCollision(coll1, coll2, false);
            }
        }
    }

    private void OnObjDestroyedEvent(EventManager.EDObjDestroyed data)
    {
        // when an obj is destroyed, make sure it is no longer being tracked by disabled coll system
        Collider2D coll = data.obj.GetComponent<Collider2D>();

        if(coll != null)
        {
            List<Collider2DPair> toRemoveList = new List<Collider2DPair>();
            foreach(KeyValuePair<Collider2DPair, LogicLocker> disabledColls in disabledColliders)
            {
                // if coll was pair of disabled pair, save it to remove
                if(disabledColls.Key.coll1 == coll || disabledColls.Key.coll2 == coll)
                {
                    toRemoveList.Add(disabledColls.Key);
                }
            }

            // remove of pairs containing this collider
            foreach(Collider2DPair collPair in toRemoveList)
            {
                // tell the phsyics engine to enable collisions between the two colliders
                Physics2D.IgnoreCollision(collPair.coll1, collPair.coll2, false);

                // remove the pair from the dictionary
                disabledColliders.Remove(collPair);
            }
        }
		
    }

	/// <summary> Literally only here to reference something through instance. </summary>
	public void Create() { }
    #endregion

    public void OnDestroy()
    {
        _instance = null;
    }
}

