using UnityEngine;
using Unity.Netcode;
using MonSumo.Core;

namespace MonSumo.World.Hazards
{
    [RequireComponent(typeof(Collider2D))]
    public class OnsenWaterHazard : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Only server handles damage
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            var player = other.GetComponentInParent<Player>();
            if (player != null)
            {
                Debug.Log($"[Onsen Hazard] Player {player.OwnerClientId} fell into the deep pool!");
                player.TakeDamage();
            }
        }
    }
}
