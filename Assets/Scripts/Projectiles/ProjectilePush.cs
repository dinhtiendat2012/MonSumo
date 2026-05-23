using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ProjectilePush : MonoBehaviour
{
    private Rigidbody body;
    private GameObject owner;
    private Vector3 moveDirection;
    private float pushForce;
    private LayerMask targetMask;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        GetComponent<Collider>().isTrigger = true;
    }

    // Initialize the projectile after it is spawned by a skill.
    public void Launch(Vector3 direction, float speed, float force, float lifeTime, GameObject ownerObject, LayerMask mask)
    {
        owner = ownerObject;
        moveDirection = direction.normalized;
        pushForce = force;
        targetMask = mask;
        body.linearVelocity = moveDirection * speed;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner != null && other.transform.IsChildOf(owner.transform))
        {
            return;
        }

        if ((targetMask.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        Rigidbody targetBody = other.attachedRigidbody;
        if (targetBody == null)
        {
            return;
        }

        PushUtility.ApplyImpulse(targetBody, moveDirection, pushForce);
        Destroy(gameObject);
    }
}
