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
        Vector2 direction = GetActivationDirection(origin);
        Vector2 spawnPosition = (Vector2)origin.position + direction * spawnForwardOffset;

        ProjectilePush projectile = projectilePrefab != null
            ? Instantiate(projectilePrefab, spawnPosition, Quaternion.identity)
            : CreateGreyboxProjectile(spawnPosition);

        projectile.Launch(direction, projectileSpeed, pushForce, lifeTime, owner.gameObject, targetMask);
    }

    private ProjectilePush CreateGreyboxProjectile(Vector2 position)
    {
        GameObject projectileObject = new GameObject("Greybox Push Projectile");
        projectileObject.name = "Greybox Push Projectile";
        projectileObject.transform.position = position;

        CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        return projectileObject.AddComponent<ProjectilePush>();
    }

    private Vector2 GetActivationDirection(Transform origin)
    {
        Vector2 direction = origin.right;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = origin.up;
        }

        return direction.normalized;
    }
}
