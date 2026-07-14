using System.Collections.Generic;
using UnityEngine;

namespace MonSumo.World.Hazards
{
    public class TornamiMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private List<Transform> _waypoints = new List<Transform>();
        [SerializeField] private float _moveSpeed = 5.0f;

        private Animator _animator;
        private int _currentWaypointIndex = 0;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            UpdateMovement();
        }

        private void UpdateMovement()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;

            Transform target = _waypoints[_currentWaypointIndex];
            if (target == null) return;

            // Move child object towards the target waypoint
            
            Vector3 pos = Vector3.MoveTowards(transform.position, target.position, _moveSpeed * Time.deltaTime);
            transform.position = pos;

            // If we reached the target, switch to the next one
            if (Vector3.Distance(transform.position, target.position) < 0.05f)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_waypoints == null || _waypoints.Count == 0) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < _waypoints.Count; i++)
            {
                if (_waypoints[i] == null) continue;
                Gizmos.DrawWireSphere(_waypoints[i].position, 0.3f);
                int nextIndex = (i + 1) % _waypoints.Count;
                if (_waypoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[nextIndex].position);
                }
            }
        }
    }
}
