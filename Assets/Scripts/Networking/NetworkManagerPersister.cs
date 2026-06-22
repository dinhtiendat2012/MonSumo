using UnityEngine;
using Unity.Netcode;

namespace MonSumo.Networking
{
    [DefaultExecutionOrder(-9999)]
    public sealed class NetworkManagerPersister : MonoBehaviour
    {
        private void Awake()
        {
            var net = GetComponent<NetworkManager>();
            if (net == null)
            {
                Destroy(gameObject);
                return;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton != net)
            {
                Debug.Log($"[NetworkManagerPersister] Duplicate NetworkManager found on {gameObject.name}. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Debug.Log($"[NetworkManagerPersister] Registering persistent NetworkManager on {gameObject.name}.");
            DontDestroyOnLoad(gameObject);
        }
    }
}
