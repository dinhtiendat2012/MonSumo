using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private SkillDefinition skill;

    public SkillDefinition Skill => skill;

    private void Reset()
    {
        Collider pickupCollider = GetComponent<Collider>();
        pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null)
        {
            return;
        }

        if (inventory.TryPickup(this))
        {
            Destroy(gameObject);
        }
    }
}
