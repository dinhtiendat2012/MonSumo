using System.Collections.Generic;
using MonSumo.UI.Base;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace MonSumo.Networking.Lobby
{
    public sealed class LobbyView : BaseView<LobbyPresenter>
    {
        [Header("Inputs")]
        [SerializeField] private TMP_InputField _playerNameInput;
        [SerializeField] private TMP_InputField _joinIpInput;

        [Header("Buttons")]
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _disconnectButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text _roomIdText;
        [SerializeField] private TMP_Text _statusText;

        [Header("Slot List")]
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private LobbySlotItem _slotItemPrefab;

        [Header("Panels (Optional)")]
        [SerializeField] private GameObject _connectionPanel;
        [SerializeField] private GameObject _roomPanel;

        private readonly List<LobbySlotItem> _spawnedItems = new();

        protected override void OnPresenterSet()
        {
            _hostButton?.onClick.AddListener(OnHostClicked);
            _joinButton?.onClick.AddListener(OnJoinClicked);
            _startButton?.onClick.AddListener(OnStartClicked);
            _disconnectButton?.onClick.AddListener(OnDisconnectClicked);
        }

        private void OnDestroy()
        {
            _hostButton?.onClick.RemoveListener(OnHostClicked);
            _joinButton?.onClick.RemoveListener(OnJoinClicked);
            _startButton?.onClick.RemoveListener(OnStartClicked);
            _disconnectButton?.onClick.RemoveListener(OnDisconnectClicked);
            Presenter?.Dispose();
        }

        public override void Render()
        {
            if (Presenter == null) return;

            LobbyModel model = GetModel();
            if (model == null) return;

            if (_roomIdText != null)
            {
                _roomIdText.text = model.IsHost ? $"Room ID (IP): {model.LocalIp}" : $"My IP: {model.LocalIp}";
            }

            if (_statusText != null)
            {
                _statusText.text = model.Status;
            }

            bool inLobby = model.State == LobbyConnectionState.InLobby;
            bool disconnected = model.State == LobbyConnectionState.Disconnected;

            if (_hostButton != null) _hostButton.interactable = disconnected;
            if (_joinButton != null) _joinButton.interactable = disconnected;
            if (_playerNameInput != null) _playerNameInput.interactable = disconnected;
            if (_joinIpInput != null) _joinIpInput.interactable = disconnected;

            if (_startButton != null)
            {
                bool canStart = inLobby && model.IsHost && model.Slots.Count >= 1;
                _startButton.gameObject.SetActive(model.IsHost);
                _startButton.interactable = canStart;
            }

            if (_disconnectButton != null)
            {
                _disconnectButton.gameObject.SetActive(!disconnected);
            }

            if (_connectionPanel != null)
            {
                _connectionPanel.SetActive(disconnected);
            }

            if (_roomPanel != null)
            {
                _roomPanel.SetActive(!disconnected);
            }

            RenderSlots(model);
        }

        private void RenderSlots(LobbyModel model)
        {
            if (_slotContainer == null || _slotItemPrefab == null) return;

            IReadOnlyList<LobbySlot> slots = model.Slots;
            ulong hostId = NetworkManager.ServerClientId;

            while (_spawnedItems.Count < slots.Count)
            {
                LobbySlotItem item = Instantiate(_slotItemPrefab, _slotContainer);
                _spawnedItems.Add(item);
            }

            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (i < slots.Count)
                {
                    LobbySlot slot = slots[i];
                    _spawnedItems[i].gameObject.SetActive(true);
                    _spawnedItems[i].Setup(i, slot.PlayerName.ToString(), slot.ClientId == hostId);
                }
                else
                {
                    _spawnedItems[i].gameObject.SetActive(false);
                }
            }
        }

        private LobbyModel _modelCache;

        private LobbyModel GetModel()
        {
            return _modelCache ??= Presenter != null ? Presenter.ModelForView : null;
        }

        private void OnHostClicked()
        {
            Presenter?.OnHostClicked(GetPlayerName());
        }

        private void OnJoinClicked()
        {
            Presenter?.OnJoinClicked(_joinIpInput != null ? _joinIpInput.text : string.Empty, GetPlayerName());
        }

        private void OnStartClicked()
        {
            Presenter?.OnStartClicked();
        }

        private void OnDisconnectClicked()
        {
            Presenter?.OnDisconnectClicked();
        }

        private string GetPlayerName()
        {
            string name = _playerNameInput != null ? _playerNameInput.text : string.Empty;
            return string.IsNullOrWhiteSpace(name) ? "Player" : name;
        }
    }
}
