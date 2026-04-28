using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreBackend.Infrastructure.Events;

public sealed class EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var handlers = serviceProvider.GetServices<IEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            var handlerType = handler.GetType().Name;
            logger.LogDebug("Dispatching {Event} to {Handler}", typeof(TEvent).Name, handlerType);

            await handler.HandleAsync(@event, cancellationToken);
        }
    }
}
