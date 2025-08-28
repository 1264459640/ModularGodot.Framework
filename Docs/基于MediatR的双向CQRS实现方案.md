# 基于MediatR的双向CQRS实现方案

## 架构概述

基于你的建议，我们将使用MediatR作为命令处理的核心，结合事件总线实现完整的双向CQRS架构。这种方案充分利用了MediatR的成熟生态，同时保持了我们讨论的双向通信优势。

## 架构流程

```
节点层 → MediatR命令 → 命令处理器 → 调用服务层
                                        ↓
节点层 ← 选择性订阅 ← 事件总线 ← 发布事件 ← 服务层执行完成
```

## 核心设计

### 1. MediatR命令接口扩展

```csharp
// 扩展MediatR的IRequest接口，添加我们需要的属性
public interface IGameCommand : IRequest
{
    string CommandId { get; }
    DateTime Timestamp { get; }
    string Source { get; }
}

public interface IGameCommand<out TResponse> : IRequest<TResponse>
{
    string CommandId { get; }
    DateTime Timestamp { get; }
    string Source { get; }
}

// 命令基类
public abstract record GameCommand : IGameCommand
{
    public string CommandId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.Now;
    public string Source { get; set; } = "Unknown";
}

public abstract record GameCommand<TResponse> : IGameCommand<TResponse>
{
    public string CommandId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.Now;
    public string Source { get; set; } = "Unknown";
}
```

### 2. 事件系统定义

```csharp
// 游戏事件接口
public interface IGameEvent
{
    string EventId { get; }
    DateTime Timestamp { get; }
    string Source { get; }
    string CorrelationId { get; } // 关联到原始命令
}

// 事件基类
public abstract record GameEvent : IGameEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.Now;
    public string Source { get; set; } = "Unknown";
    public string CorrelationId { get; set; } = string.Empty;
}

// 事件总线接口
public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IGameEvent;
    IDisposable Subscribe<T>(Func<T, Task> handler) where T : IGameEvent;
    IDisposable Subscribe<T>(Func<T, bool> filter, Func<T, Task> handler) where T : IGameEvent;
    IDisposable SubscribeOnce<T>(Func<T, Task> handler) where T : IGameEvent;
}
```

### 3. 增强的命令处理器基类

```csharp
// 命令处理器基类，集成事件发布功能
public abstract class GameCommandHandler<TCommand> : IRequestHandler<TCommand>
    where TCommand : IGameCommand
{
    protected readonly IEventBus EventBus;
    protected readonly ILogger Logger;

    protected GameCommandHandler(IEventBus eventBus, ILogger logger)
    {
        EventBus = eventBus;
        Logger = logger;
    }

    public async Task Handle(TCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug($"Handling command {typeof(TCommand).Name} with ID {request.CommandId}");
            
            // 执行具体的命令处理逻辑
            await HandleCommandAsync(request, cancellationToken);
            
            // 发布命令完成事件
            await PublishCommandCompletedEventAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to handle command {typeof(TCommand).Name} with ID {request.CommandId}");
            
            // 发布命令失败事件
            await PublishCommandFailedEventAsync(request, ex, cancellationToken);
            throw;
        }
    }

    protected abstract Task HandleCommandAsync(TCommand command, CancellationToken cancellationToken);

    protected async Task PublishEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IGameEvent
    {
        await EventBus.PublishAsync(@event, cancellationToken);
    }

    protected virtual async Task PublishCommandCompletedEventAsync(TCommand command, CancellationToken cancellationToken)
    {
        await PublishEventAsync(new CommandCompletedEvent
        {
            CorrelationId = command.CommandId,
            CommandType = typeof(TCommand).Name,
            Success = true,
            Source = GetType().Name
        }, cancellationToken);
    }

    protected virtual async Task PublishCommandFailedEventAsync(TCommand command, Exception exception, CancellationToken cancellationToken)
    {
        await PublishEventAsync(new CommandFailedEvent
        {
            CorrelationId = command.CommandId,
            CommandType = typeof(TCommand).Name,
            Error = exception.Message,
            Source = GetType().Name
        }, cancellationToken);
    }
}

// 带返回值的命令处理器基类
public abstract class GameCommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : IGameCommand<TResponse>
{
    protected readonly IEventBus EventBus;
    protected readonly ILogger Logger;

    protected GameCommandHandler(IEventBus eventBus, ILogger logger)
    {
        EventBus = eventBus;
        Logger = logger;
    }

    public async Task<TResponse> Handle(TCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug($"Handling command {typeof(TCommand).Name} with ID {request.CommandId}");
            
            var result = await HandleCommandAsync(request, cancellationToken);
            
            await PublishCommandCompletedEventAsync(request, result, cancellationToken);
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to handle command {typeof(TCommand).Name} with ID {request.CommandId}");
            
            await PublishCommandFailedEventAsync(request, ex, cancellationToken);
            throw;
        }
    }

    protected abstract Task<TResponse> HandleCommandAsync(TCommand command, CancellationToken cancellationToken);

    protected async Task PublishEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IGameEvent
    {
        await EventBus.PublishAsync(@event, cancellationToken);
    }

    protected virtual async Task PublishCommandCompletedEventAsync(TCommand command, TResponse result, CancellationToken cancellationToken)
    {
        await PublishEventAsync(new CommandCompletedEvent<TResponse>
        {
            CorrelationId = command.CommandId,
            CommandType = typeof(TCommand).Name,
            Result = result,
            Success = true,
            Source = GetType().Name
        }, cancellationToken);
    }

    protected virtual async Task PublishCommandFailedEventAsync(TCommand command, Exception exception, CancellationToken cancellationToken)
    {
        await PublishEventAsync(new CommandFailedEvent
        {
            CorrelationId = command.CommandId,
            CommandType = typeof(TCommand).Name,
            Error = exception.Message,
            Source = GetType().Name
        }, cancellationToken);
    }
}
```

### 4. 事件总线实现

```csharp
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<IEventSubscription>> _subscriptions = new();
    private readonly ILogger<EventBus> _logger;
    private readonly SemaphoreSlim _publishSemaphore = new(1, 1);

    public EventBus(ILogger<EventBus> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IGameEvent
    {
        await _publishSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug($"Publishing event {typeof(T).Name} with ID {@event.EventId}");

            if (!_subscriptions.TryGetValue(typeof(T), out var subscriptions))
            {
                _logger.LogDebug($"No subscriptions found for event {typeof(T).Name}");
                return;
            }

            var tasks = new List<Task>();
            var subscriptionsToRemove = new List<IEventSubscription>();

            foreach (var subscription in subscriptions.ToList())
            {
                try
                {
                    if (subscription.IsValid)
                    {
                        var task = subscription.HandleAsync(@event, cancellationToken);
                        tasks.Add(task);
                        
                        if (subscription.IsOnce)
                        {
                            subscriptionsToRemove.Add(subscription);
                        }
                    }
                    else
                    {
                        subscriptionsToRemove.Add(subscription);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in event subscription for {typeof(T).Name}");
                    subscriptionsToRemove.Add(subscription);
                }
            }

            // 清理无效订阅
            foreach (var subscription in subscriptionsToRemove)
            {
                subscriptions.Remove(subscription);
            }

            // 并行执行所有事件处理器
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }

            _logger.LogDebug($"Event {typeof(T).Name} published to {tasks.Count} handlers");
        }
        finally
        {
            _publishSemaphore.Release();
        }
    }

    public IDisposable Subscribe<T>(Func<T, Task> handler) where T : IGameEvent
    {
        return Subscribe<T>(_ => true, handler, false);
    }

    public IDisposable Subscribe<T>(Func<T, bool> filter, Func<T, Task> handler) where T : IGameEvent
    {
        return Subscribe<T>(filter, handler, false);
    }

    public IDisposable SubscribeOnce<T>(Func<T, Task> handler) where T : IGameEvent
    {
        return Subscribe<T>(_ => true, handler, true);
    }

    private IDisposable Subscribe<T>(Func<T, bool> filter, Func<T, Task> handler, bool isOnce) where T : IGameEvent
    {
        var subscription = new EventSubscription<T>(filter, handler, isOnce);
        
        _subscriptions.AddOrUpdate(typeof(T),
            new List<IEventSubscription> { subscription },
            (key, existing) => { existing.Add(subscription); return existing; });

        _logger.LogDebug($"Added subscription for event {typeof(T).Name}");
        return subscription;
    }
}

// 事件订阅接口和实现
interface IEventSubscription : IDisposable
{
    bool IsValid { get; }
    bool IsOnce { get; }
    Task HandleAsync(object @event, CancellationToken cancellationToken);
}

class EventSubscription<T> : IEventSubscription where T : IGameEvent
{
    private readonly Func<T, bool> _filter;
    private readonly Func<T, Task> _handler;
    private readonly bool _isOnce;
    private bool _disposed = false;

    public EventSubscription(Func<T, bool> filter, Func<T, Task> handler, bool isOnce)
    {
        _filter = filter;
        _handler = handler;
        _isOnce = isOnce;
    }

    public bool IsValid => !_disposed;
    public bool IsOnce => _isOnce;

    public async Task HandleAsync(object @event, CancellationToken cancellationToken)
    {
        if (_disposed || @event is not T typedEvent)
            return;

        if (_filter(typedEvent))
        {
            await _handler(typedEvent);
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
```

### 5. 节点层集成

```csharp
// 节点基类，集成MediatR和事件总线
public abstract class MediatorEventNode<T> : Node where T : Node
{
    protected IMediator Mediator { get; private set; }
    protected IEventBus EventBus { get; private set; }
    private readonly List<IDisposable> _eventSubscriptions = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _pendingCommands = new();
    private bool _disposed = false;

    public override void _Ready()
    {
        // 获取服务实例
        Mediator = GetService<IMediator>();
        EventBus = GetService<IEventBus>();
        
        // 订阅事件
        SubscribeToEvents();
        
        base._Ready();
    }

    public override void _ExitTree()
    {
        UnsubscribeFromEvents();
        base._ExitTree();
    }

    protected abstract void SubscribeToEvents();

    // 发送命令（不等待结果）
    protected async Task SendCommandAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : IGameCommand
    {
        command.Source = Name;
        await Mediator.Send(command, cancellationToken);
    }

    // 发送命令并等待结果
    protected async Task<TResponse> SendCommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : IGameCommand<TResponse>
    {
        command.Source = Name;
        return await Mediator.Send(command, cancellationToken);
    }

    // 发送命令并异步等待特定事件
    protected async Task<TEvent> SendCommandAndWaitForEventAsync<TCommand, TEvent>(
        TCommand command, 
        Func<TEvent, bool> eventFilter = null, 
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default) 
        where TCommand : IGameCommand
        where TEvent : IGameEvent
    {
        command.Source = Name;
        
        var tcs = new TaskCompletionSource<TEvent>();
        _pendingCommands[command.CommandId] = tcs as TaskCompletionSource<object>;
        
        // 订阅相关事件
        var subscription = EventBus.Subscribe<TEvent>(
            evt => (eventFilter?.Invoke(evt) ?? true) && evt.CorrelationId == command.CommandId,
            async evt =>
            {
                if (_pendingCommands.TryRemove(command.CommandId, out var completionSource))
                {
                    (completionSource as TaskCompletionSource<TEvent>)?.SetResult(evt);
                }
            }
        );
        
        try
        {
            // 发送命令
            await Mediator.Send(command, cancellationToken);
            
            // 设置超时
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);
            cts.Token.Register(() =>
            {
                if (_pendingCommands.TryRemove(command.CommandId, out var completionSource))
                {
                    (completionSource as TaskCompletionSource<TEvent>)?.SetException(
                        new TimeoutException($"Command {command.CommandId} timed out waiting for event {typeof(TEvent).Name}"));
                }
            });
            
            return await tcs.Task;
        }
        finally
        {
            subscription.Dispose();
            _pendingCommands.TryRemove(command.CommandId, out _);
        }
    }

    // 订阅事件
    protected IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IGameEvent
    {
        var subscription = EventBus.Subscribe<TEvent>(handler);
        _eventSubscriptions.Add(subscription);
        return subscription;
    }

    protected IDisposable Subscribe<TEvent>(Func<TEvent, bool> filter, Func<TEvent, Task> handler) where TEvent : IGameEvent
    {
        var subscription = EventBus.Subscribe<TEvent>(filter, handler);
        _eventSubscriptions.Add(subscription);
        return subscription;
    }

    protected IDisposable SubscribeOnce<TEvent>(Func<TEvent, Task> handler) where TEvent : IGameEvent
    {
        var subscription = EventBus.SubscribeOnce<TEvent>(handler);
        _eventSubscriptions.Add(subscription);
        return subscription;
    }

    private void UnsubscribeFromEvents()
    {
        if (_disposed) return;
        
        foreach (var subscription in _eventSubscriptions)
        {
            subscription?.Dispose();
        }
        _eventSubscriptions.Clear();
        
        // 取消所有等待中的命令
        foreach (var kvp in _pendingCommands)
        {
            kvp.Value.SetCanceled();
        }
        _pendingCommands.Clear();
        
        _disposed = true;
    }

    private T GetService<T>()
    {
        // 通过服务定位器或依赖注入获取服务
        var serviceLocator = GetNode("/root/ServiceLocator") as ServiceLocator;
        return serviceLocator.GetService<T>();
    }
}
```

### 6. 具体实现示例

#### A. 重构现有的音频命令

```csharp
// 重构后的音频命令（继承GameCommand）
public record PlaySoundCommand(string Path, float Volume = 1.0f) : GameCommand;

public record PlayMusicCommand(string Path, float FadeDuration = 1.0f, bool Loop = true) : GameCommand;

public record SetVolumeCommand(AudioEnums.AudioType Type, float Volume) : GameCommand;

// 音频事件定义
public record SoundPlayedEvent : GameEvent
{
    public string Path { get; init; }
    public float Volume { get; init; }
    public bool Success { get; init; }
}

public record MusicStartedEvent : GameEvent
{
    public string Path { get; init; }
    public float FadeDuration { get; init; }
    public bool Loop { get; init; }
}

public record VolumeChangedEvent : GameEvent
{
    public AudioEnums.AudioType Type { get; init; }
    public float OldVolume { get; init; }
    public float NewVolume { get; init; }
}
```

#### B. 重构后的音频命令处理器

```csharp
// 重构后的播放音效命令处理器
public class PlaySoundCommandHandler : GameCommandHandler<PlaySoundCommand>
{
    private readonly IAudioManagerService _audioManagerService;

    public PlaySoundCommandHandler(
        IAudioManagerService audioManagerService,
        IEventBus eventBus,
        ILogger<PlaySoundCommandHandler> logger) 
        : base(eventBus, logger)
    {
        _audioManagerService = audioManagerService;
    }

    protected override async Task HandleCommandAsync(PlaySoundCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // 执行音频播放
            _audioManagerService.PlaySound(command.Path, command.Volume);
            
            // 发布音效播放事件
            await PublishEventAsync(new SoundPlayedEvent
            {
                CorrelationId = command.CommandId,
                Path = command.Path,
                Volume = command.Volume,
                Success = true,
                Source = nameof(PlaySoundCommandHandler)
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            // 发布播放失败事件
            await PublishEventAsync(new SoundPlayedEvent
            {
                CorrelationId = command.CommandId,
                Path = command.Path,
                Volume = command.Volume,
                Success = false,
                Source = nameof(PlaySoundCommandHandler)
            }, cancellationToken);
            
            throw;
        }
    }
}

// 设置音量命令处理器
public class SetVolumeCommandHandler : GameCommandHandler<SetVolumeCommand>
{
    private readonly IAudioManagerService _audioManagerService;

    public SetVolumeCommandHandler(
        IAudioManagerService audioManagerService,
        IEventBus eventBus,
        ILogger<SetVolumeCommandHandler> logger) 
        : base(eventBus, logger)
    {
        _audioManagerService = audioManagerService;
    }

    protected override async Task HandleCommandAsync(SetVolumeCommand command, CancellationToken cancellationToken)
    {
        var oldVolume = _audioManagerService.GetVolume(command.Type);
        
        // 设置音量
        _audioManagerService.SetVolume(command.Type, command.Volume);
        
        // 发布音量变化事件
        await PublishEventAsync(new VolumeChangedEvent
        {
            CorrelationId = command.CommandId,
            Type = command.Type,
            OldVolume = oldVolume,
            NewVolume = command.Volume,
            Source = nameof(SetVolumeCommandHandler)
        }, cancellationToken);
    }
}
```

#### C. 游戏逻辑命令示例

```csharp
// 游戏逻辑命令
public record MovePlayerCommand(string PlayerId, Vector2 Direction, float Speed) : GameCommand;

public record AttackCommand(string AttackerId, string TargetId, float Damage, Vector2 Position) : GameCommand;

public record GetPlayerInfoCommand(string PlayerId) : GameCommand<PlayerInfo>;

// 游戏事件
public record PlayerMovedEvent : GameEvent
{
    public string PlayerId { get; init; }
    public Vector2 OldPosition { get; init; }
    public Vector2 NewPosition { get; init; }
    public Vector2 Direction { get; init; }
}

public record AttackExecutedEvent : GameEvent
{
    public string AttackerId { get; init; }
    public string TargetId { get; init; }
    public float Damage { get; init; }
    public bool Hit { get; init; }
    public Vector2 Position { get; init; }
}

public record HealthChangedEvent : GameEvent
{
    public string EntityId { get; init; }
    public float OldHealth { get; init; }
    public float NewHealth { get; init; }
    public string Reason { get; init; }
}

// 命令处理器
public class MovePlayerCommandHandler : GameCommandHandler<MovePlayerCommand>
{
    private readonly IGameLogicService _gameLogicService;

    public MovePlayerCommandHandler(
        IGameLogicService gameLogicService,
        IEventBus eventBus,
        ILogger<MovePlayerCommandHandler> logger) 
        : base(eventBus, logger)
    {
        _gameLogicService = gameLogicService;
    }

    protected override async Task HandleCommandAsync(MovePlayerCommand command, CancellationToken cancellationToken)
    {
        var result = await _gameLogicService.MovePlayerAsync(command.PlayerId, command.Direction, command.Speed);
        
        await PublishEventAsync(new PlayerMovedEvent
        {
            CorrelationId = command.CommandId,
            PlayerId = command.PlayerId,
            OldPosition = result.OldPosition,
            NewPosition = result.NewPosition,
            Direction = command.Direction,
            Source = nameof(MovePlayerCommandHandler)
        }, cancellationToken);
    }
}

public class GetPlayerInfoCommandHandler : GameCommandHandler<GetPlayerInfoCommand, PlayerInfo>
{
    private readonly IPlayerService _playerService;

    public GetPlayerInfoCommandHandler(
        IPlayerService playerService,
        IEventBus eventBus,
        ILogger<GetPlayerInfoCommandHandler> logger) 
        : base(eventBus, logger)
    {
        _playerService = playerService;
    }

    protected override async Task<PlayerInfo> HandleCommandAsync(GetPlayerInfoCommand command, CancellationToken cancellationToken)
    {
        return await _playerService.GetPlayerInfoAsync(command.PlayerId);
    }
}
```

#### D. 节点层使用示例

```csharp
// 玩家节点实现
public partial class PlayerNode : MediatorEventNode<CharacterBody2D>
{
    [Export] public float Speed = 300.0f;
    [Export] public float Health = 100.0f;
    
    private CharacterBody2D _body;
    private AnimationPlayer _animationPlayer;
    private bool _isMoving = false;

    public override void _Ready()
    {
        _body = this as CharacterBody2D;
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        
        base._Ready();
    }

    protected override void SubscribeToEvents()
    {
        // 订阅自己的移动完成事件
        Subscribe<PlayerMovedEvent>(
            evt => evt.PlayerId == Name,
            HandlePlayerMovedEvent
        );
        
        // 订阅健康变化事件
        Subscribe<HealthChangedEvent>(
            evt => evt.EntityId == Name,
            HandleHealthChangedEvent
        );
        
        // 订阅攻击执行事件
        Subscribe<AttackExecutedEvent>(
            evt => evt.AttackerId == Name || evt.TargetId == Name,
            HandleAttackExecutedEvent
        );
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_isMoving)
        {
            var direction = GetMovementInput();
            if (direction != Vector2.Zero)
            {
                _ = RequestMoveAsync(direction);
            }
        }
    }

    private async Task RequestMoveAsync(Vector2 direction)
    {
        _isMoving = true;
        
        try
        {
            // 方式1：发送命令并等待移动完成事件
            var moveEvent = await SendCommandAndWaitForEventAsync<MovePlayerCommand, PlayerMovedEvent>(
                new MovePlayerCommand(Name, direction, Speed),
                evt => evt.PlayerId == Name
            );
            
            GD.Print($"Player {Name} moved from {moveEvent.OldPosition} to {moveEvent.NewPosition}");
        }
        catch (TimeoutException)
        {
            GD.PrintErr($"Move command timed out for player {Name}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Move command failed for player {Name}: {ex.Message}");
        }
        finally
        {
            _isMoving = false;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            _ = RequestAttackAsync(mouseEvent.GlobalPosition);
        }
    }

    private async Task RequestAttackAsync(Vector2 targetPosition)
    {
        try
        {
            var target = FindNearestEnemy(targetPosition);
            if (target != null)
            {
                // 方式2：发送命令，通过事件订阅异步接收结果
                await SendCommandAsync(new AttackCommand(Name, target.Name, 25.0f, targetPosition));
                
                _animationPlayer.Play("attack");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Attack command failed for player {Name}: {ex.Message}");
        }
    }

    // 事件处理方法
    private async Task HandlePlayerMovedEvent(PlayerMovedEvent evt)
    {
        GlobalPosition = evt.NewPosition;
        
        if (evt.Direction != Vector2.Zero)
        {
            _animationPlayer.Play("walk");
        }
        else
        {
            _animationPlayer.Play("idle");
        }
        
        GD.Print($"Player {Name} position updated to {evt.NewPosition}");
    }

    private async Task HandleHealthChangedEvent(HealthChangedEvent evt)
    {
        Health = evt.NewHealth;
        UpdateHealthBar();
        
        if (evt.NewHealth < evt.OldHealth)
        {
            _animationPlayer.Play("hurt");
        }
        
        if (Health <= 0)
        {
            await HandlePlayerDeath();
        }
    }

    private async Task HandleAttackExecutedEvent(AttackExecutedEvent evt)
    {
        if (evt.AttackerId == Name)
        {
            if (evt.Hit)
            {
                GD.Print($"Player {Name} successfully attacked {evt.TargetId}");
                PlayAttackHitEffect(evt.Position);
            }
            else
            {
                GD.Print($"Player {Name} missed attack on {evt.TargetId}");
                PlayAttackMissEffect(evt.Position);
            }
        }
        else if (evt.TargetId == Name && evt.Hit)
        {
            GD.Print($"Player {Name} was hit by {evt.AttackerId}");
            PlayHitEffect();
        }
    }

    // 辅助方法
    private Vector2 GetMovementInput()
    {
        var direction = Vector2.Zero;
        if (Input.IsActionPressed("move_up")) direction.Y -= 1;
        if (Input.IsActionPressed("move_down")) direction.Y += 1;
        if (Input.IsActionPressed("move_left")) direction.X -= 1;
        if (Input.IsActionPressed("move_right")) direction.X += 1;
        return direction.Normalized();
    }

    private Node FindNearestEnemy(Vector2 position) { /* 实现逻辑 */ return null; }
    private async Task HandlePlayerDeath() { /* 实现逻辑 */ }
    private void UpdateHealthBar() { /* 实现逻辑 */ }
    private void PlayAttackHitEffect(Vector2 position) { /* 实现逻辑 */ }
    private void PlayAttackMissEffect(Vector2 position) { /* 实现逻辑 */ }
    private void PlayHitEffect() { /* 实现逻辑 */ }
}
```

### 7. 依赖注入配置更新

```csharp
// 更新后的MediatorModule
public class MediatorModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // 注册事件总线
        builder.RegisterType<EventBus>().As<IEventBus>().SingleInstance();
        
        // 注册MediatR
        var handlersFromAssembly = Assembly.Load("MF.CommandHandlers");
        var commandAssembly = Assembly.Load("MF.Commands");
        var servicesAssembly = Assembly.Load("MF.Services");
        var eventsAssembly = Assembly.Load("MF.Events"); // 新增事件程序集
        
        var configuration = MediatRConfigurationBuilder
            .Create(handlersFromAssembly, commandAssembly, servicesAssembly, eventsAssembly)
            .WithAllOpenGenericHandlerTypesRegistered()
            .Build();
            
        builder.RegisterMediatR(configuration);
        
        // 注册所有命令处理器（确保它们能获取到IEventBus）
        builder.RegisterAssemblyTypes(handlersFromAssembly)
            .AsClosedTypesOf(typeof(GameCommandHandler<>))
            .InstancePerDependency();
            
        builder.RegisterAssemblyTypes(handlersFromAssembly)
            .AsClosedTypesOf(typeof(GameCommandHandler<,>))
            .InstancePerDependency();
    }
}

// 服务容器配置
public partial class ServiceContainer : Node
{
    private IServiceProvider _serviceProvider;
    private IEventBus _eventBus;
    private IMediator _mediator;

    public override void _Ready()
    {
        ConfigureServices();
        
        // 设置全局访问
        GetTree().SetMeta("EventBus", _eventBus);
        GetTree().SetMeta("Mediator", _mediator);
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // 注册日志
        services.AddLogging(builder => builder.AddConsole());
        
        // 注册业务服务
        services.AddSingleton<IGameLogicService, GameLogicService>();
        services.AddSingleton<IAudioManagerService, AudioManagerService>();
        services.AddSingleton<IPlayerService, PlayerService>();
        
        // 使用Autofac作为容器
        var containerBuilder = new ContainerBuilder();
        containerBuilder.Populate(services);
        containerBuilder.RegisterModule<MediatorModule>();
        
        var container = containerBuilder.Build();
        _serviceProvider = new AutofacServiceProvider(container);
        
        _eventBus = _serviceProvider.GetRequiredService<IEventBus>();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    public T GetService<T>() => _serviceProvider.GetRequiredService<T>();
}
```

### 8. 系统事件定义

```csharp
// 系统级事件
public record CommandCompletedEvent : GameEvent
{
    public string CommandType { get; init; }
    public bool Success { get; init; }
}

public record CommandCompletedEvent<T> : GameEvent
{
    public string CommandType { get; init; }
    public T Result { get; init; }
    public bool Success { get; init; }
}

public record CommandFailedEvent : GameEvent
{
    public string CommandType { get; init; }
    public string Error { get; init; }
}

// 游戏状态事件
public record GameStateChangedEvent : GameEvent
{
    public string OldState { get; init; }
    public string NewState { get; init; }
    public Dictionary<string, object> StateData { get; init; } = new();
}

// UI事件
public record UIShowEvent : GameEvent
{
    public string UIName { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
    public bool Modal { get; init; }
}

public record UIHideEvent : GameEvent
{
    public string UIName { get; init; }
    public bool Immediate { get; init; }
}
```

## 架构优势

### 1. MediatR集成优势
- **成熟生态**：利用MediatR的成熟功能和社区支持
- **标准化**：遵循.NET社区的CQRS最佳实践
- **工具支持**：丰富的调试和监控工具
- **性能优化**：MediatR的内置性能优化

### 2. 双向通信优势
- **异步响应**：命令立即返回，事件异步通知
- **完全解耦**：节点层和服务层通过中介者通信
- **灵活订阅**：节点可以选择性订阅感兴趣的事件
- **关联追踪**：通过CorrelationId关联命令和事件

### 3. 开发体验优势
- **类型安全**：强类型命令和事件定义
- **易于测试**：可以独立测试命令处理和事件响应
- **代码复用**：基类提供通用功能，减少样板代码
- **调试友好**：清晰的调用链和事件流

### 4. 扩展性优势
- **新功能添加**：只需定义命令、事件和处理器
- **向后兼容**：可以逐步迁移现有代码
- **模块化**：每个功能模块可以独立开发和部署
- **插件支持**：第三方可以轻松扩展命令和事件

## 迁移策略

### 第一阶段：基础设施搭建
1. 实现事件总线和基类
2. 更新依赖注入配置
3. 创建系统事件定义

### 第二阶段：现有命令重构
1. 将现有命令继承GameCommand
2. 重构命令处理器继承GameCommandHandler
3. 添加相应的事件定义和发布

### 第三阶段：节点层集成
1. 创建MediatorEventNode基类
2. 逐步迁移现有节点
3. 实现事件订阅和命令发送

### 第四阶段：功能完善
1. 添加更多游戏逻辑命令和事件
2. 优化性能和错误处理
3. 完善日志和监控

## 总结

基于MediatR的双向CQRS架构方案完美结合了：

1. **MediatR的成熟性**：利用现有的生态和最佳实践
2. **双向通信的优雅性**：命令-事件的异步响应机制
3. **架构的清晰性**：明确的职责分离和通信边界
4. **开发的高效性**：减少样板代码，提高开发效率

这种方案既保持了我们讨论的架构优势，又充分利用了MediatR的强大功能，是ModularGodot框架的理想选择！