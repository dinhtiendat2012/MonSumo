using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ZoneController : MonoBehaviour
{
    [Header("Zone Visual")]
    [SerializeField] private int segments = 128;
    [SerializeField] private float lineWidth = 0.15f;
    [SerializeField] private Color zoneColor = Color.cyan;

    [Header("Zone Size")]
    [SerializeField] private float startRadius = 10f;
    [SerializeField] private float endRadius = 3f;

    [Header("Zone Timing")]
    [SerializeField] private float delayBeforeShrink = 5f;
    [SerializeField] private float shrinkDuration = 120f;

    private LineRenderer lineRenderer;
    private float currentRadius;
    private float shrinkTimer;
    private bool isShrinking;

    public float CurrentRadius => currentRadius;
    public Vector2 Center => transform.position;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        lineRenderer.startColor = zoneColor;
        lineRenderer.endColor = zoneColor;

        lineRenderer.sortingOrder = 50;
    }

    private void Start()
    {
        currentRadius = startRadius;
        DrawCircle(currentRadius);

        Invoke(nameof(StartShrink), delayBeforeShrink);
    }

    private void Update()
    {
        if (!isShrinking) return;

        shrinkTimer += Time.deltaTime;

        float t = Mathf.Clamp01(shrinkTimer / shrinkDuration);
        currentRadius = Mathf.Lerp(startRadius, endRadius, t);

        DrawCircle(currentRadius);
    }

    private void StartShrink()
    {
        isShrinking = true;
        shrinkTimer = 0f;
    }

    private void DrawCircle(float radius)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = ((float)i / segments) * Mathf.PI * 2f;

            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    public bool IsInsideZone(Vector2 position)
    {
        float distance = Vector2.Distance(position, Center);
        return distance <= currentRadius;
    }
}