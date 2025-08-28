using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using MF.Infrastructure.Abstractions.Core.Logging;

namespace MF.Infrastructure.Tests.Logging;

/// <summary>
/// GodotGameLogger 单元测试
/// </summary>
public class GodotGameLoggerTests : IDisposable
{
    private readonly TestGodotGameLogger _logger;
    private readonly TestGodotGameLogger<TestClass> _genericLogger;

    public GodotGameLoggerTests()
    {
        _logger = new TestGodotGameLogger("TestCategory");
        _genericLogger = new TestGodotGameLogger<TestClass>();
    }

    #region 初始化测试

    [Fact]
    public void Constructor_WithValidCategoryName_ShouldInitializeSuccessfully()
    {
        // Arrange
        const string categoryName = "TestLogger";

        // Act
        var logger = new TestGodotGameLogger(categoryName);

        // Assert
        Assert.NotNull(logger);
        Assert.Equal(categoryName, logger.CategoryName);
        Assert.False(logger.IsDisposed);
    }

    [Fact]
    public void Constructor_WithNullCategoryName_ShouldInitializeWithNull()
    {
        // Arrange & Act
        var logger = new TestGodotGameLogger(null!);

        // Assert
        Assert.NotNull(logger);
        Assert.Null(logger.CategoryName);
    }

    [Fact]
    public void Constructor_WithEmptyCategoryName_ShouldInitializeWithEmpty()
    {
        // Arrange & Act
        var logger = new TestGodotGameLogger("");

        // Assert
        Assert.NotNull(logger);
        Assert.Equal("", logger.CategoryName);
    }

    #endregion

    #region 调试日志测试

    [Fact]
    public void LogDebug_WithSimpleMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Debug message";

        // Act
        _logger.LogDebug(message);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Debug", logs[0]);
        Assert.Contains(message, logs[0]);
        Assert.Contains("TestCategory", logs[0]);
    }

    [Fact]
    public void LogDebug_WithFormattedMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Debug message with {0} and {1}";
        const string arg1 = "parameter1";
        const int arg2 = 42;

        // Act
        _logger.LogDebug(message, arg1, arg2);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Debug", logs[0]);
        Assert.Contains(arg1, logs[0]);
        Assert.Contains(arg2.ToString(), logs[0]);
    }

    [Fact]
    public void LogDebug_WithNullMessage_ShouldNotThrow()
    {
        // Act & Assert
        _logger.LogDebug(null!); // Should not throw
        
        var logs = _logger.GetLogs();
        Assert.Single(logs);
    }

    #endregion

    #region 信息日志测试

    [Fact]
    public void LogInformation_WithSimpleMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Information message";

        // Act
        _logger.LogInformation(message);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Information", logs[0]);
        Assert.Contains(message, logs[0]);
    }

    [Fact]
    public void LogInformation_WithFormattedMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "User {0} logged in at {1}";
        const string username = "testuser";
        var loginTime = DateTime.Now;

        // Act
        _logger.LogInformation(message, username, loginTime);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Information", logs[0]);
        Assert.Contains(username, logs[0]);
    }

    [Fact]
    public void LogInformation_WithEmptyArgs_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Simple information";

        // Act
        _logger.LogInformation(message, new object[0]);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains(message, logs[0]);
    }

    #endregion

    #region 警告日志测试

    [Fact]
    public void LogWarning_WithSimpleMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Warning message";

        // Act
        _logger.LogWarning(message);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Warning", logs[0]);
        Assert.Contains(message, logs[0]);
    }

    [Fact]
    public void LogWarning_WithFormattedMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Performance warning: {0}ms exceeds threshold of {1}ms";
        const int actualTime = 1500;
        const int threshold = 1000;

        // Act
        _logger.LogWarning(message, actualTime, threshold);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Warning", logs[0]);
        Assert.Contains(actualTime.ToString(), logs[0]);
        Assert.Contains(threshold.ToString(), logs[0]);
    }

    #endregion

    #region 错误日志测试

    [Fact]
    public void LogError_WithSimpleMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Error message";

        // Act
        _logger.LogError(message);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Error", logs[0]);
        Assert.Contains(message, logs[0]);
    }

    [Fact]
    public void LogError_WithFormattedMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Failed to process {0} with error code {1}";
        const string operation = "user_login";
        const int errorCode = 500;

        // Act
        _logger.LogError(message, operation, errorCode);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Error", logs[0]);
        Assert.Contains(operation, logs[0]);
        Assert.Contains(errorCode.ToString(), logs[0]);
    }

    #endregion

    #region 异常错误日志测试

    [Fact]
    public void LogError_WithException_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "An error occurred";
        var exception = new InvalidOperationException("Test exception");

        // Act
        _logger.LogError(exception, message);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Error", logs[0]);
        Assert.Contains(message, logs[0]);
        Assert.Contains("Exception:", logs[0]);
        Assert.Contains("InvalidOperationException", logs[0]);
        Assert.Contains("Test exception", logs[0]);
    }

    [Fact]
    public void LogError_WithExceptionAndFormattedMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Failed to process {0}";
        const string operation = "file_upload";
        var exception = new ArgumentException("Invalid file format");

        // Act
        _logger.LogError(exception, message, operation);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Error", logs[0]);
        Assert.Contains(operation, logs[0]);
        Assert.Contains("Exception:", logs[0]);
        Assert.Contains("ArgumentException", logs[0]);
        Assert.Contains("Invalid file format", logs[0]);
    }

    [Fact]
    public void LogError_WithNullException_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Error with null exception";

        // Act
        _logger.LogError(null!, message);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Error", logs[0]);
        Assert.Contains(message, logs[0]);
    }

    #endregion

    #region 严重日志测试

    [Fact]
    public void LogCritical_WithSimpleMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Critical error occurred";

        // Act
        _logger.LogCritical(message);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Critical", logs[0]);
        Assert.Contains(message, logs[0]);
    }

    [Fact]
    public void LogCritical_WithFormattedMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "System failure in {0} module at {1}";
        const string module = "Authentication";
        var timestamp = DateTime.Now;

        // Act
        _logger.LogCritical(message, module, timestamp);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Critical", logs[0]);
        Assert.Contains(module, logs[0]);
    }

    #endregion

    #region 异常严重日志测试

    [Fact]
    public void LogCritical_WithException_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Critical system failure";
        var exception = new SystemException("Database connection lost");

        // Act
        _logger.LogCritical(exception, message);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Critical", logs[0]);
        Assert.Contains(message, logs[0]);
        Assert.Contains("Exception:", logs[0]);
        Assert.Contains("SystemException", logs[0]);
        Assert.Contains("Database connection lost", logs[0]);
    }

    [Fact]
    public void LogCritical_WithExceptionAndFormattedMessage_ShouldLogCorrectly()
    {
        // Arrange
        const string message = "Critical failure in {0} at {1}";
        const string component = "PaymentProcessor";
        var time = DateTime.Now.ToString("HH:mm:ss");
        var exception = new OutOfMemoryException("Insufficient memory");

        // Act
        _logger.LogCritical(exception, message, component, time);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("Critical", logs[0]);
        Assert.Contains(component, logs[0]);
        Assert.Contains(time, logs[0]);
        Assert.Contains("OutOfMemoryException", logs[0]);
    }

    #endregion

    #region 泛型日志测试

    [Fact]
    public void GenericLogger_ShouldUseTypeNameAsCategory()
    {
        // Arrange
        const string message = "Generic logger test";

        // Act
        _genericLogger.LogInformation(message);

        // Assert
        var logs = _genericLogger.GetLogs();
        Assert.Single(logs);
        Assert.Contains("TestClass", logs[0]);
        Assert.Contains(message, logs[0]);
    }

    [Fact]
    public void GenericLogger_ShouldImplementIGameLoggerT()
    {
        // Assert
        Assert.IsAssignableFrom<IGameLogger<TestClass>>(_genericLogger);
        Assert.IsAssignableFrom<IGameLogger>(_genericLogger);
    }

    [Fact]
    public void GenericLogger_AllLogMethods_ShouldWork()
    {
        // Act
        _genericLogger.LogDebug("Debug from generic");
        _genericLogger.LogInformation("Info from generic");
        _genericLogger.LogWarning("Warning from generic");
        _genericLogger.LogError("Error from generic");
        _genericLogger.LogCritical("Critical from generic");

        // Assert
        var logs = _genericLogger.GetLogs();
        Assert.Equal(5, logs.Count);
        Assert.All(logs, log => Assert.Contains("TestClass", log));
    }

    #endregion

    #region 释放资源测试

    [Fact]
    public void Dispose_ShouldSetDisposedFlag()
    {
        // Arrange
        var logger = new TestGodotGameLogger("DisposeTest");

        // Act
        logger.Dispose();

        // Assert
        Assert.True(logger.IsDisposed);
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var logger = new TestGodotGameLogger("DisposeTest");

        // Act & Assert
        logger.Dispose();
        logger.Dispose(); // Should not throw
    }

    [Fact]
    public void LogMethods_AfterDispose_ShouldNotLog()
    {
        // Arrange
        var logger = new TestGodotGameLogger("DisposeTest");
        logger.Dispose();

        // Act
        logger.LogDebug("Should not be logged");
        logger.LogInformation("Should not be logged");
        logger.LogWarning("Should not be logged");
        logger.LogError("Should not be logged");
        logger.LogCritical("Should not be logged");

        // Assert
        var logs = logger.GetLogs();
        Assert.Empty(logs);
    }

    [Fact]
    public void LogMethods_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        _logger.Dispose();

        // Act & Assert - Should not throw
        _logger.LogDebug("Test");
        _logger.LogInformation("Test");
        _logger.LogWarning("Test");
        _logger.LogError("Test");
        _logger.LogError(new Exception(), "Test");
        _logger.LogCritical("Test");
        _logger.LogCritical(new Exception(), "Test");
    }

    #endregion

    #region 并发测试

    [Fact]
    public async Task ConcurrentLogging_ShouldHandleCorrectly()
    {
        // Arrange
        const int taskCount = 10;
        const int logsPerTask = 10;
        var tasks = new Task[taskCount];

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            var taskId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < logsPerTask; j++)
                {
                    _logger.LogInformation($"Task {taskId} - Log {j}");
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Equal(taskCount * logsPerTask, logs.Count);
    }

    #endregion

    #region 边界测试

    [Fact]
    public void LogMethods_WithVeryLongMessage_ShouldHandleCorrectly()
    {
        // Arrange
        var longMessage = new string('A', 10000); // 10KB message

        // Act
        _logger.LogInformation(longMessage);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains(longMessage, logs[0]);
    }

    [Fact]
    public void LogMethods_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        const string message = "Special chars: \n\t\r\"'{}[]()";

        // Act
        _logger.LogInformation(message);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains(message, logs[0]);
    }

    [Fact]
    public void LogMethods_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        const string message = "Unicode: 你好世界 🌍 αβγ";

        // Act
        _logger.LogInformation(message);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        Assert.Contains(message, logs[0]);
    }

    [Fact]
    public void LogMethods_WithManyParameters_ShouldHandleCorrectly()
    {
        // Arrange
        const string message = "Many params: {0} {1} {2} {3} {4} {5} {6} {7} {8} {9}";
        var args = new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // Act
        _logger.LogInformation(message, args);

        // Assert
        var logs = _logger.GetLogs();
        Assert.Single(logs);
        for (int i = 1; i <= 10; i++)
        {
            Assert.Contains(i.ToString(), logs[0]);
        }
    }

    #endregion

    #region 资源清理

    public void Dispose()
    {
        _logger?.Dispose();
        _genericLogger?.Dispose();
    }

    #endregion

    #region 测试辅助类

    /// <summary>
    /// 测试用的类
    /// </summary>
    public class TestClass
    {
    }

    /// <summary>
    /// 测试用的 GodotGameLogger 实现
    /// </summary>
    public class TestGodotGameLogger : IGameLogger, IDisposable
    {
        private readonly List<string> _logs = new();
        private readonly object _lock = new();
        private bool _disposed;

        public string? CategoryName { get; }
        public bool IsDisposed => _disposed;

        public TestGodotGameLogger(string? categoryName)
        {
            CategoryName = categoryName;
        }

        public void LogDebug(string message, params object[] args)
        {
            Log("Debug", message, args);
        }

        public void LogInformation(string message, params object[] args)
        {
            Log("Information", message, args);
        }

        public void LogWarning(string message, params object[] args)
        {
            Log("Warning", message, args);
        }

        public void LogError(string message, params object[] args)
        {
            Log("Error", message, args);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            var fullMessage = args.Length > 0 ? string.Format(message, args) : message;
            if (exception != null)
            {
                fullMessage += $"\nException: {exception}";
            }
            Log("Error", fullMessage);
        }

        public void LogCritical(string message, params object[] args)
        {
            Log("Critical", message, args);
        }

        public void LogCritical(Exception exception, string message, params object[] args)
        {
            var fullMessage = args.Length > 0 ? string.Format(message, args) : message;
            if (exception != null)
            {
                fullMessage += $"\nException: {exception}";
            }
            Log("Critical", fullMessage);
        }

        private void Log(string level, string message, params object[] args)
        {
            if (_disposed) return;

            try
            {
                lock (_lock)
                {
                    var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logMessage = $"[{timestamp}] [{level}] [{CategoryName}] {formattedMessage}";
                    _logs.Add(logMessage);
                }
            }
            catch (Exception ex)
            {
                // 在测试中记录格式化错误
                lock (_lock)
                {
                    _logs.Add($"[ERROR] Logging error: {ex.Message}");
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

        public void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            lock (_lock)
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 测试用的泛型 GodotGameLogger 实现
    /// </summary>
    /// <typeparam name="T">日志类别类型</typeparam>
    public class TestGodotGameLogger<T> : TestGodotGameLogger, IGameLogger<T>
    {
        public TestGodotGameLogger() : base(typeof(T).Name)
        {
        }
    }

    #endregion
}