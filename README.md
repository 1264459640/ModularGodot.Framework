# ModularGodot.Framework

## 概述

ModularGodot.Framework 是 ModularGodot 项目的核心框架，提供了模块化游戏开发的基础设施和服务。

## 项目结构

```
ModularGodot.Framework/
├── Core/                           # 核心框架代码
│   ├── 0_Base/                     # 基础层
│   │   ├── MF.Commons/             # 通用工具和扩展
│   │   └── MF.Contexts/            # 上下文管理
│   ├── 1_1_Fronted/                # 前端层
│   │   └── MF.Nodes.Abstractions/  # 节点抽象
│   ├── 1_2_Backend/                # 后端层
│   │   ├── MF.Data/                # 数据模型
│   │   ├── MF.Infrastructure/      # 基础设施实现
│   │   ├── MF.Infrastructure.Abstractions/ # 基础设施抽象
│   │   ├── MF.Repositories/        # 仓储实现
│   │   └── MF.Repositories.Abstractions/   # 仓储抽象
│   ├── 2_App/                      # 应用层
│   │   ├── MF.CommandHandlers/     # 命令处理器
│   │   ├── MF.Commands/            # 命令定义
│   │   ├── MF.Events/              # 事件定义
│   │   ├── MF.Services/            # 服务实现
│   │   └── MF.Services.Abstractions/ # 服务抽象
│   └── 3_UniTest/                  # 单元测试
│       ├── MF.Infrastructure.Tests/ # 基础设施测试
│       └── MF.Services.Tests/      # 服务测试
├── Docs/                           # 文档
├── Extensions/                     # 扩展模块（被Git忽略）
└── Workspace/                      # 工作空间
```

## 特性

- **模块化架构**: 采用分层架构设计，支持模块化开发
- **依赖注入**: 内置依赖注入容器，支持服务注册和解析
- **事件驱动**: 基于事件的松耦合架构
- **缓存系统**: 内置高性能缓存服务
- **监控系统**: 提供性能监控和内存管理
- **资源管理**: 智能资源加载和管理系统

## 技术栈

- **.NET 9.0**: 基于最新的.NET平台
- **Godot 4.4+**: 支持Godot游戏引擎
- **MediatR**: 中介者模式实现
- **R3**: 响应式编程支持
- **xUnit**: 单元测试框架

## 开始使用

### 环境要求

- .NET 9.0 SDK
- Godot 4.4 或更高版本
- Visual Studio 2022 或 JetBrains Rider

### 构建项目

```bash
# 恢复NuGet包
dotnet restore

# 构建解决方案
dotnet build

# 运行测试
dotnet test
```

## 扩展开发

扩展模块应放置在 `Extensions/` 目录下。该目录已被Git忽略，允许开发者创建自定义扩展而不影响核心框架。

### 扩展结构示例

```
Extensions/
└── YourExtension/
    ├── Services/
    ├── Commands/
    ├── Events/
    └── extension.json
```

## 贡献指南

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 相关链接

- [ModularGodot 主项目](../)
- [文档](./Docs/)
- [问题反馈](../../issues)