#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieGame.Core
{
    /// <summary>
    /// The one service locator of the game. Systems register themselves here during boot and are
    /// resolved by type, so nothing has to hunt for its collaborators through the scene graph.
    /// </summary>
    /// <remarks>
    /// This exists so gameplay code never needs <c>GameObject.Find</c> or <c>FindObjectOfType</c>.
    /// It is deliberately a small, boring container: register during boot, resolve during play,
    /// and never use it as a general-purpose global bag of state.
    /// </remarks>
    public static class GameServices
    {
        private static readonly Dictionary<Type, object> Registered = new();

        /// <summary>How many services are currently registered.</summary>
        public static int Count => Registered.Count;

        /// <summary>
        /// Registers <paramref name="service"/> under the type <typeparamref name="T"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">The service is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// A service of that type is already registered. Double registration is almost always a
        /// boot-order bug, so it fails loudly instead of silently replacing the first one.
        /// </exception>
        public static void Register<T>(T service) where T : class
        {
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (Registered.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException(
                    $"A service of type {typeof(T).Name} is already registered. " +
                    "Call Replace if overwriting is intended.");
            }

            Registered.Add(typeof(T), service);
        }

        /// <summary>
        /// Registers <paramref name="service"/>, overwriting any previous registration.
        /// Returns the service that was replaced, or null if there was none.
        /// </summary>
        public static T? Replace<T>(T service) where T : class
        {
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Registered.TryGetValue(typeof(T), out object? previous);
            Registered[typeof(T)] = service;
            return previous as T;
        }

        /// <summary>Resolves a service, or throws if it was never registered.</summary>
        /// <exception cref="InvalidOperationException">No service of that type is registered.</exception>
        public static T Get<T>() where T : class
        {
            if (!Registered.TryGetValue(typeof(T), out object? service))
            {
                throw new InvalidOperationException(
                    $"No service of type {typeof(T).Name} is registered. " +
                    "Services are registered during boot — is the Boot scene loaded?");
            }

            return (T)service;
        }

        /// <summary>
        /// Resolves a service without throwing. Returns false when it is not registered, and sets
        /// the service to null.
        /// </summary>
        /// <remarks>
        /// The result is nullable on purpose, so a component may pass its own nullable field
        /// straight in without having to lie about it with a null-forgiving operator.
        /// </remarks>
        public static bool TryGet<T>(out T? service) where T : class
        {
            if (Registered.TryGetValue(typeof(T), out object? found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>True when a service of that type is registered.</summary>
        public static bool IsRegistered<T>() where T : class => Registered.ContainsKey(typeof(T));

        /// <summary>Removes a service. Returns false when there was nothing to remove.</summary>
        public static bool Unregister<T>() where T : class => Registered.Remove(typeof(T));

        /// <summary>Drops every registration. Used when tearing down a session and by tests.</summary>
        public static void Reset() => Registered.Clear();

        /// <summary>
        /// Clears the locator before a play session starts. Static state survives when the user has
        /// domain reload disabled in Enter Play Mode Options, which would otherwise leak services
        /// from the previous session into the next one.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBeforePlay() => Reset();
    }
}
