using UnityEngine;
using System.Collections.Generic;

public enum INTERACTION
{
    CAN_PASS_THROUGH,                   // the object can enter the wormhole special
    COLL_CANCELS_PASS_THROUGH,          // the object disabled the wormhole special when colliding with the ship
    ENDS_PASS_THROUGH,                  // the object ends the wormhole special after being launched by it
    AFFECTED_BY_SHOCKWAVE,              // the object can be hit by the shockwave special
    AFFECTED_BY_FORCE_SLAM,              // the object can be hit by the shockwave special
    CAN_BE_GRAPPLED,                    // the object can be hit by the grapple special
    GRAPPLE_LOCKON,                     // can be locked on to by grappling hook
    LANCE_LOCKON,                       // can be locked on to by lance
    AFFECTED_BY_GRAPPLE_CHAIN,          // the object reacts to touching the grapple chain
    COLL_CANCELS_WARP,                  // the object stops a ships warp when colliding with the warping ship
    COLL_ACTIVATES_WARP_OVERRIDE,       // the object can cause a warp override on collision
    PULLED_BY_WARP_LINE,                // the object is drawn toward a player's warp line
    GRABBED_BY_THORNS,                  // the object can be grabbed by the thorns special
    DEATH_ZONE,                         // the object is affected when entering a death zone
    ZAPPED_BY_TESLA_COIL,               // the tesla coil will fire lightning at objects in the death zone
    PUSHED_BY_TESLA_COIL,               // obj can be pushed by tesla coil lightning 
    COLL_CAN_STUN_SHIP,                 // the object has potential to stun a ship when colliding with it
    COLL_IS_STATIC,                     // the object is static and doesn't provide any momentum going into the collision
    COLL_KILLS_AT_MAX_DMG,              // the object can kill something upon collision when it is fully damaged
    AI_TARGET,                          // the object is a valid target for enemy ai
    CAN_PICKUP_HEALTH,                  // the object can pickup health
    CAN_PICKUP_ABILITY_POINTS,          // the object can pickup ability points
    CAN_BE_LAUNCHED_BY_CANNON,          // the object enter and launch from a cannon
    KNOCKS_OFF_RAILS,                   // the object can knock a ship off a rail
    KILLS_ON_CONTACT,					// The object will instantly kill a player it touches
    CAN_BE_CRTICALLY_HIT_BY_COLL,       // the object can be critially hit by collisions
    TRIGGERS_THORN_GRAB_TIMER           // the object start the thorn grab timer
}

public struct InteractionEnumComparer : IEqualityComparer<INTERACTION>
{
    public bool Equals(INTERACTION x, INTERACTION y)
    {
        return x == y;
    }

    public int GetHashCode(INTERACTION obj)
    {
        // you need to do some thinking here,
        return (int)obj;
    }
}

public class InteractionController : MonoBehaviour, IResettable
{
    [SerializeField]
    protected INTERACTION[] interactions = new INTERACTION[0];
    protected BitSet interactionBitSet;

    protected HashSet<IRbodyCollisions> rbodyCollListeners = new HashSet<IRbodyCollisions>();
    protected HashSet<IStaticCollisions> staticCollListeners = new HashSet<IStaticCollisions>();
    public int teamNum { get; protected set; }
    private static float sameTeamStunDmgScalar = 1.0f / 3.0f;

    public virtual void Awake()
    {
        interactionBitSet = new BitSet();

        // init bitset with interactions
        foreach (INTERACTION interaction in interactions)
        {
            interactionBitSet.SetBit((int)interaction);
        }

        // have implementation initialize
        Initialize();
    }

    protected virtual void Initialize() { }
    protected virtual void Enabled() { }
    protected virtual void Disabled() { }

    void OnEnable()
    {
        // register with the interaction type groups
        foreach(int setBit in interactionBitSet.GetListOfSetBits())
        {
            INTERACTION interaction = (INTERACTION)setBit;
            if (RegistersWithGroup(interaction) && InteractionGroups.instance != null)
            {
                InteractionGroups.instance.RegisterWithGroup(interaction, this);
            }
        }

        Enabled();
    }

    void OnDisable()
    {
        // register with the interaction type groups
        foreach (int setBit in interactionBitSet.GetListOfSetBits())
        {
            INTERACTION interaction = (INTERACTION)setBit;
            if (RegistersWithGroup(interaction) && InteractionGroups.instance != null)
            {
                InteractionGroups.instance.UnregisterWithGroup(interaction, this);
            }
        }
        Disabled();
    }

    public virtual void ExecuteReset()
    {
        rbodyCollListeners.Clear();
        staticCollListeners.Clear();
    }

    public virtual void ReInit() { }

    private bool RegistersWithGroup(INTERACTION interaction)
    {
        switch (interaction)
        {
            case INTERACTION.AFFECTED_BY_SHOCKWAVE:
            case INTERACTION.AFFECTED_BY_FORCE_SLAM:
            case INTERACTION.AI_TARGET:
            case INTERACTION.PULLED_BY_WARP_LINE:
            case INTERACTION.GRAPPLE_LOCKON:
            case INTERACTION.CAN_PASS_THROUGH:
                return true;
            default:
                return false;
        }
    }

    public void AddSupportForInteraction(INTERACTION interaction)
    {
        if (!SupportsInteraction(interaction))
        {
            interactionBitSet.SetBit((int)interaction);
            if (RegistersWithGroup(interaction) && InteractionGroups.instance != null)
            {
                InteractionGroups.instance.RegisterWithGroup(interaction, this);
            }
        }
    }

    public void StopSupportingInteraction(INTERACTION interaction)
    {
        if (SupportsInteraction(interaction))
        {
            interactionBitSet.ClearBit((int)interaction);
            if (RegistersWithGroup(interaction) && InteractionGroups.instance != null)
            {
                InteractionGroups.instance.UnregisterWithGroup(interaction, this);
            }
        }
    }

    public bool SupportsInteractions(BitSet requiredInteractions)
    {
        return requiredInteractions.IsSubsetOf(interactionBitSet);
    }

    public bool SupportsInteraction(INTERACTION requiredInteraction)
    {
        return interactionBitSet.IsSet((int)requiredInteraction);
    }

    public void RegisterRbodyCollisionListener(IRbodyCollisions listener)
    {
        if(!rbodyCollListeners.Contains(listener))
            rbodyCollListeners.Add(listener);
    }

    public void RegisterStaticCollisionListener(IStaticCollisions listener)
    {
        if(!staticCollListeners.Contains(listener))
            staticCollListeners.Add(listener);
    }

    public void UnRegisterRbodyCollisionListener(IRbodyCollisions listener)
    {
        rbodyCollListeners.Remove(listener);
    }

    public void UnRegisterStaticCollisionListener(IStaticCollisions listener)
    {
        staticCollListeners.Remove(listener);
    }

    public static InteractionController GetInteractionController(GameObject obj)
    {
        // find a type controller on the object, starting at the given obj then moving onto parent objects if not found
        InteractionController comp = obj.GetComponent<InteractionController>();
        if (comp == null)
        {
            if (obj.transform.parent == null) return null;
            else
            {
                return GetInteractionController(obj.transform.parent.gameObject);
            }
        }
        else
        {
            return comp;
        }
    }

    public float GetAdjustedTeamVal(InteractionController interactionCont, float val)
    {
        if (OnSameTeam(interactionCont))
        {
            return val *= sameTeamStunDmgScalar;
        }
        else return val;
    }

    public bool OnSameTeam(InteractionController interactionCont)
    {
        return teamNum == interactionCont.teamNum && teamNum != -1 && interactionCont.teamNum != -1;
    }
    public bool OnSameTeam(int teamNum) { return this.teamNum == teamNum && this.teamNum != -1 && teamNum != -1; }

    // ALL POSSIBLE INTERACTION CALLBACKS /////////////////////////////////////////////////////////////////////////////////////
    public virtual int GetTeam() { return teamNum; }
    public void SetTeam(int team) { teamNum = team; }
    public virtual RbodyCollController GetRbodyCollCont() { return GetComponent<RbodyCollController>(); }

    #region CollisionTriggerCallbacks
    public virtual List<Collider2D> GetEnabledColliders2D() { return null; }
    public virtual List<Collider2D> GetEnabledTriggers2D() { return null; }

    public virtual void OnCriticalCollision(GameObject hitByObj, float force)
    {

    }

	public virtual void OnCollisionAndTriggerEvents(CollisionAndTriggerInfoBundle collAndTriggerData)
	{
		// send ship max info about collisions
		foreach (KeyValuePair<InteractionController, CollisionAndTriggerInfo> objInteractions in collAndTriggerData.objBasedInteractionsDict)
		{
			float myMaxCollMomentum = 0.0f;
			Vector2 impactPoint = new Vector2();

			if (!objInteractions.Key.SupportsInteraction(INTERACTION.COLL_IS_STATIC))
			{
				// iterate over collisions to determine strongest point of impact and collision momentum regarding both objects
				if (objInteractions.Value.collisionEnterData.Count > 0)
				{
					// both colliders have rbody
					if (!SupportsInteraction(INTERACTION.COLL_IS_STATIC) && rbodyCollListeners.Count > 0)
					{
						float theirMaxCollMomentum = 0.0f;
						float maxTotalMomentum = 0.0f;
						CollisionEnterData strongestColl = null;
						foreach (CollisionEnterData collEnterData in objInteractions.Value.collisionEnterData)
						{
							if (collEnterData.myEventData.collision.contacts.Length == 0) continue;

							if (collEnterData.myEventData.collMomentum > myMaxCollMomentum) myMaxCollMomentum = collEnterData.myEventData.collMomentum;
							if (collEnterData.theirEventData.collMomentum > theirMaxCollMomentum) theirMaxCollMomentum = collEnterData.theirEventData.collMomentum;
							float totalMomentum = collEnterData.GetTotalMomentum();
							if (totalMomentum > maxTotalMomentum)
							{
								strongestColl = collEnterData;
								maxTotalMomentum = totalMomentum;
								impactPoint = collEnterData.myEventData.collision.contacts[0].point;
							}
						}


						if (strongestColl != null)
						{
							//strongestColl = objInteractions.Value.collisionEnterData[0];
							//impactPoint = strongestColl.myEventData.collision.contacts[0].point;
							foreach (IRbodyCollisions rbodyCollListener in rbodyCollListeners)
								rbodyCollListener.NonStaticColl(strongestColl, objInteractions.Key, myMaxCollMomentum, theirMaxCollMomentum, impactPoint, collAndTriggerData);
						}

					}
					else
					{
						// im static, they have rbody
						if (objInteractions.Value.collisionEnterData.Count > 0 && staticCollListeners.Count > 0)
						{
							CollisionEnterData strongestColl = null;
							float theirMaxCollMomentum = 0.0f;
							foreach (CollisionEnterData collEnterData in objInteractions.Value.collisionEnterData)
							{
								if (collEnterData.myEventData.collision.contacts.Length == 0) continue;
								if (collEnterData.theirEventData.collMomentum > theirMaxCollMomentum)
								{
									strongestColl = collEnterData;
									theirMaxCollMomentum = collEnterData.theirEventData.collMomentum;
									impactPoint = collEnterData.theirEventData.collision.contacts[0].point;
								}
							}

							if (strongestColl != null)
							{
								//strongestColl = objInteractions.Value.collisionEnterData[0];
								//impactPoint = strongestColl.theirEventData.collision.contacts[0].point;
								foreach (IStaticCollisions staticCollListener in staticCollListeners)
									staticCollListener.Collision(strongestColl, objInteractions.Key, theirMaxCollMomentum, impactPoint, collAndTriggerData);
							}

						}
					}
				}
			}
			else
			{
				// their collision is static, so I have rbody
				// iterate over collisions to determine strongest point of impact and collision momentum regarding only your object
				if (objInteractions.Value.collisionEnterData.Count > 0 && rbodyCollListeners.Count > 0)
				{
					CollisionEnterData strongestColl = null;
					foreach (CollisionEnterData collEnterData in objInteractions.Value.collisionEnterData)
					{
						if (collEnterData.myEventData.collision.contacts.Length == 0) continue;
						if (collEnterData.myEventData.collMomentum > myMaxCollMomentum)
						{
							strongestColl = collEnterData;
							myMaxCollMomentum = collEnterData.myEventData.collMomentum;
							impactPoint = collEnterData.myEventData.collision.contacts[0].point;
						}
					}

					if (strongestColl != null)
					{
						//strongestColl = objInteractions.Value.collisionEnterData[0];
						//impactPoint = strongestColl.myEventData.collision.contacts[0].point;
						foreach (IRbodyCollisions rbodyCollListener in rbodyCollListeners)
							rbodyCollListener.StaticColl(strongestColl, objInteractions.Key, myMaxCollMomentum, impactPoint, collAndTriggerData);
					}

				}
			}


		}

		SendAllCollAndTriggerCallbacks(collAndTriggerData);
	}

	protected void SendAllCollAndTriggerCallbacks(CollisionAndTriggerInfoBundle collAndTriggerData)
	{
		// send all trigger callbacks
		foreach (TriggerEnterData triggerEnterData in collAndTriggerData.eventBasedInteractions.triggerEnterData)
		{
			if (triggerEnterData.myEventData.callback != null)
			{
				triggerEnterData.myEventData.callback(triggerEnterData, collAndTriggerData);
				triggerEnterData.SetAsProcessed();
			}
		}
		foreach (TriggerStayExitData triggerStayData in collAndTriggerData.eventBasedInteractions.triggerStayData)
		{
			if (triggerStayData.myEventData.callback != null)
			{
				triggerStayData.myEventData.callback(triggerStayData, collAndTriggerData);
				triggerStayData.SetAsProcessed();
			}
		}
		foreach (TriggerStayExitData triggerExitData in collAndTriggerData.eventBasedInteractions.triggerExitData)
		{
			if (triggerExitData.myEventData.callback != null)
			{
				triggerExitData.myEventData.callback(triggerExitData, collAndTriggerData);
				triggerExitData.SetAsProcessed();
			}
		}

		// send all collision callbacks
		foreach (CollisionEnterData collEnterData in collAndTriggerData.eventBasedInteractions.collisionEnterData)
		{
			if (collEnterData.myEventData.callback != null)
			{
				collEnterData.myEventData.callback(collEnterData, collAndTriggerData);
				collEnterData.SetAsProcessed();
			}
		}
		foreach (CollisionData collStayData in collAndTriggerData.eventBasedInteractions.collisionStayData)
		{
			if (collStayData.myEventData.callback != null)
			{
				collStayData.myEventData.callback(collStayData, collAndTriggerData);
				collStayData.SetAsProcessed();
			}
		}
		foreach (CollisionData collExitData in collAndTriggerData.eventBasedInteractions.collisionExitData)
		{
			if (collExitData.myEventData.callback != null)
			{
				collExitData.myEventData.callback(collExitData, collAndTriggerData);
				collExitData.SetAsProcessed();
			}
		}
	}
	#endregion


}
