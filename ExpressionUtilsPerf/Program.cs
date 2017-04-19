using BenchmarkDotNet.Running;

namespace ExpressionUtilsPerf {
	class Program {
		static void Main(string[] args) {
			new BenchmarkSwitcher(typeof(Program).Assembly).Run(args);
		}
	}
}
