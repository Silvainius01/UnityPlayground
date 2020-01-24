using UnityEngine;
using System.Collections;

public interface IStaticCollisions {
    void Collision(CollisionEnterData collData, InteractionController otherInteractionCont,
                                        float collMomentum, Vector2 impactPoint, CollisionAndTriggerInfoBundle allCollandTriggerData);
}
