using MF.Contexts.Attributes;

namespace MF.Contexts.Examples;

/// <summary>
/// 使用SkipRegistrationAttribute的示例
/// </summary>
public static class SkipRegistrationExample
{
    /// <summary>
    /// 正常会被注册的服务类
    /// </summary>
    public class NormalService
    {
        public void DoSomething() { }
    }
    
    /// <summary>
    /// 标记跳过注册的服务类
    /// </summary>
    [SkipRegistration]
    public class SkippedService
    {
        public void DoSomething() { }
    }
    
    /// <summary>
    /// 带原因说明的跳过注册服务类
    /// </summary>
    [SkipRegistration("This service is manually registered elsewhere")]
    public class ManuallyRegisteredService
    {
        public void DoSomething() { }
    }
    
    /// <summary>
    /// 测试用的服务类，跳过自动注册
    /// </summary>
    [SkipRegistration("Test class, should not be registered in production")]
    public class TestOnlyService
    {
        public void DoSomething() { }
    }
}