using BenchmarkDotNet.Attributes;
using MiaPlaza.ExpressionUtils;
using MiaPlaza.ExpressionUtils.Evaluating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ExpressionUtilsPerf {
	/// <summary>
	/// Determines the performance of different approaches to evaluate an expression abstract syntax tree.
	/// </summary>
	public class ExpressionEvaluation {
		static Expression<Func<int, bool>> expression = i => i == 0 || (i < 43 && i > 12 && (i % 3) == 2 && i != 15);
		
		Func<int, bool> compiledTypedDelegate;
		VariadicArrayParametersDelegate compiledDelegate;

		Func<int, bool> cachedTypedCompiledDelegate;
		VariadicArrayParametersDelegate cachedCompiledDelegate;

		Func<int, bool> interpretationTypedDelegate;
		VariadicArrayParametersDelegate interpretationDelegate;

		[Setup]
		public void Init() {
			compiledTypedDelegate = TypedCompilation();
			compiledDelegate = UntypedCompilation();
			
			cachedTypedCompiledDelegate = CachedTypedCompilation();
			cachedCompiledDelegate = UntypedCachedCompilation();

			interpretationTypedDelegate = TypedInterpretation();
			interpretationDelegate = UntypedInterpretation();
		}

		public int Value => 14;

		[Benchmark]
		public Func<int, bool> TypedCompilation() => ExpressionCompiler.Instance.EvaluateTypedLambda(expression);
		
		[Benchmark]
		public bool TypedCompiledExecution() => compiledTypedDelegate(Value);

		[Benchmark]
		public VariadicArrayParametersDelegate UntypedCompilation() => ExpressionCompiler.Instance.EvaluateLambda(expression);

		[Benchmark]
		public object UntypedCompiledExecution() => compiledDelegate(Value);
		

		[Benchmark]
		public Func<int, bool> CachedTypedCompilation() => CachedExpressionCompiler.Instance.CachedCompileTypedLambda(expression);

		[Benchmark]
		public bool CachedTypedCompiledExecution() => cachedTypedCompiledDelegate(Value);

		[Benchmark]
		public VariadicArrayParametersDelegate UntypedCachedCompilation() => CachedExpressionCompiler.Instance.CachedCompileLambda(expression);

		[Benchmark]
		public object UntypedCachedCompiledExecution() => cachedCompiledDelegate(Value);
		

		[Benchmark]
		public Func<int, bool> TypedInterpretation() => ExpressionInterpreter.Instance.InterpretTypedLambda(expression);

		[Benchmark]
		public bool TypedInterpretationExecution() => interpretationTypedDelegate(Value);

		[Benchmark]
		public VariadicArrayParametersDelegate UntypedInterpretation() => ExpressionInterpreter.Instance.InterpretLambda(expression);

		[Benchmark(Baseline = true)]
		public object UntypedInterpretationExecution() => interpretationDelegate(Value);
	}
}
