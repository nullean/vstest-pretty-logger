using System;
using System.Threading.Tasks;
using Xunit;

namespace Nullean.VsTest.Pretty.TestLogger.Example
{
	public class UnitTest1
	{
		[Fact]
		public void PassingTest() { }

		[Fact]
		public void FailingTest() => Creating();

		[Fact(Skip = "I don't feel like running this test")]
		public void SkipTest() => throw new Exception("boom!");

		[Fact]
		public async Task SlowTest() => await Task.Delay(TimeSpan.FromSeconds(3));

		private void Creating() => An();
		private void An() => Artifical();
		private void Artifical() => Somewhat();
		private void Somewhat() => Interesting();
		private void Interesting() => StackTrance();
		private void StackTrance() => throw new Exception("boom!");

	}
}
