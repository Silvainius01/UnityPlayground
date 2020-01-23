using UnityEngine;
using System.Collections;

public interface IInteractionGroupListener {
    void ObjRemovedFromInteractionGroup(INTERACTION interactionGroup, InteractionController interactionCont);
    void ObjAddedToInteractionGroup(INTERACTION interactionGroup, InteractionController interactionCont);
}
