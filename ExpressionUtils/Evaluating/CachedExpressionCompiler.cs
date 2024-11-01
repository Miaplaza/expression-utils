using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ExpressionUtilsTest")]


namespace MiaPlaza.ExpressionUtils.Evaluating {
	/// <summary>
	/// A cache for expression compilation. For compilations for structural identical <see cref="ExpressionComparing"/> expressions
	/// this speeds up the compilations significantly (but slows the executions slightly) and avoids the memory leak created when
	/// calling <see cref="LambdaExpression.Compile"/> repeatedly.
	/// </summary>
	/// <remarks>
	/// The result from <see cref="LambdaExpression"/> compilation usually cannot be cached when it contains captured variables 
	/// or constants (closures) that should be replaced in later calls. This cache first extracts all constants from the expression
	/// and will then look up the normalized (constant free) expression in a compile cache. The constants then get re-inserted into 
	/// the result via a closure and a delegate capturing the actual parameters of the original expression is returned.
	/// </remarks>
	public class CachedExpressionCompiler : IExpressionEvaluator {
		static ConcurrentDictionary<LambdaParts, ParameterListDelegate> delegates = new ConcurrentDictionary<LambdaParts, ParameterListDelegate>(new LambdaPartsComparer());

		public static readonly CachedExpressionCompiler Instance = new CachedExpressionCompiler();

		private CachedExpressionCompiler() { }

		public VariadicArrayParametersDelegate EvaluateLambda(LambdaExpression lambdaExpression) => CachedCompileLambda(lambdaExpression);

		public VariadicArrayParametersDelegate CachedCompileLambda(LambdaParts lambda) {
			IReadOnlyList<object> constants;

			ParameterListDelegate compiled;
			if (delegates.TryGetValue(lambda, out compiled)) {
				constants = ConstantExtractor.ExtractConstantsOnly(lambda.Body);
			} else {
				var extractionResult = ConstantExtractor.ExtractConstants(lambda.Body);

				compiled = ParameterListRewriter.RewriteLambda(
					Expression.Lambda(
						extractionResult.ConstantfreeExpression.Body,
						extractionResult.ConstantfreeExpression.Parameters.Concat(lambda.Parameters)))
						.Compile();

				var key = getClosureFreeKeyForCaching(extractionResult, lambda.Parameters);

				delegates.TryAdd(key, compiled);
				constants = extractionResult.ExtractedConstants;
			}

			return args => compiled(constants.Concat(args).ToArray());
		}

		public object Evaluate(Expression unparametrizedExpression) => CachedCompile(unparametrizedExpression);
		public object CachedCompile(Expression unparametrizedExpression) => CachedCompileLambda(new LambdaParts { Body = unparametrizedExpression, Parameters = Array.Empty<ParameterExpression>() })();

		public DELEGATE EvaluateTypedLambda<DELEGATE>(Expression<DELEGATE> expression) where DELEGATE : class => CachedCompileTypedLambda(expression);
		public DELEGATE CachedCompileTypedLambda<DELEGATE>(Expression<DELEGATE> expression) where DELEGATE : class => CachedCompileLambda(expression).WrapDelegate<DELEGATE>();


		/// <summary>
		/// Creates a closure-free key for caching, represented as a tuple of the expression body and parameter collection.
		/// Can be used with the <see cref="LambdaPartsComparer" /> to compare to the original lambda expression.
		/// </summary>
		private LambdaParts getClosureFreeKeyForCaching(ConstantExtractor.ExtractionResult extractionResult, IReadOnlyCollection<ParameterExpression> parameterExpressions) {
			var e = SimpleParameterSubstituter.SubstituteParameter(extractionResult.ConstantfreeExpression,
				extractionResult.ConstantfreeExpression.Parameters.Select(
						p => (Expression) Expression.Constant(getDefaultValue(p.Type), p.Type)));

			return new LambdaParts { Body = e, Parameters = parameterExpressions };
		}

		private static object getDefaultValue(Type t) {
			if (t.IsValueType) {
				return Activator.CreateInstance(t);
			}

			return null;
		}
		
		/// <remarks>
		/// Use for testing only.
		/// </remarks>
		internal bool IsCached(LambdaExpression lambda) {
			return delegates.ContainsKey(lambda);
		}

		/// <summary>
		/// Previously as a key for <see cref="delegates"/> we were using <see cref="LambdaExpression"/> objects and <see cref="ExpressionComparing.StructuralComparer"/> as a comparer.
		/// But that required us to make calls to <see cref="Expression.Lambda(Expression, ParameterExpression[])"/> which contains global lock inside, and that started to become a problem.
		/// So instead we now use <see cref="LambdaParts"/> as a key and pass body and parameters separately, to reduce number of calls to <see cref="Expression.Lambda(Expression, ParameterExpression[])"/>.
		/// </summary>
		private class LambdaPartsComparer : IEqualityComparer<LambdaParts> {
			private IEqualityComparer<Expression> expressionComparer = new ExpressionComparing.StructuralComparer(ignoreConstantsValues: true);

			public bool Equals(LambdaParts x, LambdaParts y) {
				return x.Body.Type == y.Body.Type
					&& x.Parameters.SequenceEqualOrBothNull(y.Parameters, expressionComparer.Equals)
					&& expressionComparer.Equals(x.Body, y.Body);
			}

			public int GetHashCode(LambdaParts obj) {
				var hash = Hashing.FnvOffset;
				Hashing.Hash(ref hash, obj.Body.Type.GetHashCode());
				Hashing.Hash(ref hash, expressionComparer.GetHashCode(obj.Body));
				return hash;
			}
		}
	}
}
