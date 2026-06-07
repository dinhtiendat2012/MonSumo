using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ProjectilePush : MonoBehaviour
{
    private Rigidbody2D body;
    private GameObject owner;
    private Vector2 moveDirection;
    private float pushForce;
    private LayerMask targetMask;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    // Initialize the projectile after it is spawned by a skill.
    public void Launch(Vector2 direction, float speed, float force, float lifeTime, GameObject ownerObject, LayerMask mask)
    {
        owner = ownerObject;
        moveDirection = direction.normalized;
        pushForce = force;
        targetMask = mask;
        body.linearVelocity = moveDirection * speed;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.transform.IsChildOf(owner.transform))
        {
            return;
        }

        if ((targetMask.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        Rigidbody2D targetBody = other.attachedRigidbody;
        if (targetBody == null)
        {
            return;
        }

        PushUtility.ApplyImpulse(targetBody, moveDirection, pushForce);
        Destroy(gameObject);
    }
}
