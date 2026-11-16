#nullable enable

namespace ZombieGame.Core.Events
{
    /// <summary>
    /// Marker for everything that travels on the <see cref="EventBus"/>.
    /// </summary>
    /// <remarks>
    /// Events are structs on purpose: publishing one must not allocate, because the bus is used
    /// from systems that tick hundreds of zombies.
    /// </remarks>
    public interface IEvent
    {
    }
}
