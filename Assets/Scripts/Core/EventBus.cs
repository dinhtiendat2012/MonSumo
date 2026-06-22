using System;
using UnityEngine;
using MonSumo.Core.Enums;

namespace MonSumo.Core
{
    public sealed class EventBus
    {
        // Lobby & Network events
        public event Action<ulong, string> OnPlayerLobbyJoined;
        public event Action<ulong> OnPlayerLobbyLeft;
        public event Action OnLobbySlotsChanged;

        // Gameplay state events
        public event Action OnMatchStarted;
        public event Action<ulong> OnMatchEnded; // Winner ClientId
        public event Action<ulong, GameObject> OnPlayerSpawned;
        public event Action<ulong> OnPlayerKnockedOut; // ClientId of player fell out

        // Safe zone / Storm events
        public event Action<float> OnZoneShrinkStarted; // Target radius
        public event Action<Vector2> OnZoneMoveStarted; // Target center position

        // Item events
        public event Action<ItemType, Vector2> OnItemSpawned;
        public event Action<ulong, ItemType> OnItemPickedUp;

        // Methods to raise events
        public void RaisePlayerLobbyJoined(ulong clientId, string name) => OnPlayerLobbyJoined?.Invoke(clientId, name);
        public void RaisePlayerLobbyLeft(ulong clientId) => OnPlayerLobbyLeft?.Invoke(clientId);
        public void RaiseLobbySlotsChanged() => OnLobbySlotsChanged?.Invoke();

        public void RaiseMatchStarted() => OnMatchStarted?.Invoke();
        public void RaiseMatchEnded(ulong winnerClientId) => OnMatchEnded?.Invoke(winnerClientId);
        public void RaisePlayerSpawned(ulong clientId, GameObject playerObj) => OnPlayerSpawned?.Invoke(clientId, playerObj);
        public void RaisePlayerKnockedOut(ulong clientId) => OnPlayerKnockedOut?.Invoke(clientId);

        public void RaiseZoneShrinkStarted(float targetRadius) => OnZoneShrinkStarted?.Invoke(targetRadius);
        public void RaiseZoneMoveStarted(Vector2 targetCenter) => OnZoneMoveStarted?.Invoke(targetCenter);

        public void RaiseItemSpawned(ItemType itemType, Vector2 position) => OnItemSpawned?.Invoke(itemType, position);
        public void RaiseItemPickedUp(ulong playerClientId, ItemType itemType) => OnItemPickedUp?.Invoke(playerClientId, itemType);
    }
}
