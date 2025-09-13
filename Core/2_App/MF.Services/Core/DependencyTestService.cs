using Godot;
using MF.Services.Abstractions;
using TestPlugin;

namespace MF.Services.Core
{
    public class DependencyTestService : IDependencyTestService
    {
        private readonly ITestService _testService;

        public DependencyTestService(ITestService testService)
        {
            _testService = testService;
        }

        public void Test()
        {
            GD.Print("Test");
            _testService.Print();
        }
    }
}