using MonSumo.Core;
using Unity.Netcode;
using UnityEngine;

public class TurnamiSkill : NetworkBehaviour
{
    [Header("Tsunami Settings")]
    [Tooltip("Lực đẩy của sóng thần áp dụng lên Player")]
    [SerializeField] private float pushForce = 15f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Chỉ xử lý logic đẩy trên Server để đảm bảo tính đồng bộ hệ thống Network
        if (!IsServer) return;

        // Kiểm tra xem đối tượng va chạm có chứa component Player hay không
        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            // Tính toán hướng từ gốc (tâm của Tsunami GameObject) đến vị trí của Player
            Vector2 originPosition = transform.position;
            Vector2 playerPosition = player.transform.position;

            Vector2 pushDirection = (playerPosition - originPosition).normalized;

            // Nếu trùng vị trí tuyệt đối (hướng bằng 0), mặc định đẩy lên trên
            if (pushDirection.sqrMagnitude < 0.001f)
            {
                pushDirection = Vector2.up;
            }

            // Áp dụng hệ số giảm/tăng lực đẩy từ item nếu Player đang có (ví dụ: Thorn Shield)
            float finalForce = pushForce;
            if (player.ReceivedPushMultiplier != 1f)
            {
                finalForce *= player.ReceivedPushMultiplier;
            }

            // Gọi RPC áp dụng lực đẩy (Knockback) lên Player thông qua Rigidbody2D của họ
            player.ApplyKnockbackRpc(pushDirection * finalForce);

            Debug.Log($"[Tsunami] Đã đẩy Player {player.OwnerClientId} theo hướng {pushDirection} với lực {finalForce}");
        }
    }
}
