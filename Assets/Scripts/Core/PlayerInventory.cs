using UnityEngine;
using Unity.Netcode;
using MonSumo.Items;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private KeyCode activationKey = KeyCode.Space;
    [SerializeField] private bool allowMouseLeftClick = true;
    [SerializeField] private Transform skillOrigin;
    [SerializeField] private SkillCatalogSO skillCatalog;

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
        if (!IsOwner) return;

        if (!HasSkill)
        {
            return;
        }

        bool pressedKey = Input.GetKeyDown(activationKey);
        bool pressedMouse = allowMouseLeftClick && Input.GetMouseButtonDown(0);

        if (pressedKey || pressedMouse)
        {
            // Request server to activate the skill
            RequestActivateSkillServerRpc();
        }
    }

    // Pick up an item. Run on Server.
    public bool TryPickup(ItemPickup item)
    {
        if (!IsServer) return false;
        if (item == null || HasSkill)
        {
            return false;
        }

        currentSkill = item.Skill;
        if (currentSkill != null && skillCatalog != null)
        {
            int index = skillCatalog.GetIndex(currentSkill);
            SyncSkillClientRpc(index);
            return true;
        }

        return false;
    }

    [Rpc(SendTo.Owner)]
    private void SyncSkillClientRpc(int index)
    {
        if (index == -1)
        {
            currentSkill = null;
        }
        else if (skillCatalog != null)
        {
            currentSkill = skillCatalog.GetSkill(index);
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestActivateSkillServerRpc()
    {
        if (currentSkill == null) return;

        SkillDefinition skillToUse = currentSkill;
        currentSkill = null;

        // Clear client skill slot
        SyncSkillClientRpc(-1);

        if (skillToUse != null)
        {
            skillToUse.Activate(this);
            Debug.Log($"[INVENTORY] Player {OwnerClientId} activated skill: {skillToUse.name}");
        }
    }
}
