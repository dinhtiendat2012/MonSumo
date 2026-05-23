using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private KeyCode activationKey = KeyCode.Space;
    [SerializeField] private bool allowMouseLeftClick = true;
    [SerializeField] private Transform skillOrigin;

    private SkillDefinition currentSkill;

    public bool HasSkill => currentSkill != null;
    public Transform SkillOrigin => skillOrigin != null ? skillOrigin : transform;

    private void Awake()
    {
        if (skillOrigin == null)
        {
            skillOrigin = transform;
        }
    }

    private void Update()
    {
        if (!HasSkill)
        {
            return;
        }

        bool pressedKey = Input.GetKeyDown(activationKey);
        bool pressedMouse = allowMouseLeftClick && Input.GetMouseButtonDown(0);

        if (pressedKey || pressedMouse)
        {
            ActivateCurrentSkill();
        }
    }

    // Pick up an item. The inventory can hold only one skill at a time.
    public bool TryPickup(ItemPickup item)
    {
        if (item == null || HasSkill)
        {
            return false;
        }

        currentSkill = item.Skill;
        return currentSkill != null;
    }

    // Activate the current skill and clear it from the inventory after use.
    private void ActivateCurrentSkill()
    {
        SkillDefinition skillToUse = currentSkill;
        currentSkill = null;

        if (skillToUse != null)
        {
            skillToUse.Activate(this);
        }
    }
}
