using BenchmarkDotNet.Attributes;
using MiaPlaza.ExpressionUtils;
using MiaPlaza.ExpressionUtils.Evaluating;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionUtilsPerf {
	/// <summary>
	/// Determines the performance of different approaches to evaluate an expression abstract syntax tree.
	/// </summary>
	public class ExpressionEvaluation {

		static readonly Expression<Func<int, bool>>[] expressions = {
			// Tiny expression without branching
			i => i > 5,
			// Big expression with short-circuits
			i => i == 0 || (i < 43 && i > 12 && (i % 3) == 2 && i != 15)
		};

		static readonly Func<int, bool>[] compiledDelegates = expressions
			.Select(e => e.Compile())
			.ToArray();

		static readonly VariadicArrayParametersDelegate[] cachedCompiledDelegates = expressions
			.Select(CachedExpressionCompiler.Instance.CachedCompileLambda)
			.ToArray();

		static readonly VariadicArrayParametersDelegate[] interpretationDelegates = expressions
			.Select(ExpressionInterpreter.Instance.InterpretLambda)
			.ToArray();

		[Params(0, 1)]
		public int ExpressionIndex { get; set; }

		[Params(0, 14)]
		public int Value { get; set; }

		[Benchmark]
		public Delegate Compilation() {
			return expressions[ExpressionIndex].Compile();
		}

		[Benchmark]
		public bool CompiledExecution() {
			return compiledDelegates[ExpressionIndex](Value);
		}

		[Benchmark]
		public Delegate CachedCompilation() {
			return CachedExpressionCompiler.Instance.CachedCompileLambda(expressions[ExpressionIndex]);
		}

		[Benchmark]
		public object CachedCompiledExecution() {
			return cachedCompiledDelegates[ExpressionIndex](Value);
		}

		[Benchmark]
		public Delegate Interpretation() {
			return ExpressionInterpreter.Instance.InterpretLambda(expressions[ExpressionIndex]);
		}

		[Benchmark(Baseline = true)]
		public object InterpretationExecution() {
			return interpretationDelegates[ExpressionIndex](Value);
		}
	}
}
