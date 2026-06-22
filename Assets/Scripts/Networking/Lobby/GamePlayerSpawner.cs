using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using MonSumo.Core;

namespace MonSumo.Networking.Lobby
{
    public sealed class GamePlayerSpawner : MonoBehaviour
    {
        private NetworkManager _net;
        private string _gameSceneName;
        private bool _armed;

        private static readonly Dictionary<ulong, string> _playerNames = new();

        private static readonly Vector3[] DefaultSpawnOffsets = new Vector3[]
        {
            new Vector3(6f, 0f, 0f),   // East
            new Vector3(-6f, 0f, 0f),  // West
            new Vector3(0f, -6f, 0f),  // South
            new Vector3(0f, 6f, 0f)    // North
        };

        public static void RegisterPlayerName(ulong clientId, string name)
        {
            _playerNames[clientId] = name;
        }

        public static void Arm(NetworkManager net, string gameSceneName)
        {
            if (net == null) return;

            GamePlayerSpawner spawner = net.GetComponent<GamePlayerSpawner>();
            if (spawner == null)
            {
                spawner = net.gameObject.AddComponent<GamePlayerSpawner>();
            }

            spawner._net = net;
            spawner._gameSceneName = gameSceneName;

            if (spawner._armed)
            {
                net.SceneManager.OnLoadEventCompleted -= spawner.HandleLoadEventCompleted;
            }

            net.SceneManager.OnLoadEventCompleted += spawner.HandleLoadEventCompleted;
            spawner._armed = true;
        }

        private void OnDestroy()
        {
            if (_armed && _net != null && _net.SceneManager != null)
            {
                _net.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
            }
        }

        private void HandleLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (_net == null || !_net.IsServer || sceneName != _gameSceneName)
            {
                return;
            }

            SpawnAllPlayers();
        }

        private void SpawnAllPlayers()
        {
            var clientsList = new List<ulong>(_net.ConnectedClientsIds);
            int clientCount = clientsList.Count;

            var spawnIndices = new List<int> { 0, 1, 2, 3 };
            
            var rng = new System.Random();
            for (int i = spawnIndices.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                int temp = spawnIndices[i];
                spawnIndices[i] = spawnIndices[k];
                spawnIndices[k] = temp;
            }

            Transform spawnEast = GameObject.Find("SpawnPoint_East")?.transform;
            Transform spawnWest = GameObject.Find("SpawnPoint_West")?.transform;
            Transform spawnSouth = GameObject.Find("SpawnPoint_South")?.transform;
            Transform spawnNorth = GameObject.Find("SpawnPoint_North")?.transform;

            Transform[] inSceneSpawnPoints = new Transform[] { spawnEast, spawnWest, spawnSouth, spawnNorth };

            for (int i = 0; i < clientCount; i++)
            {
                ulong clientId = clientsList[i];
                
                if (_net.ConnectedClients.TryGetValue(clientId, out NetworkClient client)
                    && client.PlayerObject != null)
                {
                    continue;
                }

                int directionIndex = spawnIndices[i % spawnIndices.Count];
                Vector3 spawnPosition;

                if (inSceneSpawnPoints[directionIndex] != null)
                {
                    spawnPosition = inSceneSpawnPoints[directionIndex].position;
                }
                else
                {
                    spawnPosition = DefaultSpawnOffsets[directionIndex];
                }

                SpawnPlayerFor(clientId, spawnPosition);
            }
        }

        private void SpawnPlayerFor(ulong clientId, Vector3 position)
        {
            GameObject playerPrefab = _net.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogWarning("[GamePlayerSpawner] PlayerPrefab is null in NetworkConfig.");
                return;
            }

            NetworkObject prefabNo = playerPrefab.GetComponent<NetworkObject>();
            if (prefabNo == null)
            {
                Debug.LogWarning("[GamePlayerSpawner] PlayerPrefab lacks NetworkObject component.");
                return;
            }

            NetworkObject spawnedNo = _net.SpawnManager.InstantiateAndSpawn(
                prefabNo,
                ownerClientId: clientId,
                destroyWithScene: true,
                isPlayerObject: true,
                position: position,
                rotation: Quaternion.identity);

            // Assign name from Lobby register
            var player = spawnedNo.GetComponent<Player>();
            if (player != null)
            {
                string name = _playerNames.TryGetValue(clientId, out string n) ? n : $"Player {clientId}";
                player.playerName.Value = new Unity.Collections.FixedString32Bytes(name);
            }

            Debug.Log($"[GamePlayerSpawner] Spawned player '{player?.playerName.Value}' for client {clientId} at {position}.");
        }
    }
}
