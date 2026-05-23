using UnityEngine;

[CreateAssetMenu(menuName = "MonSumo/Skills/Melee Push Skill")]
public class MeleePushSkill : SkillDefinition
{
    [SerializeField] private float pushForce = 14f;
    [SerializeField] private float radius = 2.25f;
    [SerializeField] private float forwardOffset = 1.4f;
    [SerializeField] private float upwardBoost = 0.15f;
    [SerializeField] private LayerMask targetMask = ~0;

    public override void Activate(PlayerInventory owner)
    {
        Transform origin = owner.SkillOrigin;
        Vector3 center = origin.position + origin.forward * forwardOffset;

        // Scan the area in front of the player and push Rigidbody targets away from the activation center.
        Collider[] hits = Physics.OverlapSphere(center, radius, targetMask, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            Rigidbody targetBody = hit.attachedRigidbody;
            if (targetBody == null || targetBody.transform == owner.transform)
            {
                continue;
            }

            PushUtility.ApplyExplosionImpulse(targetBody, center, pushForce, upwardBoost);
        }
    }
}
