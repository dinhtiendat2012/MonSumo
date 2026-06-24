using UnityEngine;
using MonSumo.Core.Enums;

namespace MonSumo.Data
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "MonSumo/Item Data", order = 2)]
    public class ItemDataSO : ScriptableObject
    {
        [Header("Identity")]
        public ItemType itemType;
        public string itemName;
        public Sprite icon;
        public GameObject prefab;

        [Header("Rarity Settings")]
        [SerializeField] private bool _useCustomRarity = false;
        [SerializeField] private ItemRarity _customRarity = ItemRarity.Common;

        public ItemRarity Rarity
        {
            get
            {
                if (_useCustomRarity) return _customRarity;

                switch (itemType)
                {
                    case ItemType.SpeedJuice:
                    case ItemType.HeavyAnchor:
                        return ItemRarity.Common;
                    case ItemType.ThornShield:
                    case ItemType.SuperPush:
                        return ItemRarity.Rare;
                    case ItemType.GodForce:
                        return ItemRarity.Epic;
                    default:
                        return ItemRarity.Common;
                }
            }
        }


        [Header("Spawn Settings")]
        [Range(0f, 1f)]
        public float spawnWeight = 0.5f; // Relative probability of spawning

        [Header("Effect Duration")]
        public float duration = 10f; // Seconds

        [Header("Stat Modifiers")]
        [Tooltip("Percentage modifier for Speed (e.g. 0.3 means +30%)")]
        public float speedModifierPercent = 0f;

        [Tooltip("Percentage modifier for Mass (e.g. 0.5 means +50%)")]
        public float massModifierPercent = 0f;

        [Tooltip("Percentage modifier for Push Force (e.g. 1.0 means +100%)")]
        public float pushForceModifierPercent = 0f;

        [Header("Special Modifiers")]
        [Tooltip("Received push multiplier (e.g. 0.5 means reduce by 50%)")]
        public float receivedPushMultiplier = 1f;

        [Tooltip("Reflected push force percentage (e.g. 0.2 means reflect 20% to attacker)")]
        public float reflectedPushPercent = 0f;
    }
}
