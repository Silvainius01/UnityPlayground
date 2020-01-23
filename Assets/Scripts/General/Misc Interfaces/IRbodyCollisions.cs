using UnityEngine;
using System.Collections;

public interface IRbodyCollisions {
    void NonStaticColl(CollisionEnterData collData, InteractionController otherInteractionCont,
                        float myCollMomentum, float theirCollMomentum, Vector2 impactPoint, CollisionAndTriggerInfoBundle allCollandTriggerData);

    void StaticColl(CollisionEnterData collData, InteractionController otherInteractionCont,
                                        float myCollMomentum, Vector2 impactPoint, CollisionAndTriggerInfoBundle allCollandTriggerData);
}
