using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using MonSumo.Core;

namespace MonSumo.World.Hazards
{
    public class OnsenAreaModifier : MonoBehaviour
    {
        public enum ModifierType
        {
            BathSlow,       // Slow player speed by 50%
            WaterfallFlow,   // Gently drift player in a direction
            WavePulse       // Periodically push player strongly in a direction
        }

        [Header("Area Type")]
        [SerializeField] private ModifierType _modifierType = ModifierType.BathSlow;

        [Header("Waterfall / Wave Settings")]
        [SerializeField] private Vector2 _pushDirection = Vector2.down;
        [SerializeField] private float _constantPushForce = 5f;
        [SerializeField] private float _waveInterval = 8f; // Wave every 8s
        [SerializeField] private float _wavePushForce = 15f;

        [Header("Bath Settings")]
        [SerializeField] [Range(0f, 1f)] private float _slowSpeedMultiplier = 0.5f;

        private float _waveTimer;
        private readonly List<PlayerMovement> _affectedMovements = new();
        private readonly List<Rigidbody2D> _affectedRigidbodies = new();

        private void Update()
        {
            if (_modifierType == ModifierType.WavePulse)
            {
                _waveTimer += Time.deltaTime;
                if (_waveTimer >= _waveInterval)
                {
                    _waveTimer = 0f;
                    TriggerWavePulse();
                }
            }
        }

        private void FixedUpdate()
        {
            if (_modifierType == ModifierType.WaterfallFlow)
            {
                ApplyConstantPush();
            }
        }

        private void TriggerWavePulse()
        {
            // Waves push players on the server, sending knockback RPCs
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            foreach (var movement in _affectedMovements)
            {
                if (movement == null) continue;
                var player = movement.GetComponent<Player>();
                if (player != null)
                {
                    player.ApplyKnockbackRpc(_pushDirection.normalized * _wavePushForce);
                    Debug.Log($"[Onsen Hazard] Wave hit player {player.OwnerClientId} with force {_wavePushForce}");
                }
            }
        }

        private void ApplyConstantPush()
        {
            foreach (var rb in _affectedRigidbodies)
            {
                if (rb != null)
                {
                    rb.AddForce(_pushDirection.normalized * _constantPushForce, ForceMode2D.Force);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var movement = other.GetComponentInParent<PlayerMovement>();
            if (movement != null && !_affectedMovements.Contains(movement))
            {
                _affectedMovements.Add(movement);

                var rb = movement.GetComponent<Rigidbody2D>();
                if (rb != null) _affectedRigidbodies.Add(rb);

                // Apply Slow effect
                if (_modifierType == ModifierType.BathSlow)
                {
                    movement.SetAreaSpeedMultiplier(_slowSpeedMultiplier);
                    Debug.Log($"[Onsen Hazard] Player {movement.OwnerClientId} entered bath slow area.");
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var movement = other.GetComponentInParent<PlayerMovement>();
            if (movement != null)
            {
                _affectedMovements.Remove(movement);

                var rb = movement.GetComponent<Rigidbody2D>();
                if (rb != null) _affectedRigidbodies.Remove(rb);

                // Revert Slow effect
                if (_modifierType == ModifierType.BathSlow)
                {
                    movement.SetAreaSpeedMultiplier(1.0f);
                    Debug.Log($"[Onsen Hazard] Player {movement.OwnerClientId} exited bath slow area.");
                }
            }
        }
    }
}
