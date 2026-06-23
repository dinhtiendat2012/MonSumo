using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using MonSumo.World.Zone;
using MonSumo.Core;

public class ZoneOut : NetworkBehaviour
{
    [Header("Zone")]
    [SerializeField] private ZoneController zoneController;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Scene Names")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Damage Cooldown")]
    [SerializeField] private float damageCooldown = 0.5f;

    private float _cooldownTimer;
    private bool isGameOver = false;

    private Rigidbody2D rb;
    private Collider2D col;

    public GameObject GameOverPanel => gameOverPanel;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (zoneController == null)
        {
            zoneController = FindFirstObjectByType<ZoneController>();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void Update()
    {
        // Only run zone checks on the Server
        if (!IsServer) return;
        if (isGameOver) return;
        if (zoneController == null) return;

        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            return;
        }

        bool isOutside = IsFullyOutsideZone();

        if (isOutside)
        {
            Player player = GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage();
                _cooldownTimer = damageCooldown; // Cooldown to avoid double-triggering before respawn syncs
            }
        }
    }

    private bool IsFullyOutsideZone()
    {
        if (col == null)
        {
            // Fallback to center point if no collider
            return !zoneController.IsInsideZone(transform.position);
        }

        // Get the closest point on the player's collider to the zone center
        Vector2 zoneCenter = zoneController.Center;
        Vector2 closestPoint = col.ClosestPoint(zoneCenter);

        // Calculate distance from closest point on player to zone center.
        // If the closest point's distance is greater than the radius, the entire collider is outside!
        float distance = Vector2.Distance(closestPoint, zoneCenter);
        return distance > zoneController.CurrentRadius;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene(menuSceneName);
    }
}