using UnityEngine;

public abstract class SkillDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Skill";

    public string DisplayName => displayName;

    // Main entry point called by the inventory when the player activates an item skill.
    public abstract void Activate(PlayerInventory owner);
}
