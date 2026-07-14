using System.Collections;
using UnityEngine;

namespace MonSumo.World.Hazards
{
    public class TornamiController : MonoBehaviour
    {
        [Header("Timing Settings")]
        [SerializeField] private float _activeDuration = 3.0f;
        [SerializeField] private float _dieDuration = 0.2f;
        [SerializeField] private float _inactiveDuration = 2.0f;

        [Header("References")]
        [SerializeField] private GameObject _tornami;

        private Animator _childAnimator;

        private void Start()
        {
            if (_tornami == null)
            {
                // Try to find the child by name if not assigned
                var childTransform = transform.Find("Tornami");
                if (childTransform == null && transform.childCount > 0)
                {
                    childTransform = transform.GetChild(0);
                }
                if (childTransform != null)
                {
                    _tornami = childTransform.gameObject;
                }
            }

            if (_tornami != null)
            {
                _childAnimator = _tornami.GetComponent<Animator>();
            }

            StartCoroutine(CycleRoutine());
        }

        private IEnumerator CycleRoutine()
        {
            while (true)
            {
                // 1. Activate Child (Born & Looping)
                if (_tornami != null)
                {
                    _tornami.SetActive(true);
                }

                if (_childAnimator != null)
                {
                    _childAnimator.SetBool("active", true);
                }

                // Wait for the duration before Die state starts
                float bornAndLoopingDuration = Mathf.Max(0f, _activeDuration - _dieDuration);
                yield return new WaitForSeconds(bornAndLoopingDuration);

                // 2. Trigger Die State
                if (_childAnimator != null)
                {
                    _childAnimator.SetBool("active", false);
                }

                // Wait for Die animation to finish
                yield return new WaitForSeconds(_dieDuration);

                // 3. Deactivate Child for Inactive period
                if (_tornami != null)
                {
                    _tornami.SetActive(false);
                }

                yield return new WaitForSeconds(_inactiveDuration);
            }
        }
    }
}
