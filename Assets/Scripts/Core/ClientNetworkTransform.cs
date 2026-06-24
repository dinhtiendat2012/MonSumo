using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Allows the Client (Owner) to update their own Transform rather than only the Server.
/// Replace the default NetworkTransform component on Player prefabs with this.
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    /// <summary>
    /// Overrides server-authoritative status to allow owner client authority.
    /// </summary>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
