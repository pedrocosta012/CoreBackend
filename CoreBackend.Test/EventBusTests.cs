using CoreBackend.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreBackend.Test;

public sealed class EventBusTests
{
    private record SampleEvent(string Message) : IEvent;
    private record AnotherEvent(int Value) : IEvent;
    private record UnhandledEvent : IEvent;

    private sealed class SampleEventHandler : IEventHandler<SampleEvent>
    {
        public List<string> ReceivedMessages { get; } = [];

        public Task HandleAsync(SampleEvent @event, CancellationToken cancellationToken = default)
        {
            ReceivedMessages.Add(@event.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class SecondSampleEventHandler : IEventHandler<SampleEvent>
    {
        public int CallCount { get; private set; }

        public Task HandleAsync(SampleEvent @event, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class AnotherEventHandler : IEventHandler<AnotherEvent>
    {
        public List<int> ReceivedValues { get; } = [];

        public Task HandleAsync(AnotherEvent @event, CancellationToken cancellationToken = default)
        {
            ReceivedValues.Add(@event.Value);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHandler : IEventHandler<SampleEvent>
    {
        public Task HandleAsync(SampleEvent @event, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Handler failed");
        }
    }

    private static EventBus CreateBus(IServiceCollection services)
    {
        services.AddSingleton<ILogger<EventBus>>(NullLogger<EventBus>.Instance);
        var provider = services.BuildServiceProvider();
        return new EventBus(provider, provider.GetRequiredService<ILogger<EventBus>>());
    }

    [Fact]
    public async Task PublishAsync_DispatchesToRegisteredHandler()
    {
        var services = new ServiceCollection();
        var handler = new SampleEventHandler();
        services.AddSingleton<IEventHandler<SampleEvent>>(handler);

        var bus = CreateBus(services);

        await bus.PublishAsync(new SampleEvent("hello"));

        Assert.Single(handler.ReceivedMessages);
        Assert.Equal("hello", handler.ReceivedMessages[0]);
    }

    [Fact]
    public async Task PublishAsync_DispatchesToMultipleHandlers()
    {
        var services = new ServiceCollection();
        var first = new SampleEventHandler();
        var second = new SecondSampleEventHandler();
        services.AddSingleton<IEventHandler<SampleEvent>>(first);
        services.AddSingleton<IEventHandler<SampleEvent>>(second);

        var bus = CreateBus(services);

        await bus.PublishAsync(new SampleEvent("test"));

        Assert.Single(first.ReceivedMessages);
        Assert.Equal(1, second.CallCount);
    }

    [Fact]
    public async Task PublishAsync_DoesNothing_WhenNoHandlersRegistered()
    {
        var services = new ServiceCollection();
        var bus = CreateBus(services);

        var exception = await Record.ExceptionAsync(() => bus.PublishAsync(new UnhandledEvent()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_DifferentEventTypes_RoutedToCorrectHandlers()
    {
        var services = new ServiceCollection();
        var sampleHandler = new SampleEventHandler();
        var anotherHandler = new AnotherEventHandler();
        services.AddSingleton<IEventHandler<SampleEvent>>(sampleHandler);
        services.AddSingleton<IEventHandler<AnotherEvent>>(anotherHandler);

        var bus = CreateBus(services);

        await bus.PublishAsync(new SampleEvent("msg"));
        await bus.PublishAsync(new AnotherEvent(42));

        Assert.Single(sampleHandler.ReceivedMessages);
        Assert.Equal("msg", sampleHandler.ReceivedMessages[0]);
        Assert.Single(anotherHandler.ReceivedValues);
        Assert.Equal(42, anotherHandler.ReceivedValues[0]);
    }

    [Fact]
    public async Task PublishAsync_PropagatesHandlerException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<SampleEvent>>(new FailingHandler());

        var bus = CreateBus(services);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => bus.PublishAsync(new SampleEvent("fail")));

        Assert.Equal("Handler failed", ex.Message);
    }

    [Fact]
    public async Task PublishAsync_MultiplePublishes_AllDelivered()
    {
        var services = new ServiceCollection();
        var handler = new SampleEventHandler();
        services.AddSingleton<IEventHandler<SampleEvent>>(handler);

        var bus = CreateBus(services);

        await bus.PublishAsync(new SampleEvent("first"));
        await bus.PublishAsync(new SampleEvent("second"));
        await bus.PublishAsync(new SampleEvent("third"));

        Assert.Equal(3, handler.ReceivedMessages.Count);
        Assert.Equal(["first", "second", "third"], handler.ReceivedMessages);
    }
}
