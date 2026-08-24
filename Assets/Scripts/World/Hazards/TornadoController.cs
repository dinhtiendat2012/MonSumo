using System.Collections;
using UnityEngine;

public class TornadoController : MonoBehaviour
{
    [SerializeField] private GameObject skillObject;
    [SerializeField] private float activeSkillTime = 2f;
    [SerializeField] private float dieSkillTime = 0.5f;
    [SerializeField] private float deactiveSkillTime = 3f;

    Animator animatorChild;

    private void Start()
    {
        animatorChild = GetComponentInChildren<Animator>();
        StartCoroutine(CycleRoutine());
    }
    // Update is called once per frame
    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            // 1. Activate Child (Born & Looping)
            if (skillObject != null)
            {
                skillObject.SetActive(true);
                animatorChild.SetBool("IsActive", true);
            }
            yield return new WaitForSeconds(activeSkillTime-dieSkillTime);

            // 2. Activate Child (Die)
            if (skillObject != null)
            {
                animatorChild.SetBool("IsActive", false);
            }
            yield return new WaitForSeconds(dieSkillTime);
            // 3. Deactivate Child for Inactive period
            if (skillObject != null)
            {
                skillObject.SetActive(false);
            }

            yield return new WaitForSeconds(deactiveSkillTime);
        }
    }
}
