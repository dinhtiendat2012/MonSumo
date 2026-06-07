using UnityEngine;

[CreateAssetMenu(menuName = "MonSumo/Skills/Timed Bomb Push Skill")]
public class TimedBombSkill : SkillDefinition
{
    [SerializeField] private TimedBomb bombPrefab;
    [SerializeField] private float pushForce = 16f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float fuseTime = 2.5f;
    [SerializeField] private float placeForwardOffset = 1f;
    [SerializeField] private LayerMask targetMask = ~0;

    public override void Activate(PlayerInventory owner)
    {
        Transform origin = owner.SkillOrigin;
        Vector2 direction = GetActivationDirection(origin);
        Vector2 placePosition = (Vector2)origin.position + direction * placeForwardOffset;

        TimedBomb bomb = bombPrefab != null
            ? Instantiate(bombPrefab, placePosition, Quaternion.identity)
            : CreateGreyboxBomb(placePosition);

        bomb.Arm(fuseTime, explosionRadius, pushForce, targetMask);
    }

    private TimedBomb CreateGreyboxBomb(Vector2 position)
    {
        GameObject bombObject = new GameObject("Greybox Timed Bomb");
        bombObject.name = "Greybox Timed Bomb";
        bombObject.transform.position = position;

        BoxCollider2D collider = bombObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;

        Rigidbody2D body = bombObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;

        return bombObject.AddComponent<TimedBomb>();
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
