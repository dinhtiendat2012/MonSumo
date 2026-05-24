using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private float smoothTime = 0.08f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Map Bounds")]
    [SerializeField] private bool useBounds = true;

    [Tooltip("Góc dưới bên trái của map")]
    [SerializeField] private Vector2 mapMin = new Vector2(-22f, -14.5f);

    [Tooltip("Góc trên bên phải của map")]
    [SerializeField] private Vector2 mapMax = new Vector2(22f, 14.5f);

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        // Nếu target đã được kéo sẵn trong Inspector,
        // camera sẽ nhảy ngay tới nhân vật khi vào game.
        ForceSnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = GetDesiredCameraPosition();

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime
        );
    }

    private Vector3 GetDesiredCameraPosition()
    {
        Vector3 desiredPosition = target.position + offset;

        if (useBounds)
        {
            desiredPosition = ClampCameraPosition(desiredPosition);
        }

        desiredPosition.z = offset.z;
        return desiredPosition;
    }

    private Vector3 ClampCameraPosition(Vector3 desiredPosition)
    {
        float cameraHalfHeight = cam.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * cam.aspect;

        float minX = mapMin.x + cameraHalfWidth;
        float maxX = mapMax.x - cameraHalfWidth;

        float minY = mapMin.y + cameraHalfHeight;
        float maxY = mapMax.y - cameraHalfHeight;

        // Nếu map nhỏ hơn vùng nhìn của camera, giữ camera ở giữa map
        if (minX > maxX)
        {
            desiredPosition.x = (mapMin.x + mapMax.x) / 2f;
        }
        else
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        }

        if (minY > maxY)
        {
            desiredPosition.y = (mapMin.y + mapMax.y) / 2f;
        }
        else
        {
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        desiredPosition.z = offset.z;
        return desiredPosition;
    }

    public void SetTarget(Transform newTarget, bool snapImmediately = true)
    {
        target = newTarget;

        if (snapImmediately)
        {
            ForceSnapToTarget();
        }
    }

    public void ForceSnapToTarget()
    {
        if (target == null) return;

        transform.position = GetDesiredCameraPosition();

        // Reset velocity để SmoothDamp không bị kéo theo quán tính cũ
        velocity = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 center = new Vector3(
            (mapMin.x + mapMax.x) / 2f,
            (mapMin.y + mapMax.y) / 2f,
            0f
        );

        Vector3 size = new Vector3(
            mapMax.x - mapMin.x,
            mapMax.y - mapMin.y,
            0f
        );

        Gizmos.DrawWireCube(center, size);
    }
}