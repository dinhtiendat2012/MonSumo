using System.Collections;
using UnityEngine;

public class LionFireController : MonoBehaviour
{
    [SerializeField] private LionFireSkill skillObject;
    [SerializeField] private float activeSkillTime = 0.58f;
    [SerializeField] private float deactiveSkillTime = 3f;
    
    private void Start()
    {
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
                skillObject.gameObject.SetActive(true);
            }
            yield return new WaitForSeconds(activeSkillTime);

            // 2. Deactivate Child for Inactive period
            if (skillObject != null)
            {
                skillObject.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(deactiveSkillTime);
        }
    }
}
