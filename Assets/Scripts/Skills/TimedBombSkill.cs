using UnityEngine;

[CreateAssetMenu(menuName = "MonSumo/Skills/Timed Bomb Push Skill")]
public class TimedBombSkill : SkillDefinition
{
    [SerializeField] private TimedBomb bombPrefab;
    [SerializeField] private float pushForce = 16f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float fuseTime = 2.5f;
    [SerializeField] private float placeForwardOffset = 1f;
    [SerializeField] private float upwardBoost = 0.25f;
    [SerializeField] private LayerMask targetMask = ~0;

    public override void Activate(PlayerInventory owner)
    {
        Transform origin = owner.SkillOrigin;
        Vector3 placePosition = origin.position + origin.forward * placeForwardOffset;

        TimedBomb bomb = bombPrefab != null
            ? Instantiate(bombPrefab, placePosition, Quaternion.identity)
            : CreateGreyboxBomb(placePosition);

        bomb.Arm(fuseTime, explosionRadius, pushForce, upwardBoost, targetMask);
    }

    private TimedBomb CreateGreyboxBomb(Vector3 position)
    {
        GameObject bombObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bombObject.name = "Greybox Timed Bomb";
        bombObject.transform.position = position;
        bombObject.transform.localScale = Vector3.one * 0.7f;

        Rigidbody body = bombObject.AddComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeRotation;

        return bombObject.AddComponent<TimedBomb>();
    }
}
