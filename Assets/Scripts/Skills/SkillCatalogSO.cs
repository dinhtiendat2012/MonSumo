using UnityEngine;

/// <summary>
/// ScriptableObject catalog storing all available skills.
/// Allows synchronizing skills over network RPCs/variables via integer indices.
/// </summary>
[CreateAssetMenu(fileName = "SkillCatalog", menuName = "MonSumo/Skill Catalog")]
public class SkillCatalogSO : ScriptableObject
{
    [SerializeField] private SkillDefinition[] skills;

    /// <summary>
    /// Gets a skill definition by its catalog index.
    /// </summary>
    public SkillDefinition GetSkill(int index)
    {
        if (skills == null || index < 0 || index >= skills.Length)
        {
            return null;
        }
        return skills[index];
    }

    /// <summary>
    /// Finds the catalog index for a given skill definition. Returns -1 if not found.
    /// </summary>
    public int GetIndex(SkillDefinition skill)
    {
        if (skill == null || skills == null)
        {
            return -1;
        }

        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i] == skill)
            {
                return i;
            }
        }
        return -1;
    }
}
