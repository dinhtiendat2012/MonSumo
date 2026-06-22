using UnityEngine;
using Unity.Netcode;
using MonSumo.Core;
using System.Collections.Generic;
using VContainer;

namespace MonSumo.World.Zone
{
    [RequireComponent(typeof(LineRenderer))]
    public class ZoneController : NetworkBehaviour
    {
        [Header("Zone Visual")]
        [SerializeField] private int segments = 128;
        [SerializeField] private float lineWidth = 0.15f;
        [SerializeField] private Color zoneColor = Color.red;

        [Header("Zone Size Settings")]
        [SerializeField] private float startRadius = 15f;
        [SerializeField] private float endRadius = 2f;

        [Header("Zone Boundaries")]
        [SerializeField] private bool autoDetectBounds = true;
        [Tooltip("Góc dưới bên trái của map (Sẽ tự động cập nhật nếu bật Auto Detect)")]
        [SerializeField] private Vector2 mapMin = new Vector2(-22f, -14.5f);
        [Tooltip("Góc trên bên phải của map (Sẽ tự động cập nhật nếu bật Auto Detect)")]
        [SerializeField] private Vector2 mapMax = new Vector2(22f, 14.5f);

        // Networked properties to sync with all clients
        public readonly NetworkVariable<float> currentRadius = new(
            15f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public readonly NetworkVariable<Vector2> currentCenter = new(
            Vector2.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private LineRenderer lineRenderer;

        // Timers and State (Server-only)
        private float _gameTimer;
        private float _moveTimer;
        private bool _isShrinkingStarted;
        private bool _isMovingStarted;
        private bool _isSpeedingUpMoving;
        private Vector2 _moveDirection;

        [Header("Zone Timing & Speed Settings")]
        [SerializeField] private float shrinkStartSecond = 180f; // 3 minutes
        [SerializeField] private float shrinkSpeedUpSecond = 540f; // 9 minutes
        [SerializeField] private float moveSpeedUpDelay = 120f; // 2 minutes after moving starts
        [SerializeField] private float baseShrinkSpeed = 2f; // units per minute
        [SerializeField] private float speedUpBaseShrinkSpeed = 5f; // units per minute after 9 minutes
        [SerializeField] private float shrinkAcceleration = 0.05f; // units/s increase rate
        [SerializeField] private float baseMoveSpeed = 5f; // units per minute
        [SerializeField] private float moveAcceleration = 0.05f; // units/s increase rate

        public float CurrentRadius => currentRadius.Value;
        public float StartRadius => startRadius;
        public Vector2 Center => currentCenter.Value;

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

        public override void OnNetworkSpawn()
        {
            currentRadius.OnValueChanged += HandleRadiusChanged;
            currentCenter.OnValueChanged += HandleCenterChanged;

            if (autoDetectBounds)
            {
                AutoDetectMapBounds();
            }

            if (IsServer)
            {
                currentRadius.Value = startRadius;
                currentCenter.Value = (Vector2)transform.position;
                _gameTimer = 0f;
                _moveTimer = 0f;
                _isShrinkingStarted = false;
                _isMovingStarted = false;
                _isSpeedingUpMoving = false;
            }
            else
            {
                transform.position = (Vector3)currentCenter.Value;
                DrawCircle(currentRadius.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            currentRadius.OnValueChanged -= HandleRadiusChanged;
            currentCenter.OnValueChanged -= HandleCenterChanged;
        }

        private void HandleRadiusChanged(float previousValue, float newValue)
        {
            DrawCircle(newValue);
        }

        private void HandleCenterChanged(Vector2 previousValue, Vector2 newValue)
        {
            transform.position = (Vector3)newValue;
            DrawCircle(currentRadius.Value);
        }

        private void Update()
        {
            if (IsServer)
            {
                UpdateServerZone();
            }

            // Sync visual position
            transform.position = (Vector3)currentCenter.Value;
        }

        private void UpdateServerZone()
        {
            _gameTimer += Time.deltaTime;

            // 1. Handle Shrinking
            if (_gameTimer >= shrinkStartSecond)
            {
                if (!_isShrinkingStarted)
                {
                    _isShrinkingStarted = true;
                    TriggerAlertClientRpc("Vòng bo đang bắt đầu thu nhỏ lại!");
                    
                    // Start moving zone as well when shrinking starts
                    _isMovingStarted = true;
                    TriggerAlertClientRpc("Vòng bo đang bắt đầu dịch chuyển!");
                    InitializeMoveDirection();
                }

                // Calculate shrink speed (units/minute divided by 60)
                float shrinkSpeedMin;
                if (_gameTimer < shrinkSpeedUpSecond)
                {
                    shrinkSpeedMin = baseShrinkSpeed;
                }
                else
                {
                    float secondsSinceNineMin = _gameTimer - shrinkSpeedUpSecond;
                    shrinkSpeedMin = speedUpBaseShrinkSpeed + shrinkAcceleration * secondsSinceNineMin;
                }

                float shrinkSpeedSec = shrinkSpeedMin / 60f;
                currentRadius.Value = Mathf.Max(endRadius, currentRadius.Value - shrinkSpeedSec * Time.deltaTime);
            }

            // 2. Handle Zone Center Movement
            if (_isMovingStarted)
            {
                _moveTimer += Time.deltaTime;

                float moveSpeedMin = baseMoveSpeed;
                if (_moveTimer >= moveSpeedUpDelay)
                {
                    if (!_isSpeedingUpMoving)
                    {
                        _isSpeedingUpMoving = true;
                        TriggerAlertClientRpc("Vòng bo đang tăng tốc độ dịch chuyển!");
                    }
                    float secondsSinceSpeedUp = _moveTimer - moveSpeedUpDelay;
                    moveSpeedMin = baseMoveSpeed + moveAcceleration * secondsSinceSpeedUp;
                }

                float moveSpeedSec = moveSpeedMin / 60f;
                MoveAndBounce(moveSpeedSec);
            }
        }

        private void InitializeMoveDirection()
        {
            Vector2 centroid = GetPlayerCentroid();
            Vector2 toCentroid = centroid - currentCenter.Value;
            if (toCentroid.sqrMagnitude > 0.01f)
            {
                _moveDirection = toCentroid.normalized;
            }
            else
            {
                float randomAngle = Random.Range(0f, Mathf.PI * 2f);
                _moveDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)).normalized;
            }
        }

        private void MoveAndBounce(float speed)
        {
            Vector2 nextPosition = currentCenter.Value + _moveDirection * speed * Time.deltaTime;
            bool bounced = false;
            float radius = currentRadius.Value;

            // X boundary check
            if (nextPosition.x - radius <= mapMin.x)
            {
                nextPosition.x = mapMin.x + radius;
                _moveDirection.x = Mathf.Abs(_moveDirection.x); // reflect right
                bounced = true;
            }
            else if (nextPosition.x + radius >= mapMax.x)
            {
                nextPosition.x = mapMax.x - radius;
                _moveDirection.x = -Mathf.Abs(_moveDirection.x); // reflect left
                bounced = true;
            }

            // Y boundary check
            if (nextPosition.y - radius <= mapMin.y)
            {
                nextPosition.y = mapMin.y + radius;
                _moveDirection.y = Mathf.Abs(_moveDirection.y); // reflect up
                bounced = true;
            }
            else if (nextPosition.y + radius >= mapMax.y)
            {
                nextPosition.y = mapMax.y - radius;
                _moveDirection.y = -Mathf.Abs(_moveDirection.y); // reflect down
                bounced = true;
            }

            currentCenter.Value = nextPosition;

            // Add slight randomness to reflection angle
            if (bounced)
            {
                float angleChange = Random.Range(-15f, 15f) * Mathf.Deg2Rad;
                float currentAngle = Mathf.Atan2(_moveDirection.y, _moveDirection.x);
                currentAngle += angleChange;
                _moveDirection = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)).normalized;
            }
        }

        private Vector2 GetPlayerCentroid()
        {
            var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
            if (players.Length == 0) return Vector2.zero;

            Vector2 sum = Vector2.zero;
            int count = 0;
            foreach (var p in players)
            {
                if (p != null)
                {
                    sum += (Vector2)p.transform.position;
                    count++;
                }
            }
            return count > 0 ? sum / count : Vector2.zero;
        }

        private void DrawCircle(float radius)
        {
            if (lineRenderer == null) return;
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
            return distance <= currentRadius.Value;
        }

        [Rpc(SendTo.Everyone)]
        private void TriggerAlertClientRpc(string message)
        {
            // Raise UI warning event via EventBus (resolving from VContainer active scope)
            var scope = VContainer.Unity.LifetimeScope.Find<MonSumo.Networking.Scopes.GameLifetimeScope>();
            var eventBus = scope?.Container.Resolve<EventBus>();
            
            // Trigger local EventBus callbacks
            if (message.Contains("thu nhỏ"))
            {
                eventBus?.RaiseZoneShrinkStarted(currentRadius.Value);
            }
            else
            {
                eventBus?.RaiseZoneMoveStarted(currentCenter.Value);
            }

            Debug.Log($"[ZoneController Alert] {message}");
        }

#if UNITY_EDITOR
        public void DebugForceStartShrink()
        {
            if (!IsServer || !IsSpawned) return;
            if (!_isShrinkingStarted)
            {
                _isShrinkingStarted = true;
                TriggerAlertClientRpc("Vòng bo đang bắt đầu thu nhỏ lại!");
                _isMovingStarted = true;
                TriggerAlertClientRpc("Vòng bo đang bắt đầu dịch chuyển!");
                InitializeMoveDirection();
            }
        }

        public void DebugForceStartMoving()
        {
            if (!IsServer || !IsSpawned) return;
            if (!_isMovingStarted)
            {
                _isMovingStarted = true;
                TriggerAlertClientRpc("Vòng bo đang bắt đầu dịch chuyển!");
                InitializeMoveDirection();
            }
        }
#endif

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
    }
}