using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Simple Debug UI allowing easy triggers for StartHost / StartClient in editor.
/// Place this script on any GameObject in the scene for testing.
/// Remove before building final release.
/// </summary>
public sealed class NetworkDebugUI : MonoBehaviour
{
    [SerializeField] private string hostAddress = "127.0.0.1";

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsListening)
        {
            GUILayout.Label($"[Network] Listening — IsHost={NetworkManager.Singleton.IsHost} | Clients={NetworkManager.Singleton.ConnectedClientsIds.Count}");
            if (GUILayout.Button("Disconnect")) NetworkManager.Singleton.Shutdown();
            return;
        }

        GUILayout.Label("=== Network Debug ===");
        if (GUILayout.Button("Start HOST"))
        {
            NetworkManager.Singleton.StartHost();
        }

        if (GUILayout.Button("Start CLIENT"))
        {
            var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(hostAddress, transport.ConnectionData.Port);
            }
            NetworkManager.Singleton.StartClient();
        }

        if (GUILayout.Button("Start SERVER (headless)"))
        {
            NetworkManager.Singleton.StartServer();
        }
    }
}
