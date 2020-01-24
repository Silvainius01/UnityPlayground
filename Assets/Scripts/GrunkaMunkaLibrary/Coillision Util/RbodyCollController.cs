using UnityEngine;
using System.Collections;

// this class is responsible for making velocity, angular velocity, and mass available for non static collision calculations
public class RbodyCollController : CollController {

    [SerializeField]
    public Rigidbody2D rbody { get; protected set; }
    public Vector2 lastVel { get; protected set; }
    public float lastAngularVel { get; protected set; }
    public float lastMass { get; protected set; }
    public float currSpeed { get
        {
            return rbody.velocity.magnitude;
        }
    }
    public Vector2 currVel
    {
        get
        {
            return rbody.velocity;
        }
    }
    public Vector2 worldCenterOfMass
    {
        get
        {
            return rbody.worldCenterOfMass;
        }
    }

    public virtual void Awake()
    {
        rbody = GetComponent<Rigidbody2D>();
    }

    public virtual void FixedUpdate()
    {
        // grab most recent collision related info for use in collision momentum calculations
        lastVel = rbody.velocity;
        lastAngularVel = rbody.angularVelocity;
        lastMass = rbody.mass;
    }

}
