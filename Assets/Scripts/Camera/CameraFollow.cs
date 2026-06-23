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
    [SerializeField] private bool autoDetectBounds = true;

    [Tooltip("Góc dưới bên trái của map (Sẽ tự động cập nhật nếu bật Auto Detect)")]
    [SerializeField] private Vector2 mapMin = new Vector2(-22f, -14.5f);

    [Tooltip("Góc trên bên phải của map (Sẽ tự động cập nhật nếu bật Auto Detect)")]
    [SerializeField] private Vector2 mapMax = new Vector2(22f, 14.5f);

    public Vector2 MapMin => mapMin;
    public Vector2 MapMax => mapMax;


    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (autoDetectBounds)
        {
            AutoDetectMapBounds();
        }

        // Nếu target đã được kéo sẵn trong Inspector,
        // camera sẽ nhảy ngay tới nhân vật khi vào game.
        ForceSnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.LocalClient != null)
            {
                var localObj = Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localObj != null)
                {
                    SetTarget(localObj.transform);
                }
            }
        }

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

    public void AutoDetectMapBounds()
    {
        Vector2 detectedMin = mapMin;
        Vector2 detectedMax = mapMax;
        bool found = false;

        // 1. Try to find by name first
        string[] commonNames = { "Background", "Map", "Arena", "Stage", "Onsen", "Playground", "Grid", "Tilemap" };
        foreach (var name in commonNames)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                if (go.transform is RectTransform || go.layer == 5) continue; // Skip UI

                if (go.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null)
                {
                    detectedMin = sr.bounds.min;
                    detectedMax = sr.bounds.max;
                    found = true;
                    break;
                }
                if (go.TryGetComponent<Collider2D>(out var col))
                {
                    detectedMin = col.bounds.min;
                    detectedMax = col.bounds.max;
                    found = true;
                    break;
                }
            }
        }

        // 2. Search for any SpriteRenderer that is not UI and is large
        if (!found)
        {
            var allRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            SpriteRenderer bestSr = null;
            float maxArea = 0f;
            foreach (var sr in allRenderers)
            {
                if (sr.gameObject.layer == 5 || sr.transform is RectTransform) continue; // Skip UI
                if (sr.gameObject.CompareTag("Player")) continue; // Skip player
                if (sr.gameObject.name == "ZoneVignetteOverlay") continue; // Skip large overlay shader vignetting

                float area = sr.bounds.size.x * sr.bounds.size.y;
                if (area > maxArea && area > 10f)
                {
                    maxArea = area;
                    bestSr = sr;
                }
            }

            if (bestSr != null)
            {
                detectedMin = bestSr.bounds.min;
                detectedMax = bestSr.bounds.max;
                found = true;
            }
        }

        // 3. Search for any Collider2D that is large and not UI/player
        if (!found)
        {
            var allColliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            Collider2D bestCol = null;
            float maxColArea = 0f;
            foreach (var col in allColliders)
            {
                if (col.gameObject.layer == 5 || col.transform is RectTransform) continue; // Skip UI
                if (col.gameObject.CompareTag("Player")) continue; // Skip player

                float area = col.bounds.size.x * col.bounds.size.y;
                if (area > maxColArea && area > 10f)
                {
                    maxColArea = area;
                    bestCol = col;
                }
            }

            if (bestCol != null)
            {
                detectedMin = bestCol.bounds.min;
                detectedMax = bestCol.bounds.max;
                found = true;
            }
        }

        if (found)
        {
            mapMin = detectedMin;
            mapMax = detectedMax;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && autoDetectBounds)
        {
            AutoDetectMapBounds();
        }
    }
#endif

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