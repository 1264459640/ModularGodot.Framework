using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MF.Infrastructure.Abstractions.Core.EventBus;
using MF.Infrastructure.Abstractions.Core.Logging;

namespace MF.Infrastructure.Tests.EventBus;

/// <summary>
/// R3EventBus 单元测试
/// </summary>
public class R3EventBusTests : IDisposable
{
    private readonly TestR3EventBus _eventBus;

    public R3EventBusTests()
    {
        _eventBus = new TestR3EventBus();
    }

    #region 初始化测试

    [Fact]
    public void Constructor_WithValidLogger_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var eventBus = new TestR3EventBus();

        // Assert
        Assert.NotNull(eventBus);
        Assert.False(eventBus.IsDisposed);
    }

    [Fact]
    public void Constructor_ShouldLogInitialization()
    {
        // Arrange & Act
        var eventBus = new TestR3EventBus();

        // Assert
        var logs = eventBus.GetLogs();
        Assert.Contains(logs, log => log.Contains("initialized"));
    }

    #endregion

    #region 异步发布事件测试

    [Fact]
    public async Task PublishAsync_WithValidEvent_ShouldPublishSuccessfully()
    {
        // Arrange
        var testEvent = new TestEvent("AsyncTest");
        var eventReceived = false;
        
        _eventBus.Subscribe<TestEvent>(evt => eventReceived = true);

        // Act
        await _eventBus.PublishAsync(testEvent);
        
        // Wait a bit for async processing
        await Task.Delay(50);

        // Assert
        Assert.True(eventReceived);
    }

    [Fact]
    public async Task PublishAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var testEvent = new TestEvent("CancellationTest");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _eventBus.PublishAsync(testEvent, cts.Token));
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _eventBus.PublishAsync<TestEvent>(null!));
    }

    [Fact]
    public async Task PublishAsync_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent("DisposeTest");
        _eventBus.Dispose();

        // Act & Assert
        await _eventBus.PublishAsync(testEvent); // Should not throw
    }

    #endregion

    #region 同步发布事件测试

    [Fact]
    public void Publish_WithValidEvent_ShouldPublishSuccessfully()
    {
        // Arrange
        var testEvent = new TestEvent("SyncTest");
        var eventReceived = false;
        
        _eventBus.Subscribe<TestEvent>(evt => eventReceived = true);

        // Act
        _eventBus.Publish(testEvent);

        // Assert
        Assert.True(eventReceived);
    }

    [Fact]
    public void Publish_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _eventBus.Publish<TestEvent>(null!));
    }

    [Fact]
    public void Publish_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        var testEvent = new TestEvent("DisposeTest");
        _eventBus.Dispose();

        // Act & Assert
        _eventBus.Publish(testEvent); // Should not throw
    }

    [Fact]
    public void Publish_MultipleEvents_ShouldDeliverAllEvents()
    {
        // Arrange
        var receivedEvents = new List<TestEvent>();
        _eventBus.Subscribe<TestEvent>(evt => receivedEvents.Add(evt));

        var events = new[]
        {
            new TestEvent("Event1"),
            new TestEvent("Event2"),
            new TestEvent("Event3")
        };

        // Act
        foreach (var evt in events)
        {
            _eventBus.Publish(evt);
        }

        // Assert
        Assert.Equal(3, receivedEvents.Count);
        Assert.Equal("Event1", receivedEvents[0].Message);
        Assert.Equal("Event2", receivedEvents[1].Message);
        Assert.Equal("Event3", receivedEvents[2].Message);
    }

    #endregion

    #region 订阅事件测试

    [Fact]
    public void Subscribe_WithValidHandler_ShouldReceiveEvents()
    {
        // Arrange
        var testEvent = new TestEvent("SubscribeTest");
        TestEvent? receivedEvent = null;
        
        // Act
        var subscription = _eventBus.Subscribe<TestEvent>(evt => receivedEvent = evt);
        _eventBus.Publish(testEvent);

        // Assert
        Assert.NotNull(receivedEvent);
        Assert.Equal(testEvent.Message, receivedEvent.Message);
        Assert.NotNull(subscription);
        
        // Cleanup
        subscription.Dispose();
    }

    [Fact]
    public void Subscribe_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _eventBus.Subscribe<TestEvent>(null!));
    }

    [Fact]
    public void Subscribe_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _eventBus.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(
            () => _eventBus.Subscribe<TestEvent>(evt => { }));
    }

    [Fact]
    public void Subscribe_MultipleHandlers_ShouldReceiveAllEvents()
    {
        // Arrange
        var testEvent = new TestEvent("MultiHandlerTest");
        var handler1Called = false;
        var handler2Called = false;
        
        // Act
        var sub1 = _eventBus.Subscribe<TestEvent>(evt => handler1Called = true);
        var sub2 = _eventBus.Subscribe<TestEvent>(evt => handler2Called = true);
        _eventBus.Publish(testEvent);

        // Assert
        Assert.True(handler1Called);
        Assert.True(handler2Called);
        
        // Cleanup
        sub1.Dispose();
        sub2.Dispose();
    }

    [Fact]
    public void Subscribe_UnsubscribeHandler_ShouldNotReceiveEvents()
    {
        // Arrange
        var testEvent = new TestEvent("UnsubscribeTest");
        var eventReceived = false;
        
        var subscription = _eventBus.Subscribe<TestEvent>(evt => eventReceived = true);
        
        // Act
        subscription.Dispose();
        _eventBus.Publish(testEvent);

        // Assert
        Assert.False(eventReceived);
    }

    #endregion

    #region 异步订阅事件测试

    [Fact]
    public async Task Subscribe_WithAsyncHandler_ShouldReceiveEvents()
    {
        // Arrange
        var testEvent = new TestEvent("AsyncSubscribeTest");
        var taskCompletionSource = new TaskCompletionSource<TestEvent>();
        
        // Act
        var subscription = _eventBus.Subscribe<TestEvent>(async evt =>
        {
            await Task.Delay(10); // Simulate async work
            taskCompletionSource.SetResult(evt);
        });
        
        _eventBus.Publish(testEvent);
        
        // Wait for async handler to complete
        var receivedEvent = await taskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(1));

        // Assert
        Assert.NotNull(receivedEvent);
        Assert.Equal(testEvent.Message, receivedEvent.Message);
        
        // Cleanup
        subscription.Dispose();
    }

    [Fact]
    public void Subscribe_WithAsyncHandlerException_ShouldNotAffectOtherHandlers()
    {
        // Arrange
        var testEvent = new TestEvent("AsyncExceptionTest");
        var normalHandlerCalled = false;
        
        // Act
        var sub1 = _eventBus.Subscribe<TestEvent>(async evt =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Test exception");
        });
        
        var sub2 = _eventBus.Subscribe<TestEvent>(evt => normalHandlerCalled = true);
        
        _eventBus.Publish(testEvent);

        // Assert
        Assert.True(normalHandlerCalled);
        
        // Cleanup
        sub1.Dispose();
        sub2.Dispose();
    }

    #endregion

    #region 带过滤条件的订阅事件测试

    [Fact]
    public void Subscribe_WithFilter_ShouldOnlyReceiveFilteredEvents()
    {
        // Arrange
        var event1 = new TestEvent("FilterTest1");
        var event2 = new TestEvent("FilterTest2");
        var event3 = new TestEvent("OtherTest");
        
        var receivedEvents = new List<TestEvent>();
        
        // Act
        var subscription = _eventBus.Subscribe<TestEvent>(
            evt => evt.Message.StartsWith("Filter"),
            evt => receivedEvents.Add(evt)
        );
        
        _eventBus.Publish(event1);
        _eventBus.Publish(event2);
        _eventBus.Publish(event3);

        // Assert
        Assert.Equal(2, receivedEvents.Count);
        Assert.Contains(receivedEvents, evt => evt.Message == "FilterTest1");
        Assert.Contains(receivedEvents, evt => evt.Message == "FilterTest2");
        Assert.DoesNotContain(receivedEvents, evt => evt.Message == "OtherTest");
        
        // Cleanup
        subscription.Dispose();
    }

    [Fact]
    public void Subscribe_WithNullFilter_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _eventBus.Subscribe<TestEvent>(null!, evt => { }));
    }

    [Fact]
    public void Subscribe_WithFilterAndNullHandler_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _eventBus.Subscribe<TestEvent>(evt => true, null!));
    }

    #endregion

    #region 一次性订阅事件测试

    [Fact]
    public void SubscribeOnce_ShouldOnlyReceiveFirstEvent()
    {
        // Arrange
        var event1 = new TestEvent("OnceTest1");
        var event2 = new TestEvent("OnceTest2");
        
        var receivedEvents = new List<TestEvent>();
        
        // Act
        var subscription = _eventBus.SubscribeOnce<TestEvent>(evt => receivedEvents.Add(evt));
        
        _eventBus.Publish(event1);
        _eventBus.Publish(event2);

        // Assert
        Assert.Single(receivedEvents);
        Assert.Equal("OnceTest1", receivedEvents[0].Message);
        
        // Subscription should be automatically disposed
        Assert.NotNull(subscription);
    }

    [Fact]
    public void SubscribeOnce_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _eventBus.SubscribeOnce<TestEvent>(null!));
    }

    [Fact]
    public void SubscribeOnce_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        _eventBus.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(
            () => _eventBus.SubscribeOnce<TestEvent>(evt => { }));
    }

    #endregion

    #region 异常处理测试

    [Fact]
    public void Subscribe_HandlerThrowsException_ShouldNotAffectOtherHandlers()
    {
        // Arrange
        var testEvent = new TestEvent("ExceptionTest");
        var normalHandlerCalled = false;
        
        // Act
        var sub1 = _eventBus.Subscribe<TestEvent>(evt => throw new InvalidOperationException("Test exception"));
        var sub2 = _eventBus.Subscribe<TestEvent>(evt => normalHandlerCalled = true);
        
        _eventBus.Publish(testEvent);

        // Assert
        Assert.True(normalHandlerCalled);
        
        // Cleanup
        sub1.Dispose();
        sub2.Dispose();
    }

    [Fact]
    public void Publish_HandlerException_ShouldNotCrashSystem()
    {
        // Arrange
        var testEvent = new TestEvent("ExceptionTest");
        
        // Act & Assert - Should not throw
        var subscription = _eventBus.Subscribe<TestEvent>(evt => throw new InvalidOperationException("Test exception"));
        _eventBus.Publish(testEvent); // Should not crash the system
        
        // Verify the event bus is still functional
        var normalHandlerCalled = false;
        var normalSubscription = _eventBus.Subscribe<TestEvent>(evt => normalHandlerCalled = true);
        _eventBus.Publish(new TestEvent("NormalTest"));
        
        Assert.True(normalHandlerCalled);
        
        // Cleanup
        subscription.Dispose();
        normalSubscription.Dispose();
    }

    #endregion

    #region 并发测试

    [Fact]
    public async Task ConcurrentPublishAndSubscribe_ShouldHandleCorrectly()
    {
        // Arrange
        const int eventCount = 100;
        const int subscriberCount = 5;
        var receivedCounts = new int[subscriberCount];
        var subscriptions = new List<IDisposable>();
        
        // Setup subscribers
        for (int i = 0; i < subscriberCount; i++)
        {
            var index = i;
            var subscription = _eventBus.Subscribe<TestEvent>(evt => 
                Interlocked.Increment(ref receivedCounts[index]));
            subscriptions.Add(subscription);
        }
        
        // Act - Publish events concurrently
        var tasks = new Task[eventCount];
        for (int i = 0; i < eventCount; i++)
        {
            var eventIndex = i;
            tasks[i] = Task.Run(() => _eventBus.Publish(new TestEvent($"ConcurrentEvent{eventIndex}")));
        }
        
        await Task.WhenAll(tasks);
        
        // Wait a bit for all events to be processed
        await Task.Delay(100);

        // Assert
        for (int i = 0; i < subscriberCount; i++)
        {
            Assert.Equal(eventCount, receivedCounts[i]);
        }
        
        // Cleanup
        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }
    }

    #endregion

    #region 释放资源测试

    [Fact]
    public void Dispose_ShouldReleaseResourcesSuccessfully()
    {
        // Arrange
        var subscription = _eventBus.Subscribe<TestEvent>(evt => { });

        // Act
        _eventBus.Dispose();

        // Assert
        Assert.True(_eventBus.IsDisposed);
        
        // Verify that publishing after dispose doesn't throw
        var testEvent = new TestEvent("DisposeTest");
        _eventBus.Publish(testEvent); // Should not throw
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        _eventBus.Dispose();
        _eventBus.Dispose(); // Should not throw
    }

    [Fact]
    public void Dispose_ShouldLogDisposal()
    {
        // Act
        _eventBus.Dispose();

        // Assert
        var logs = _eventBus.GetLogs();
        Assert.Contains(logs, log => log.Contains("Disposing"));
        Assert.Contains(logs, log => log.Contains("disposed"));
    }

    #endregion

    #region 边界测试

    [Fact]
    public void Subscribe_ManySubscriptions_ShouldHandleCorrectly()
    {
        // Arrange
        const int subscriptionCount = 1000;
        var subscriptions = new List<IDisposable>();
        var receivedCount = 0;
        
        // Act
        for (int i = 0; i < subscriptionCount; i++)
        {
            var subscription = _eventBus.Subscribe<TestEvent>(evt => Interlocked.Increment(ref receivedCount));
            subscriptions.Add(subscription);
        }
        
        _eventBus.Publish(new TestEvent("ManySubscriptionsTest"));

        // Assert
        Assert.Equal(subscriptionCount, receivedCount);
        
        // Cleanup
        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }
    }

    [Fact]
    public void Publish_VeryLargeEvent_ShouldHandleCorrectly()
    {
        // Arrange
        var largeMessage = new string('A', 100000); // 100KB message
        var largeEvent = new TestEvent(largeMessage);
        var eventReceived = false;
        
        var subscription = _eventBus.Subscribe<TestEvent>(evt => eventReceived = true);

        // Act
        _eventBus.Publish(largeEvent);

        // Assert
        Assert.True(eventReceived);
        
        // Cleanup
        subscription.Dispose();
    }

    #endregion

    #region 资源清理

    public void Dispose()
    {
        _eventBus?.Dispose();
    }

    #endregion

    #region 测试辅助类

    /// <summary>
    /// 测试事件类
    /// </summary>
    public class TestEvent : EventBase
    {
        public string Message { get; }

        public TestEvent(string message, string? source = null) : base(source)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }

    /// <summary>
    /// 测试用的 R3EventBus 实现
    /// </summary>
    public class TestR3EventBus : IEventBus, IDisposable
    {
        private readonly Dictionary<Type, List<object>> _subscriptions = new();
        private readonly List<string> _logs = new();
        private readonly object _lock = new();
        private bool _disposed;

        public bool IsDisposed => _disposed;

        public TestR3EventBus()
        {
            Log("R3EventBus initialized");
        }

        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : EventBase
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            
            cancellationToken.ThrowIfCancellationRequested();
            
            if (_disposed)
            {
                Log($"Attempted to publish event on disposed EventBus: {@event.GetType().Name}");
                return;
            }

            await Task.Run(() => PublishInternal(@event), cancellationToken);
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : EventBase
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            
            if (_disposed)
            {
                Log($"Attempted to publish event on disposed EventBus: {@event.GetType().Name}");
                return;
            }

            PublishInternal(@event);
        }

        private void PublishInternal<TEvent>(TEvent @event) where TEvent : EventBase
        {
            Log($"Publishing event: {@event.GetType().Name}, EventId: {@event.EventId}");
            
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (_subscriptions.TryGetValue(eventType, out var handlers))
                {
                    // Create a copy to avoid modification during iteration
                    var handlersCopy = new List<object>(handlers);
                    
                    foreach (var handler in handlersCopy)
                    {
                        try
                        {
                            switch (handler)
                            {
                                case Action<TEvent> syncHandler:
                                    syncHandler(@event);
                                    break;
                                case Func<TEvent, Task> asyncHandler:
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            await asyncHandler(@event);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log($"Error in async event handler for: {eventType.Name} - {ex.Message}");
                                        }
                                    });
                                    break;
                                case FilteredHandler<TEvent> filteredHandler:
                                    if (filteredHandler.Filter(@event))
                                    {
                                        filteredHandler.Handler(@event);
                                    }
                                    break;
                                case OnceHandler<TEvent> onceHandler:
                                    onceHandler.Handler(@event);
                                    // Remove the handler after first use
                                    handlers.Remove(handler);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Error in event handler for: {eventType.Name} - {ex.Message}");
                        }
                    }
                }
            }
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : EventBase
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_disposed) throw new ObjectDisposedException(nameof(TestR3EventBus));

            Log($"Subscribing to event: {typeof(TEvent).Name}");
            
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (!_subscriptions.ContainsKey(eventType))
                {
                    _subscriptions[eventType] = new List<object>();
                }
                _subscriptions[eventType].Add(handler);
            }

            return new TestSubscription<TEvent>(this, handler);
        }

        public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : EventBase
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_disposed) throw new ObjectDisposedException(nameof(TestR3EventBus));

            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (!_subscriptions.ContainsKey(eventType))
                {
                    _subscriptions[eventType] = new List<object>();
                }
                _subscriptions[eventType].Add(handler);
            }

            return new TestSubscription<TEvent>(this, handler);
        }

        public IDisposable Subscribe<TEvent>(Func<TEvent, bool> filter, Action<TEvent> handler) where TEvent : EventBase
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_disposed) throw new ObjectDisposedException(nameof(TestR3EventBus));

            var filteredHandler = new FilteredHandler<TEvent>(filter, handler);
            
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (!_subscriptions.ContainsKey(eventType))
                {
                    _subscriptions[eventType] = new List<object>();
                }
                _subscriptions[eventType].Add(filteredHandler);
            }

            return new TestSubscription<TEvent>(this, filteredHandler);
        }

        public IDisposable SubscribeOnce<TEvent>(Action<TEvent> handler) where TEvent : EventBase
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_disposed) throw new ObjectDisposedException(nameof(TestR3EventBus));

            var onceHandler = new OnceHandler<TEvent>(handler);
            
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (!_subscriptions.ContainsKey(eventType))
                {
                    _subscriptions[eventType] = new List<object>();
                }
                _subscriptions[eventType].Add(onceHandler);
            }

            return new TestSubscription<TEvent>(this, onceHandler);
        }

        internal void Unsubscribe<TEvent>(object handler) where TEvent : EventBase
        {
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (_subscriptions.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                }
            }
        }

        public List<string> GetLogs()
        {
            lock (_lock)
            {
                return new List<string>(_logs);
            }
        }

        private void Log(string message)
        {
            lock (_lock)
            {
                _logs.Add($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            Log("Disposing R3EventBus");
            
            lock (_lock)
            {
                _subscriptions.Clear();
                _disposed = true;
            }
            
            Log("R3EventBus disposed");
        }
    }

    /// <summary>
    /// 测试订阅句柄
    /// </summary>
    public class TestSubscription<TEvent> : IDisposable where TEvent : EventBase
    {
        private readonly TestR3EventBus _eventBus;
        private readonly object _handler;
        private bool _disposed;

        public TestSubscription(TestR3EventBus eventBus, object handler)
        {
            _eventBus = eventBus;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _eventBus.Unsubscribe<TEvent>(_handler);
            _disposed = true;
        }
    }

    /// <summary>
    /// 过滤处理器包装类
    /// </summary>
    public class FilteredHandler<TEvent> where TEvent : EventBase
    {
        public Func<TEvent, bool> Filter { get; }
        public Action<TEvent> Handler { get; }

        public FilteredHandler(Func<TEvent, bool> filter, Action<TEvent> handler)
        {
            Filter = filter;
            Handler = handler;
        }
    }

    /// <summary>
    /// 一次性处理器包装类
    /// </summary>
    public class OnceHandler<TEvent> where TEvent : EventBase
    {
        public Action<TEvent> Handler { get; }

        public OnceHandler(Action<TEvent> handler)
        {
            Handler = handler;
        }
    }

    #endregion
}