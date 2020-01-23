using UnityEngine;
using System.Collections.Generic;

public class InteractionGroups : MonoBehaviour {

    public delegate void InteractionGroupChangedFcn();
    private class InteractionGroup
    {
        public List<InteractionController> objsInGroup = new List<InteractionController>();
        public List<IInteractionGroupListener> groupListeners = new List<IInteractionGroupListener>();
    }

    // private variables
    bool initialized = false;
    private static InteractionGroups interactionGroups;
	private Dictionary<INTERACTION, InteractionGroup> groupLists;


    public static InteractionGroups instance
    {
        get { return interactionGroups; }
    }

    private void Awake()
    {
        if (interactionGroups != null && interactionGroups != this)
        {
            Destroy(gameObject);
        }
        else
        {
            interactionGroups = this;
            Init();
        }
    }

    void Init()
    {
        if (!initialized)
        {
            initialized = true;
            groupLists = new Dictionary<INTERACTION, InteractionGroup>(new InteractionEnumComparer());
        }
    }

    public void RegisterWithGroup(INTERACTION group, InteractionController interactionCont)
    {
        InteractionGroup interactionGroup = null;
        if(!groupLists.TryGetValue(group, out interactionGroup))
        {
            interactionGroup = new InteractionGroup();
            groupLists[group] = interactionGroup;
        }

        // add obj to group, avoid duplicates
        if (interactionGroup.objsInGroup.Contains(interactionCont)) return;
        else {
            interactionGroup.objsInGroup.Add(interactionCont);
            foreach(IInteractionGroupListener listener in interactionGroup.groupListeners)
            {
                listener.ObjAddedToInteractionGroup(group, interactionCont);
            }
        }
    }

    public void UnregisterWithGroup(INTERACTION group, InteractionController interactionCont)
    {
        InteractionGroup interactionGroup = null;
        if (!groupLists.TryGetValue(group, out interactionGroup) || interactionGroup.objsInGroup.Count == 0)
        {
            return;
        }
        if (interactionGroup.objsInGroup.Remove(interactionCont))
        {
            foreach (IInteractionGroupListener listener in interactionGroup.groupListeners)
            {
                listener.ObjRemovedFromInteractionGroup(group, interactionCont);
            }
        }
    }

    public void RegisterGroupChangedListener(INTERACTION group, IInteractionGroupListener listener)
    {
        InteractionGroup interactionGroup = null;
        if (!groupLists.TryGetValue(group, out interactionGroup))
        {
            interactionGroup = new InteractionGroup();
            groupLists[group] = interactionGroup;
        }

        // add to group listers, avoid duplicates
        if (interactionGroup.groupListeners.Contains(listener)) return;
        else interactionGroup.groupListeners.Add(listener);
    }

    public void UnRegisterGroupChangedListener(INTERACTION group, IInteractionGroupListener listener)
    {
        InteractionGroup interactionGroup = null;
        if (!groupLists.TryGetValue(group, out interactionGroup) || interactionGroup.groupListeners.Count == 0)
        {
            return;
        }
        interactionGroup.groupListeners.Remove(listener);
    }

    public static List<InteractionController> GetObjsWithInteraction(INTERACTION group)
    {
        InteractionGroup interactionGroup = null;
        if (!instance.groupLists.TryGetValue(group, out interactionGroup))
        {
            return new List<InteractionController>();
        }

        return interactionGroup.objsInGroup;
    }
}
