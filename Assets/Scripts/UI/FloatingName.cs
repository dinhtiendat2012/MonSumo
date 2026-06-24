using UnityEngine;
using TMPro;
using Unity.Netcode;
using MonSumo.Core;

namespace MonSumo.UI
{
    public class FloatingName : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Player _player;

        private void Start()
        {
            if (_player == null)
            {
                _player = GetComponentInParent<Player>();
            }

            if (_player != null)
            {
                // Subscribe to name changes
                _player.playerName.OnValueChanged += OnNameChanged;
                
                // Initialize name
                UpdateName(_player.playerName.Value.ToString());
            }
            else
            {
                Debug.LogWarning("[FloatingName] Player component not found on parents.");
            }
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.playerName.OnValueChanged -= OnNameChanged;
            }
        }

        private void OnNameChanged(Unity.Collections.FixedString32Bytes previousValue, Unity.Collections.FixedString32Bytes newValue)
        {
            UpdateName(newValue.ToString());
        }

        private void UpdateName(string nameStr)
        {
            if (_nameText != null)
            {
                _nameText.text = nameStr;
            }
        }
    }
}
