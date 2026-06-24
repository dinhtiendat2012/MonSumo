using UnityEngine;
using System.Collections;

public class FireRoundPoint : MonoBehaviour
{
    [SerializeField] private GameObject vfxObject;

    private Animator animator;

    private void Awake()
    {
        vfxObject.SetActive(false);
        animator = vfxObject.GetComponent<Animator>();
    }

    public void Cast()
    {
        vfxObject.SetActive(true);
        animator.Play("fireTanukiSkill", 0, 0f);

        StartCoroutine(DisableAfterAnimation());
    }

    private IEnumerator DisableAfterAnimation()
    {
        yield return null; // đợi Animator cập nhật state

        float length = animator.GetCurrentAnimatorStateInfo(0).length;

        yield return new WaitForSeconds(length);

        vfxObject.SetActive(false);
    }
}
