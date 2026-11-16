#nullable enable
using System;
using System.Collections.Generic;

namespace ZombieGame.Core.Events
{
    /// <summary>
    /// Cross-domain signal bus. Systems that must not know about each other — waves and audio, the
    /// colony and the UI — talk through this. Within a single domain, use direct references instead.
    /// </summary>
    /// <remarks>
    /// Subscribing and unsubscribing during a publish is safe: changes are queued and applied once
    /// the publish finishes, so a handler may unsubscribe itself without corrupting the iteration.
    /// </remarks>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, IHandlerList> _handlers = new();

        /// <summary>Registers a handler for events of type <typeparamref name="T"/>.</summary>
        /// <exception cref="ArgumentNullException">The handler is null.</exception>
        public void Subscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            GetOrCreate<T>().Add(handler);
        }

        /// <summary>
        /// Removes a previously registered handler. Returns false when it was not subscribed —
        /// unsubscribing twice is harmless.
        /// </summary>
        public bool Unsubscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return _handlers.TryGetValue(typeof(T), out IHandlerList? list)
                   && ((HandlerList<T>)list).Remove(handler);
        }

        /// <summary>
        /// Delivers <paramref name="message"/> to every subscriber, in subscription order.
        /// Publishing an event nobody listens to is free.
        /// </summary>
        public void Publish<T>(T message) where T : struct, IEvent
        {
            if (_handlers.TryGetValue(typeof(T), out IHandlerList? list))
            {
                ((HandlerList<T>)list).Publish(message);
            }
        }

        /// <summary>How many handlers listen for <typeparamref name="T"/>. Mostly for tests.</summary>
        public int SubscriberCount<T>() where T : struct, IEvent =>
            _handlers.TryGetValue(typeof(T), out IHandlerList? list) ? list.Count : 0;

        /// <summary>Drops every subscription on the bus.</summary>
        public void Clear()
        {
            foreach (IHandlerList list in _handlers.Values)
            {
                list.Clear();
            }

            _handlers.Clear();
        }

        private HandlerList<T> GetOrCreate<T>() where T : struct, IEvent
        {
            if (!_handlers.TryGetValue(typeof(T), out IHandlerList? list))
            {
                list = new HandlerList<T>();
                _handlers.Add(typeof(T), list);
            }

            return (HandlerList<T>)list;
        }

        private interface IHandlerList
        {
            int Count { get; }

            void Clear();
        }

        /// <summary>
        /// The subscribers of one event type. Kept as a closed generic so neither subscribing nor
        /// publishing has to box the event or the delegate.
        /// </summary>
        private sealed class HandlerList<T> : IHandlerList where T : struct, IEvent
        {
            private readonly List<Action<T>> _active = new();
            private readonly List<Action<T>> _pendingAdds = new();
            private readonly List<Action<T>> _pendingRemoves = new();
            private int _publishDepth;

            public int Count => _active.Count + _pendingAdds.Count - _pendingRemoves.Count;

            public void Add(Action<T> handler)
            {
                if (_publishDepth > 0)
                {
                    _pendingAdds.Add(handler);
                    return;
                }

                _active.Add(handler);
            }

            public bool Remove(Action<T> handler)
            {
                if (_publishDepth > 0)
                {
                    if (!_active.Contains(handler) || _pendingRemoves.Contains(handler))
                    {
                        return false;
                    }

                    _pendingRemoves.Add(handler);
                    return true;
                }

                return _active.Remove(handler);
            }

            public void Publish(T message)
            {
                _publishDepth++;
                try
                {
                    // Indexed loop over the live list: handlers added during this publish are
                    // queued, so the bound never shifts underneath us.
                    for (int i = 0; i < _active.Count; i++)
                    {
                        Action<T> handler = _active[i];
                        if (_pendingRemoves.Count > 0 && _pendingRemoves.Contains(handler))
                        {
                            continue;
                        }

                        handler(message);
                    }
                }
                finally
                {
                    _publishDepth--;
                    if (_publishDepth == 0)
                    {
                        ApplyPending();
                    }
                }
            }

            public void Clear()
            {
                _active.Clear();
                _pendingAdds.Clear();
                _pendingRemoves.Clear();
                _publishDepth = 0;
            }

            private void ApplyPending()
            {
                for (int i = 0; i < _pendingRemoves.Count; i++)
                {
                    _active.Remove(_pendingRemoves[i]);
                }

                for (int i = 0; i < _pendingAdds.Count; i++)
                {
                    _active.Add(_pendingAdds[i]);
                }

                _pendingRemoves.Clear();
                _pendingAdds.Clear();
            }
        }
    }
}
