using UnityEngine;
using MonSumo.Core.Enums;

namespace MonSumo.Data
{
    [CreateAssetMenu(fileName = "NewPlayerData", menuName = "MonSumo/Player Data", order = 1)]
    public class PlayerDataSO : ScriptableObject
    {
        [Header("Yokai Identity")]
        public YokaiType yokaiType;
        public string yokaiName;
        public Sprite lobbyIcon;
        public RuntimeAnimatorController animatorController;

        [Header("Stats")]
        public float baseMass = 10f;
        public float baseSpeed = 5f;
        public float basePushForce = 5f;

        [Header("Stamina System")]
        public float maxStamina = 100f;
        public float staminaRegenPerSecond = 120f;

        [Header("Dash settings")]
        public float dashStaminaCost = 10f;
        public float dashCooldown = 3f;
        public float dashSpeedMultiplier = 4f;
        public float dashDuration = 0.2f;

        [Header("Sprint settings")]
        public float sprintStaminaCostPerSecond = 2f;
        public float sprintSpeedMultiplier = 1.5f;

        [Header("Combat settings")]
        public float pushCooldown = 3f;
        public float skillCooldown = 5f;
        public float knockbackDuration = 0.3f;
        public float meleePushForce = 8f;
        public float dashPushForce = 18f;
        public float skillPushForce = 20f;
    }
}
