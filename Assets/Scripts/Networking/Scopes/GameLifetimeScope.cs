using UnityEngine;
using VContainer;
using VContainer.Unity;
using MonSumo.Core;

namespace MonSumo.Networking.Scopes
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Register EventBus as a Singleton to decouple systems
            builder.Register<EventBus>(Lifetime.Singleton);

            Debug.Log("[GameLifetimeScope] Registered EventBus.");
        }
    }
}
