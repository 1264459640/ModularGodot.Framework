using MediatR;
using MF.Services.Abstractions;
using Godot;

namespace MF.CQRS.Commands;

public class TestDependencyCommandHandler(IDependencyTestService dependencyTestService)
    : IRequestHandler<TestDependencyCommand>
{
    public Task Handle(TestDependencyCommand request, CancellationToken cancellationToken)
    {
        GD.Print("Dependency Test");
        dependencyTestService.Test();
        return Task.CompletedTask;
    }
}