using System.Collections;
using UnityEngine;

public class Soul : MonoBehaviour
{
    [Header("Weapon")]
    public Collider2D attackCollider;

    [Header("Attack")]
    public float attackDuration = 0.2f;
    public float attackCooldown = 0.5f;

    private bool isAttacking;

    void Start()
    {
        attackCollider.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            StartCoroutine(Attack());
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Không hit chính mình
        if (other.transform.root == transform.root)
            return;
        if (other.CompareTag("Player"))
        {
            Debug.Log("Hit Player");
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                Vector2 pushDirection =
                    (other.transform.position - transform.position).normalized;

                rb.AddForce(pushDirection * 10f, ForceMode2D.Impulse);
            }
        }

    }
    IEnumerator Attack()
    {
        isAttacking = true;

        // Bật hitbox
        attackCollider.enabled = true;

        Debug.Log("Soul Attack");

        yield return new WaitForSeconds(attackDuration);

        // Tắt hitbox
        attackCollider.enabled = false;

        // Cooldown
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }
}