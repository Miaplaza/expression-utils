using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;

namespace ExpressionUtilsPerf {
	class Program {
		static void Main(string[] args) {
			new BenchmarkSwitcher(typeof(Program).Assembly).Run(args, ManualConfig.Create(DefaultConfig.Instance)
				.With(RPlotExporter.Default)
				.With(CsvMeasurementsExporter.Default));
		}
	}
}
