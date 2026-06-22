using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonSumo.Networking.Lobby
{
    public sealed class LobbyController : NetworkBehaviour
    {
        [Tooltip("Name of the scene to load when host starts (must be in Build Settings).")]
        [SerializeField] private string _gameSceneName = "Match";

        private readonly NetworkList<LobbySlot> _slots = new(
            null,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly Dictionary<ulong, string> _pendingNames = new();

        public event Action SlotsChanged;

        public IReadOnlyList<LobbySlot> Slots
        {
            get
            {
                var list = new List<LobbySlot>(_slots.Count);
                foreach (LobbySlot slot in _slots)
                {
                    list.Add(slot);
                }
                return list;
            }
        }

        public override void OnNetworkSpawn()
        {
            _slots.OnListChanged += HandleListChanged;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

                // Host doesn't go through ConnectionApprovalCallback for itself,
                // so read from local ConnectionData set before StartHost().
                byte[] payload = NetworkManager.NetworkConfig.ConnectionData;
                if (payload != null && payload.Length > 0)
                {
                    _pendingNames[NetworkManager.LocalClientId] =
                        System.Text.Encoding.UTF8.GetString(payload);
                }

                AddSlot(NetworkManager.LocalClientId);
            }

            SlotsChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            _slots.OnListChanged -= HandleListChanged;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        public void RegisterPendingName(ulong clientId, string playerName)
        {
            _pendingNames[clientId] = playerName;
        }

        public void RequestStartGame()
        {
            if (!IsServer)
            {
                Debug.LogWarning("[LobbyController] Only host/server can start the game.");
                return;
            }

            if (string.IsNullOrEmpty(_gameSceneName))
            {
                Debug.LogWarning("[LobbyController] No game scene name defined.");
                return;
            }

            Debug.Log($"[LobbyController] Host starts match. Loading scene: {_gameSceneName}");

            // Register names of all clients in the Lobby to spawner
            foreach (var slot in _slots)
            {
                GamePlayerSpawner.RegisterPlayerName(slot.ClientId, slot.PlayerName.ToString());
            }

            // Arm spawner to spawn players on load completion
            GamePlayerSpawner.Arm(NetworkManager, _gameSceneName);

            NetworkManager.SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Single);
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.LocalClientId)
            {
                return;
            }
            AddSlot(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            _pendingNames.Remove(clientId);

            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                if (_slots[i].ClientId == clientId)
                {
                    _slots.RemoveAt(i);
                }
            }
        }

        private void AddSlot(ulong clientId)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].ClientId == clientId)
                {
                    return;
                }
            }

            string name = _pendingNames.TryGetValue(clientId, out string n) && !string.IsNullOrWhiteSpace(n)
                ? n
                : $"Player {clientId}";

            _slots.Add(new LobbySlot(clientId, Truncate(name)));
        }

        private static FixedString32Bytes Truncate(string value)
        {
            const int max = 24;
            if (value.Length > max)
            {
                value = value.Substring(0, max);
            }
            return new FixedString32Bytes(value);
        }

        private void HandleListChanged(NetworkListEvent<LobbySlot> changeEvent)
        {
            SlotsChanged?.Invoke();
        }
    }
}
