# MF.Infrastructure.Tests

This project contains unit tests for the MF.Infrastructure components.

## Running the Tests

To run the tests, you can use the following commands:

```bash
# Navigate to the test project directory
cd ModularGodot.Framework\Core\3_UniTest\MF.Infrastructure.Tests

# Run all tests
dotnet test

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests and generate a coverage report (requires coverlet.msbuild package)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=../../Coverage/
```

## Test Structure

- `Caching` - Tests for caching services
  - `MemoryCacheServiceTests.cs` - Unit tests using mocks
  - `MemoryCacheServiceIntegrationTests.cs` - Integration tests using real MemoryCache

## Test Frameworks

- xUnit.net - Testing framework
- Moq - Mocking framework
- Microsoft.NET.Test.Sdk - Test SDK
- coverlet.collector - Code coverage collector

## Writing New Tests

1. Create a new test class in the appropriate folder
2. Follow the naming convention: `[ClassName]Tests.cs`
3. Use Arrange-Act-Assert pattern
4. Add the test class to the project file if necessary