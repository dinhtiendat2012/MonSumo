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

    [Header("Rule")]
    [SerializeField] private float outsideGraceTime = 1.5f;

    private float outsideTimer;
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

        bool isInsideZone = zoneController.IsInsideZone(transform.position);

        if (isInsideZone)
        {
            outsideTimer = 0f;
            return;
        }

        outsideTimer += Time.deltaTime;

        if (outsideTimer >= outsideGraceTime)
        {
            outsideTimer = 0f; // Reset to prevent double triggering
            Player player = GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage();
            }
        }
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