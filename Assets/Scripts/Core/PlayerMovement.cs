using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Base Stats")]
    public float baseSpeed = 5f;
    public float baseForce = 5f;
    public float baseMass = 10f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float staminaRegen = 2f;

    [Header("Sprint")]
    private bool isSprinting;
    [SerializeField] public float sprintCostPerSecond = 10f;

    [Header("Dash")]
    [SerializeField] float dashCooldown = 3f;
    [SerializeField] float dashCost = 10f;
    float dashDuration;
    bool isDashing= false;

    [Header("Combat")]
    public float attackCooldown = 3f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private float currentSpeed;

    private float pushTimer;
    private float dashTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentStamina = maxStamina;
    }

    void Update()
    {
        HandleInput();
        HandleStamina();

        pushTimer -= Time.deltaTime;

        if (Input.GetMouseButtonDown(0))
        {
            Push();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            UseMonsterSkill();
        }

        dashTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Dash();
        }
    }

    private void Dash()
    {
        if (dashTimer > 0) return;
        if (currentStamina < dashCost) return;

        dashTimer = dashCooldown;
        currentStamina -= dashCost;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector2 dashDirection = (mousePos - transform.position).normalized;
        float dashDistance = 10f;
        float dashSpeed = baseSpeed * 20f;

        dashDuration = dashDistance / dashSpeed;
        rb.linearVelocity = dashDirection * dashSpeed;
        Debug.Log("Dash");
        isDashing = true;
        StartCoroutine(DashRoutine());
    }
    IEnumerator DashRoutine()
    {
        yield return new WaitForSeconds(dashDuration);
        Debug.Log("Dash Ended");
        isDashing = false;
    }

    void FixedUpdate()
    {
        if (!isDashing) 
            rb.linearVelocity = moveInput.normalized * currentSpeed;
    }

    private void UseMonsterSkill()
    {
        Debug.Log("Use Monster Skill");
    }

    private void Push()
    {   
        //chỉ có thể push khi cooldown về 0
        if (pushTimer > 0) return;
        pushTimer = attackCooldown;
        
        Debug.Log($"Attack Knockback: {KnockbackForce()}");
    }

    private void HandleStamina()
    {
        //Tăng 2 stamina mỗi giây
        currentStamina += staminaRegen * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

    }

    private void HandleInput()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (moveInput == Vector2.zero)
        {
            currentSpeed = 0;
            return;
        }
        //chưa cộng speed ++ của quái
        isSprinting = Input.GetKey(KeyCode.Space);
        if (isSprinting)
        {
            // Nếu còn stamina thì tăng speed và giảm stamina
            if (currentStamina >= sprintCostPerSecond)
            {
                Debug.Log("Chạy!!!!");
                // Tăng tốc độ di chuyển khi chạy
                currentSpeed = baseSpeed * 1.5f;
                currentStamina -= sprintCostPerSecond * Time.deltaTime;
            }
        }
        else
        {
            // Nếu không chạy thì trở về tốc độ cơ bản
            currentSpeed = baseSpeed;
        }
        
    }
    public float KnockbackForce()
    {
        return baseMass + baseForce + currentSpeed;
    }

}