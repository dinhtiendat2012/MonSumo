using UnityEngine;
using System.Collections;
using MonSumo.Core;

public class FireRoundPoint : MonoBehaviour
{
    [SerializeField] private GameObject vfxObject;

    private Animator animator;
    private Player _player;
    private FireRoundSkill _fireSkill;

    [SerializeField] private AudioClip castSFX;

    private void Awake()
    {
        if (vfxObject != null)
        {
            vfxObject.SetActive(false);
            animator = vfxObject.GetComponent<Animator>();
            _fireSkill = vfxObject.GetComponent<FireRoundSkill>();
        }
        _player = GetComponentInParent<Player>();
    }

    public void Cast()
    {
        if (vfxObject == null) return;

        // Play skill cast SFX
        if (castSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(castSFX);
        }

        if (_fireSkill != null && _player != null)
        {
            _fireSkill.Init(_player);
        }

        vfxObject.SetActive(true);
        if (animator != null)
        {
            animator.Play("fireTanukiSkill", 0, 0f);
        }

        StartCoroutine(DisableAfterAnimation());
    }

    private IEnumerator DisableAfterAnimation()
    {
        yield return null; // đợi Animator cập nhật state

        if (animator != null)
        {
            float length = animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        vfxObject.SetActive(false);
    }
}
