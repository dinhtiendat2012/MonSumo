using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private SkillDefinition skill;

    public SkillDefinition Skill => skill;

    private void Reset()
    {
        Collider2D pickupCollider = GetComponent<Collider2D>();
        pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[ITEM] Trigger with {other.name}");

        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();

        Debug.Log($"[ITEM] Inventory = {inventory}");

        if (inventory == null)
        {
            Debug.Log("[ITEM] Inventory NULL");
            return;
        }

        bool picked = inventory.TryPickup(this);

        Debug.Log($"[ITEM] TryPickup = {picked}");

        if (picked)
        {
            Debug.Log("[ITEM] Destroy item");
            Destroy(gameObject);
        }
    }
}
