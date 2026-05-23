using System.Collections;
using UnityEngine;

public class Knight : MonoBehaviour
{
    [Header("Weapon")]
    public Transform weaponPivot;
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
        RotateWeapon();

        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            StartCoroutine(Attack());
        }
    }

    void RotateWeapon()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector2 direction = mousePos - weaponPivot.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
    }

    IEnumerator Attack()
    {
        isAttacking = true;

        attackCollider.enabled = true;

        Debug.Log("Knight Attack");

        yield return new WaitForSeconds(attackDuration);

        attackCollider.enabled = false;

        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }
}