namespace MF.Infrastructure.Abstractions.Core.EventBus;

/// <summary>
/// 事件总线抽象接口 - Critical级别
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 异步发布事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发布任务</returns>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : EventBase;
    
    /// <summary>
    /// 同步发布事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件实例</param>
    void Publish<TEvent>(TEvent @event) where TEvent : EventBase;
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理器</param>
    /// <returns>订阅句柄，用于取消订阅</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : EventBase;
    
    /// <summary>
    /// 异步订阅事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">异步事件处理器</param>
    /// <returns>订阅句柄，用于取消订阅</returns>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : EventBase;
    
    /// <summary>
    /// 条件订阅事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="filter">过滤条件</param>
    /// <param name="handler">事件处理器</param>
    /// <returns>订阅句柄，用于取消订阅</returns>
    IDisposable Subscribe<TEvent>(Func<TEvent, bool> filter, Action<TEvent> handler) where TEvent : EventBase;
    
    /// <summary>
    /// 一次性订阅事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理器</param>
    /// <returns>订阅句柄，用于取消订阅</returns>
    IDisposable SubscribeOnce<TEvent>(Action<TEvent> handler) where TEvent : EventBase;
    
}