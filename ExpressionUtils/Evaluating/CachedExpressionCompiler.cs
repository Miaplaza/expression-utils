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
		static ConcurrentDictionary<Expression, ParameterListDelegate> delegates = new ConcurrentDictionary<Expression, ParameterListDelegate>(new ExpressionComparing.StructuralComparer(ignoreConstantsValues: true));

		public static readonly CachedExpressionCompiler Instance = new CachedExpressionCompiler();

		private CachedExpressionCompiler() { }

		public VariadicArrayParametersDelegate EvaluateLambda(LambdaExpression lambdaExpression) => CachedCompileExpression(lambdaExpression);

		public VariadicArrayParametersDelegate CachedCompileExpression(Expression expression)
		{
			var expressionParts = 
				expression is LambdaExpression lambda ? 
					new { lambda.Body,lambda.Parameters }
					: new { Body = expression, Parameters = Array.Empty<ParameterExpression>().ToList().AsReadOnly() };

			IReadOnlyList<object> constants;
			ParameterListDelegate compiled;

			if (delegates.TryGetValue(expression, out compiled))
			{
				constants = ConstantExtractor.ExtractConstantsOnly(expressionParts.Body);
			} else {
				var extractionResult = ConstantExtractor.ExtractConstants(expressionParts.Body);

				compiled = ParameterListRewriter.RewriteLambda(extractionResult.ConstantfreeExpression, extractionResult.Parameters.Concat(expressionParts.Parameters).ToList()).Compile();

				delegates.TryAdd(expression, compiled);
				constants = extractionResult.ExtractedConstants;
			}

			return args => compiled(constants.Concat(args).ToArray());
		}

		public object Evaluate(Expression unparametrizedExpression) => CachedCompileExpression(unparametrizedExpression)();
		public DELEGATE EvaluateTypedLambda<DELEGATE>(Expression<DELEGATE> expression) where DELEGATE : class => CachedCompileTypedLambda(expression);
		public DELEGATE CachedCompileTypedLambda<DELEGATE>(Expression<DELEGATE> expression) where DELEGATE : class => CachedCompileExpression(expression).WrapDelegate<DELEGATE>();
		
		/// <remarks>
		/// Use for testing only.
		/// </remarks>
		internal bool IsCached(Expression body) {
			return delegates.ContainsKey(body);
		}

		/// <remarks>
		/// Use for testing only.
		/// </remarks>
		internal void ClearCache() {
			delegates = new ConcurrentDictionary<Expression, ParameterListDelegate>(new ExpressionComparing.StructuralComparer(ignoreConstantsValues: true));
		}
	}
}
