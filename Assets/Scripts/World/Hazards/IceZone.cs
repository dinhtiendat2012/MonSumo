using MonSumo.Core;
using Unity.Netcode;
using UnityEngine;

public class IceZone : NetworkBehaviour
{
    [Header("Ice Settings")]
    [Tooltip("Lực đẩy của băng áp dụng lên Player")]
    [SerializeField] private float iceForce = 30f;

  
    private void OnTriggerStay2D(UnityEngine.Collider2D collision)
    {
        if (!IsServer) return;
        if(collision.CompareTag("Player"))
        {
            Player targetPlayer = collision.GetComponent<Player>();
            if (targetPlayer == null)
                return;

            PlayerMovement targetMovement = collision.GetComponent<PlayerMovement>();
            Vector2 faceDir = targetMovement.GetFacingDirection(); // hướng input hoặc hướng di chuyển hiện tại

            if (faceDir == Vector2.zero)
                return;
            if(targetMovement.GetMoveInput() == Vector2.zero)
            {
                targetPlayer.ApplyKnockbackRpc(faceDir.normalized * iceForce);
                Debug.Log($"Player {targetPlayer.playerName.Value} is being pushed by ice in direction {faceDir.normalized * iceForce}");
            }
        }

    }
}
