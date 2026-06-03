using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneOut : MonoBehaviour
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
    private bool isGameOver;

    private Rigidbody2D rb;
    private Collider2D col;

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
            GameOver();
        }
    }

    private void GameOver()
    {
        isGameOver = true;

        Debug.Log("GAME OVER: Player left the zone.");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        // Dừng game để player/bot/bo không chạy tiếp
        Time.timeScale = 0f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void GoHome()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(menuSceneName);
    }
}