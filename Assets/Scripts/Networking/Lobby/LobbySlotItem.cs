using TMPro;
using UnityEngine;

namespace MonSumo.Networking.Lobby
{
    public sealed class LobbySlotItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _indexText;
        [SerializeField] private TMP_Text _nameText;

        public void Setup(int index, string playerName, bool isHost)
        {
            if (_indexText != null)
            {
                _indexText.text = $"#{index + 1}";
            }

            if (_nameText != null)
            {
                _nameText.text = isHost ? $"{playerName} (Host)" : playerName;
            }
        }
    }
}
