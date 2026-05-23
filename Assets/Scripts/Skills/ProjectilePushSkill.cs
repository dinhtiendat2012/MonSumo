using UnityEngine;

[CreateAssetMenu(menuName = "MonSumo/Skills/Projectile Push Skill")]
public class ProjectilePushSkill : SkillDefinition
{
    [SerializeField] private ProjectilePush projectilePrefab;
    [SerializeField] private float pushForce = 12f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float spawnForwardOffset = 1.2f;
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private LayerMask targetMask = ~0;

    public override void Activate(PlayerInventory owner)
    {
        Transform origin = owner.SkillOrigin;
        Vector3 spawnPosition = origin.position + origin.forward * spawnForwardOffset;

        ProjectilePush projectile = projectilePrefab != null
            ? Instantiate(projectilePrefab, spawnPosition, origin.rotation)
            : CreateGreyboxProjectile(spawnPosition, origin.rotation);

        projectile.Launch(origin.forward, projectileSpeed, pushForce, lifeTime, owner.gameObject, targetMask);
    }

    private ProjectilePush CreateGreyboxProjectile(Vector3 position, Quaternion rotation)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "Greybox Push Projectile";
        projectileObject.transform.SetPositionAndRotation(position, rotation);
        projectileObject.transform.localScale = Vector3.one * 0.45f;

        Collider collider = projectileObject.GetComponent<Collider>();
        collider.isTrigger = true;

        Rigidbody body = projectileObject.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        return projectileObject.AddComponent<ProjectilePush>();
    }
}
