# 命名规范（MediatR/CQRS + Godot C#）

适用范围：本规范用于 ModularGodot.Framework 及其示例项目中的应用层（Commands/Queries/Handlers）、契约（DTO/Result/Response）、通知（Notification）以及相关目录/命名空间组织。目标是提高一致性、可读性与可维护性，减少命名歧义与层间耦合。

## 1. 总体原则
- 面向业务意图命名，避免技术性或含糊不清的名称。
- 通过统一后缀清晰地区分不同类型：Command/Query/Notification/Handler/Dto/Request/Response/Result。
- 命名应体现幂等性与事务边界（如 Start/Stop vs Set/Enable）。
- 应用层不直接依赖 Godot 的具体类型（Node/Resource 等），使用接口与 DTO 传递数据。

## 2. 类与消息命名规范

### 2.1 Command（写操作，改变状态）
- 格式：动词 + 名词 + Command
- 示例：StartResourceStressTestCommand、StopResourceStressTestCommand、UpdateStressTestLimitsCommand
- 反例：DoItCommand、ProcessCommand、HandleSomethingCommand（意图不清）

### 2.2 Query（读操作，仅读取）
- 格式：Get/List/Find/Count + 名词 + Query
- 示例：GetResourceStressTestProgressQuery、ListResourceStressTestsQuery、FindStressTestsByTagQuery
- 反例：GetAndStartQuery（读写混合）

### 2.3 Notification / Event（消息广播）
- 格式：名词（或过去式/完成式）+ Notification（或 DomainEvent）
- 示例：ResourceStressTestCompletedNotification、ResourceStressTestProgressedNotification

### 2.4 输入/输出 DTO
- 输入（请求）DTO：与 Command/Query 同名 + Request 或 + Dto（项目统一即可）
  - 示例：StartResourceStressTestRequest 或 StartResourceStressTestDto
- 输出（结果）DTO：与消息语义一致，使用 Result/Response 后缀
  - 示例：ResourceStressTestProgressResult、ListResourceStressTestsResponse

### 2.5 Handler
- 与消息一一对应：同名 + Handler
- 示例：StartResourceStressTestCommandHandler、GetResourceStressTestProgressQueryHandler

### 2.6 接口与抽象
- 应用门面/用例集合：IGameUseCases、IResourceTestingUseCases
- 基础设施/领域服务接口：IResourceService、IStressTestScheduler

### 2.7 异常
- 语义化且聚焦业务：ResourceNotFoundException、StressTestAlreadyRunningException

## 3. 命名空间与目录结构

建议在 Core/2_App 下按“消息类型 + 领域分组”组织文件，示例如下：

- ModularGodot.Framework/Core/2_App/
  - MF.Commands/
    - ResourceManagement/
      - StartResourceStressTestCommand.cs
      - StopResourceStressTestCommand.cs
  - MF.Queries/
    - ResourceManagement/
      - GetResourceStressTestProgressQuery.cs
      - ListResourceStressTestsQuery.cs
  - MF.Notifications/
    - ResourceManagement/
      - ResourceStressTestCompletedNotification.cs
  - MF.CommandHandlers/
    - ResourceManagement/
      - StartResourceStressTestCommandHandler.cs
      - StopResourceStressTestCommandHandler.cs
  - MF.QueryHandlers/
    - ResourceManagement/
      - GetResourceStressTestProgressQueryHandler.cs
      - ListResourceStressTestsQueryHandler.cs
  - MF.NotificationHandlers/
    - ResourceManagement/
      - ResourceStressTestCompletedNotificationHandler.cs
  - MF.Contracts/
    - ResourceManagement/
      - StartResourceStressTestRequest.cs
      - ResourceStressTestProgressResult.cs

命名空间（建议与目录对应）：
- ModularGodot.Framework.Core._Layer_.MF.Commands.ResourceManagement
- ModularGodot.Framework.Core._Layer_.MF.CommandHandlers.ResourceManagement
- ModularGodot.Framework.Core._Layer_.MF.Contracts.ResourceManagement
- 其中 _Layer_ 在当前项目为 2_App。

前端/节点层建议：
- 位于 Core/1_1_Fronted（现有目录名）
  - MF.Nodes.Abstractions/（已有）
  - 可添加 MF.Nodes.AppFacade/（应用门面在节点层的适配；类型命名如 GameUseCasesFacade）
- 节点层仅依赖门面接口或 IMediator（通过门面封装更佳），不直接依赖具体 Handler。

## 4. 动词选择指南
- 幂等写操作：Set/Assign/Upsert/Enable/Disable
- 非幂等写操作：Create/Start/Stop/Delete/Increment/Schedule
- 读取：Get/List/Find/Count/Export
- 编排/批处理：Run/Execute/Schedule（确保与领域语义一致）

## 5. ResourceStressTest 示例映射
- Commands
  - StartResourceStressTestCommand（参数：ResourcePaths、Concurrency、BatchSize、Throttle、Warmup 等）
  - StopResourceStressTestCommand（参数：TestId）
- Queries
  - GetResourceStressTestProgressQuery（参数：TestId；返回：ResourceStressTestProgressResult）
  - ListResourceStressTestsQuery（参数：过滤/分页；返回：List<ResourceStressTestSummary>）
- Notifications
  - ResourceStressTestCompletedNotification

上述所有消息在不同“场景”（UI/节点）中复用，通过参数表达差异，而非为每个场景复制命令。

## 6. 反例对照
- Bad：ResourceStressTestHandler、DoStressTestCommand、ProgressCommand
- Good：StartResourceStressTestCommand、GetResourceStressTestProgressQuery、StartResourceStressTestCommandHandler

## 7. 迁移与落地建议
- 逐步将现有 MF.CommandHandlers 中“多职能”处理器拆分为按用例粒度的独立处理器。
- 引入 MF.Contracts 统一承载请求/结果 DTO，避免 Godot 类型渗透到应用层。
- 为节点层提供 Application Facade（例如 IResourceTestingUseCases），内部路由到 MediatR 命令/查询。
- 定期审视命令清单：合并仅参数不同的命令；拆分跨越多个事务边界的臃肿命令。

## 8. 提交前检查清单（Checklist）
- [ ] 新增的写操作是否以 XxxxCommand 结尾，并清晰表达动作与边界？
- [ ] 读取是否使用 XxxxQuery，且不包含副作用？
- [ ] Handler 是否与消息一一对应，命名为 XxxxHandler？
- [ ] 输入输出 DTO 是否使用 Request/Dto 与 Result/Response 后缀？
- [ ] 是否按目录/命名空间进行资源领域分组？
- [ ] 应用层是否避免了 Godot 引擎类型的直接依赖？