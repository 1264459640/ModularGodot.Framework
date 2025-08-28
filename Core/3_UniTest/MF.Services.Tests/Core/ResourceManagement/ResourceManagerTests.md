# ResourceManager 测试用例文档

## 1. 测试概述

本文档详细描述了对 `ResourceManager` 类的单元测试用例。`ResourceManager` 是资源管理系统的核心组件，负责资源缓存、监控和清理等功能。测试将覆盖其作为 `IResourceCacheService` 和 `IResourceMonitorService` 接口实现的所有功能。

## 2. 测试环境

- .NET 9.0
- xUnit 测试框架
- Moq 用于模拟依赖项
- FluentAssertions 用于断言

## 3. 测试用例

### 3.1 构造函数测试

#### 3.1.1 正常初始化
- **目的**: 验证 `ResourceManager` 能够正确初始化并订阅内存监控事件
- **前置条件**: 
  - 提供有效的 `ICacheService`, `IMemoryMonitor`, `IPerformanceMonitor`, `IEventBus` 和 `ResourceSystemConfig` 实例
- **步骤**:
  1. 创建所有必需的模拟依赖项
  2. 实例化 `ResourceManager`
- **预期结果**: 
  - `ResourceManager` 实例成功创建
  - 内存监控事件被正确订阅
  - 如果配置启用，定时清理器被正确启动

#### 3.1.2 启用自动清理
- **目的**: 验证当配置启用自动清理时，定时器被正确初始化
- **前置条件**: 
  - 提供有效的依赖项
  - `ResourceSystemConfig.EnableAutoCleanup` 设置为 `true`
- **步骤**:
  1. 创建模拟依赖项，配置启用自动清理
  2. 实例化 `ResourceManager`
- **预期结果**: 
  - 定时清理器被创建并启动

### 3.2 资源缓存服务测试 (IResourceCacheService)

#### 3.2.1 GetAsync 成功获取缓存项
- **目的**: 验证能够成功从缓存中获取资源
- **前置条件**: 
  - `ResourceManager` 已初始化
  - 模拟的 `ICacheService.GetAsync` 返回一个有效的资源
- **步骤**:
  1. 调用 `GetAsync<T>` 方法
  2. 验证返回的资源是否正确
  3. 验证缓存命中统计是否增加
- **预期结果**: 
  - 返回正确的资源对象
  - 缓存命中计数增加
  - 发布 `ResourceLoadEvent` 事件，结果为 `CacheHit`

#### 3.2.2 GetAsync 缓存未命中
- **目的**: 验证当缓存中不存在资源时的行为
- **前置条件**: 
  - `ResourceManager` 已初始化
  - 模拟的 `ICacheService.GetAsync` 返回 `null`
- **步骤**:
  1. 调用 `GetAsync<T>` 方法
  2. 验证返回值是否为 `null`
  3. 验证缓存未命中统计是否增加
- **预期结果**: 
  - 返回 `null`
  - 缓存未命中计数增加
  - 发布 `ResourceLoadEvent` 事件，结果为 `CacheMiss`

#### 3.2.3 GetAsync 发生异常
- **目的**: 验证当缓存服务抛出异常时的处理
- **前置条件**: 
  - `ResourceManager` 已初始化
  - 模拟的 `ICacheService.GetAsync` 抛出异常
- **步骤**:
  1. 调用 `GetAsync<T>` 方法
  2. 验证是否抛出相同的异常
  3. 验证错误计数是否增加
- **预期结果**: 
  - 抛出与模拟服务相同的异常
  - 错误计数增加
  - 发布 `ResourceLoadEvent` 事件，结果为 `Failed`

#### 3.2.4 SetAsync 添加资源
- **目的**: 验证能够将资源添加到缓存中
- **前置条件**: 
  - `ResourceManager` 已初始化
- **步骤**:
  1. 准备一个资源对象
  2. 调用 `SetAsync<T>` 方法
  3. 验证资源是否被添加到内部跟踪字典
- **预期结果**: 
  - 调用 `ICacheService.SetAsync` 方法
  - 资源被添加到 `_cacheItems` 字典
  - 性能监控计数器增加

#### 3.2.5 SetAsync 不同缓存策略
- **目的**: 验证不同缓存策略对资源过期时间的影响
- **前置条件**: 
  - `ResourceManager` 已初始化
- **步骤**:
  1. 使用不同的 `ResourceCacheStrategy` 调用 `SetAsync<T>`
  2. 验证传递给 `ICacheService.SetAsync` 的过期时间是否正确
- **预期结果**: 
  - `NoCache` 和 `Permanent` 策略传递 `null` 作为过期时间
  - `Default` 和 `ForceCache` 策略使用配置中的默认过期时间
  - `Temporary` 策略使用预设的5分钟过期时间

#### 3.2.6 RemoveAsync 删除资源
- **目的**: 验证能够从缓存中删除资源
- **前置条件**: 
  - `ResourceManager` 已初始化
  - 模拟的 `ICacheService` 包含指定键的资源
- **步骤**:
  1. 调用 `RemoveAsync` 方法
  2. 验证资源是否从缓存和内部跟踪字典中移除
- **预期结果**: 
  - 调用 `ICacheService.RemoveAsync` 方法
  - 资源从 `_cacheItems` 字典中移除
  - 性能监控计数器增加

#### 3.2.7 CleanupAsync 手动清理
- **目的**: 验证手动触发清理功能
- **前置条件**: 
  - `ResourceManager` 已初始化
  - 内部跟踪字典中有一些过期资源
- **步骤**:
  1. 调用 `CleanupAsync` 方法
  2. 验证过期资源是否被清理
- **预期结果**: 
  - 调用 `PerformCleanup` 方法，原因设置为 `Manual`
  - 过期资源从缓存和内部跟踪字典中移除
  - 发布 `CacheCleanupEvent` 事件

#### 3.2.8 ExistsAsync 检查资源存在性
- **目的**: 验证能够检查资源是否存在于缓存中
- **前置条件**: 
  - `ResourceManager` 已初始化
- **步骤**:
  1. 调用 `ExistsAsync` 方法
  2. 验证返回值是否与模拟服务一致
- **预期结果**: 
  - 返回与 `ICacheService.ExistsAsync` 相同的结果

### 3.3 资源监控服务测试 (IResourceMonitorService)

#### 3.3.1 GetCacheStatisticsAsync 获取缓存统计
- **目的**: 验证能够正确获取缓存统计信息
- **前置条件**: 
  - `ResourceManager` 已初始化
  - 内部统计计数器有数据
- **步骤**:
  1. 调用 `GetCacheStatisticsAsync` 方法
  2. 验证返回的统计信息是否正确
- **预期结果**: 
  - 返回包含正确命中数、未命中数、总项数和过期项数的 `CacheStatistics` 对象

#### 3.3.2 GetMemoryUsageAsync 获取内存使用情况
- **目的**: 验证能够正确获取内存使用情况
- **前置条件**: 
  - `ResourceManager` 已初始化
- **步骤**:
  1. 调用 `GetMemoryUsageAsync` 方法
  2. 验证返回的内存使用信息是否正确
- **预期结果**: 
  - 返回包含当前使用量、最大使用量、使用百分比等信息的 `MemoryUsage` 对象

#### 3.3.3 GetPerformanceReportAsync 获取性能报告
- **目的**: 验证能够正确生成性能报告
- **前置条件**: 
  - `ResourceManager` 已初始化
  - 内部有响应时间统计数据
- **步骤**:
  1. 调用 `GetPerformanceReportAsync` 方法
  2. 验证返回的性能报告是否包含正确的统计数据
- **预期结果**: 
  - 返回包含缓存统计、内存统计、请求总数、平均响应时间等信息的 `PerformanceReport` 对象

#### 3.3.4 GetConfigurationAsync 获取配置
- **目的**: 验证能够正确获取当前资源配置
- **前置条件**: 
  - `ResourceManager` 已初始化
- **步骤**:
  1. 调用 `GetConfigurationAsync` 方法
  2. 验证返回的配置是否与初始化时提供的配置一致
- **预期结果**: 
  - 返回初始化时提供的 `ResourceSystemConfig` 实例

#### 3.3.5 UpdateConfigurationAsync 更新配置
- **目的**: 验证能够正确更新资源配置
- **前置条件**: 
  - `ResourceManager` 已初始化
- **步骤**:
  1. 准备一个新的 `ResourceSystemConfig` 实例
  2. 调用 `UpdateConfigurationAsync` 方法
  3. 验证内部配置是否被更新
- **预期结果**: 
  - 内部 `_config` 字段被更新
  - 内存监控器的阈值被同步更新

### 3.4 事件处理测试

#### 3.4.1 OnMemoryPressureDetected 内存压力处理
- **目的**: 验证内存压力事件处理逻辑
- **前置条件**: 
  - `ResourceManager` 已初始化
  - 内存使用量达到警告或危急水平
- **步骤**:
  1. 触发 `MemoryPressureDetected` 事件
  2. 验证是否发布 `MemoryPressureEvent` 事件
  3. 验证在警告及以上级别时是否触发清理
- **预期结果**: 
  - 发布 `MemoryPressureEvent` 事件
  - 在警告及以上级别时调用 `PerformCleanup` 方法

#### 3.4.2 OnCleanupTimer 定时清理
- **目的**: 验证定时清理功能
- **前置条件**: 
  - `ResourceManager` 已初始化且启用了自动清理
- **步骤**:
  1. 等待定时器触发或直接调用 `OnCleanupTimer`
  2. 验证是否执行清理逻辑
- **预期结果**: 
  - 调用 `PerformCleanup` 方法，原因设置为 `Scheduled`

### 3.5 私有方法测试

#### 3.5.1 PerformCleanup 执行清理
- **目的**: 验证清理逻辑的正确性
- **前置条件**: 
  - `ResourceManager` 已初始化
  - 内部跟踪字典中有过期资源
- **步骤**:
  1. 调用 `PerformCleanup` 方法
  2. 验证过期资源是否被正确清理
- **预期结果**: 
  - 过期资源从缓存和内部跟踪字典中移除
  - 发布 `CacheCleanupEvent` 事件
  - 性能监控计数器更新

#### 3.5.2 GetPressureLevel 获取压力级别
- **目的**: 验证内存压力级别的计算
- **前置条件**: 无
- **步骤**:
  1. 使用不同的使用百分比调用 `GetPressureLevel` 方法
  2. 验证返回的压力级别是否正确
- **预期结果**: 
  - 使用率 >= 90% 返回 `Critical`
  - 使用率 >= 80% 返回 `Warning`
  - 其他情况返回 `Normal`

#### 3.5.3 GetExpirationFromStrategy 策略到过期时间转换
- **目的**: 验证缓存策略到过期时间的转换逻辑
- **前置条件**: 无
- **步骤**:
  1. 使用不同的 `ResourceCacheStrategy` 调用 `GetExpirationFromStrategy` 方法
  2. 验证返回的过期时间是否正确
- **预期结果**: 
  - `NoCache` 和 `Permanent` 返回 `null`
  - `Default` 和 `ForceCache` 返回配置中的默认过期时间
  - `Temporary` 返回5分钟
  - 其他情况返回配置中的默认过期时间

## 4. 测试覆盖率

测试将覆盖 `ResourceManager` 类的所有公共方法和关键私有方法，确保以下方面得到验证：

- 所有正常执行路径
- 所有异常处理路径
- 所有事件发布逻辑
- 所有配置更新逻辑
- 所有统计和监控功能

## 5. 结论

通过以上测试用例，我们可以全面验证 `ResourceManager` 类的功能正确性和稳定性。这些测试将帮助确保资源管理系统在各种条件下都能正常工作，并为未来的功能扩展提供可靠的测试基础。