using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("ExpressionUtilsTest")]

namespace MiaPlaza.ExpressionUtils.Evaluating {
	/// <summary>
	/// An evaluator that simply compiles expressions to MSIL-code and executes them. 
	/// </summary>
	/// <remarks>
	/// Uses the .Net Frameworks internal runtime compiler. It will usually work, but will also create a memory leak,
	/// since the compiled delegates are not garbage-collected and cannot be freed manually either. 
	/// </remarks>
	internal class ExpressionCompiler : IExpressionEvaluator {
		public static readonly IExpressionEvaluator Instance = new ExpressionCompiler();

		ExpressionCompiler() { }

		public object Evaluate(Expression unparametrizedExpression)
			=> EvaluateLambda(Expression.Lambda(unparametrizedExpression))();
		public VariadicArrayParametersDelegate EvaluateLambda(LambdaExpression lambdaExpression)
			=> lambdaExpression.Compile().DynamicInvoke;
		public D EvaluateTypedLambda<D>(Expression<D> lambdaExpression) where D : class
			=> lambdaExpression.Compile();
	}
}
