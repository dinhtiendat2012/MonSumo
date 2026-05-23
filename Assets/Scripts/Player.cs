using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [Header("HP System")]
    [SerializeField]
    public int maxHP = 3;

    private int currentHP;

    [Header("Respawn")]
    public Transform respawnPoint;

    private Rigidbody2D rb;

    void Start()
    {
        currentHP = maxHP;
        rb = GetComponent<Rigidbody2D>();
    }

    // Gọi khi player chết / rơi map / dính hazard
    public void TakeDamage()
    {
        currentHP--;

        Debug.Log("Current HP: " + currentHP);

        if (currentHP <= 0)
        {
            GameOver();
        }
        else
        {
            Respawn();
        }
    }

    void Respawn()
    {
        transform.position = respawnPoint.position;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log("Respawn!");
    }

    void GameOver()
    {
        Debug.Log("GAME OVER");

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // TEST
    void Update()
    {
        // Nhấn K để test mất máu
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage();
        }
    }
}