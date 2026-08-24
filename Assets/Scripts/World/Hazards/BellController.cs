using System;
using System.Collections;
using UnityEngine;

public class BellController : MonoBehaviour
{
    [SerializeField] private GameObject skillObject;
    [SerializeField] private float skillObjectTime = 0.5f;
    Animator animator;
    private void Awake()
    {
        animator = GetComponent<Animator>();

        // Ensure there is a Kinematic Rigidbody2D so that OnTriggerEnter2D fires reliably in Netcode (server-side)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (animator == null) Debug.LogError("[Bell] Animator is null");

        MonSumo.Core.Player player = collision.GetComponentInParent<MonSumo.Core.Player>();
        if (player != null)
        {
            Debug.Log($"[Bell] Detected player touching the bell: OwnerClientId={player.OwnerClientId}");
            animator.SetTrigger("Active");
            //Active bell skill
            StartCoroutine(BellSkill());
        }
    }

    IEnumerator BellSkill()
    {
        skillObject.SetActive(true);
        yield return new WaitForSeconds(skillObjectTime);
        skillObject.SetActive(false);
    }
}
