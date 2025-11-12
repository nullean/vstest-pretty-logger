using Nullean.Xunit.Partitions;
using Nullean.Xunit.Partitions.Sdk;
using Xunit;

[assembly: TestFramework(Partition.TestFramework, Partition.Assembly)]

namespace Nullean.Partition.Tests;

public class PartitionState : IPartitionLifetime
{
	public Task InitializeAsync() => throw new Exception("adasdsd");

	public Task DisposeAsync() => Task.CompletedTask;

	public string FailureTestOutput() => "This output should be included";

	public int? MaxConcurrency => null;
}

public class Tests : IPartitionFixture<PartitionState>
{
	private readonly PartitionState _state;

	public Tests(PartitionState state) => _state = state;

	[Fact]
	public void OneIsNotTwo() => Assert.Equal(1, 2);
}
