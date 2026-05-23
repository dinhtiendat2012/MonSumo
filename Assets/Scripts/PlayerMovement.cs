using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // WASD Movement
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // Normal Attack - Left Mouse Click
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }

        // Switch / Pick Item - E
        if (Input.GetKeyDown(KeyCode.E))
        {
            SwitchItem();
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput.normalized * moveSpeed;
    }

    void Attack()
    {
        Debug.Log("Normal Attack");
    }

    void SwitchItem()
    {
        Debug.Log("Switch Item");
    }
}