using Xunit;
using Xunit.Abstractions;

namespace Nullean.PrettyLogger.Tests;

public class UnitTest1
{
	private readonly ITestOutputHelper _output;

	public UnitTest1(ITestOutputHelper output)
	{
		_output = output;
		_output.WriteLine("output from constructor");
	}

	[Fact(Skip = "Skipped for no reason!")]
	public void Test5() => Assert.True(false);

	[Fact]
	public async Task Test4()
	{
		_output.WriteLine("Passing test's output");
		await Task.Delay(TimeSpan.FromSeconds(5));
		Assert.True(true);
	}

	[Fact]
	public void Test1()
	{
		_output.WriteLine("Making sure messages are preserved");
		throw new Exception("BOOM");
	}

	[Fact]
	public void Test2()
	{
		_output.WriteLine("line1");
		_output.WriteLine("line2");
		_output.WriteLine("line3");
		_output.WriteLine("line4");
		_output.WriteLine("line5");
		_output.WriteLine("line6");
		throw new Exception("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.");
	}

	[Fact]
	public void Test3()
	{
		_output.WriteLine("Making sure messages are preserved");
		throw new Exception($"Lorem ipsum dolor sit amet, {Environment.NewLine}consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. {Environment.NewLine}Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea {Environment.NewLine}commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse {Environment.NewLine}cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.");
	}
}
