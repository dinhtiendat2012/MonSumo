using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace MonSumo.Networking.Lobby
{
    public sealed class LobbyConnectionService : MonoBehaviour
    {
        [SerializeField] private LobbyController _lobbyController;
        [SerializeField] private ushort _port = 7777;

        private NetworkManager _net;

        public ushort Port => _port;

        public event Action<string> StatusChanged;

        private void Awake()
        {
            _net = NetworkManager.Singleton;
            if (_net != null)
            {
                DontDestroyOnLoad(_net.gameObject);
            }
        }

        public bool StartHost(string playerName)
        {
            if (!EnsureReady())
            {
                return false;
            }

            _net.NetworkConfig.ConnectionApproval = true;
            _net.ConnectionApprovalCallback = ApprovalCheck;
            _net.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;

            var transport = _net.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData("0.0.0.0", _port, "0.0.0.0");
            }

            _net.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(playerName ?? string.Empty);

            bool ok = _net.StartHost();
            StatusChanged?.Invoke(ok ? "Hosting" : "Failed to start host");
            return ok;
        }

        public bool StartClient(string hostIp, string playerName)
        {
            if (!EnsureReady())
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(hostIp))
            {
                StatusChanged?.Invoke("Enter host IP (Room ID) first");
                return false;
            }

            _net.NetworkConfig.ConnectionApproval = true;
            _net.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;

            var transport = _net.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(hostIp.Trim(), _port);
            }

            _net.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(playerName ?? string.Empty);

            bool ok = _net.StartClient();
            StatusChanged?.Invoke(ok ? $"Connecting to {hostIp}..." : "Failed to start client");
            return ok;
        }

        public void Disconnect()
        {
            if (_net != null && _net.IsListening)
            {
                _net.Shutdown();
            }
            StatusChanged?.Invoke("Disconnected");
        }

        public string GetLocalIPv4()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                {
                    return endPoint.Address.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LobbyConnectionService] Could not resolve IP via UDP socket: {e.Message}");
            }

            try
            {
                foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LobbyConnectionService] Could not resolve IP via DNS lookup: {e.Message}");
            }

            return "127.0.0.1";
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            string playerName = request.Payload != null && request.Payload.Length > 0
                ? Encoding.UTF8.GetString(request.Payload)
                : null;

            if (_lobbyController != null)
            {
                _lobbyController.RegisterPendingName(request.ClientNetworkId, playerName);
            }

            response.CreatePlayerObject = false; // Spawn manually in MatchScene
            response.Approved = true;
            response.Pending = false;
        }

        private bool EnsureReady()
        {
            _net ??= NetworkManager.Singleton;
            if (_net == null)
            {
                StatusChanged?.Invoke("NetworkManager not found");
                return false;
            }

            if (_net.IsListening)
            {
                StatusChanged?.Invoke("Connection already active");
                return false;
            }

            return true;
        }
    }
}
