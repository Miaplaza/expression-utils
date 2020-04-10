using System.Linq.Expressions;
using MiaPlaza.ExpressionUtils;
using MiaPlaza.ExpressionUtils.Evaluating;
using NUnit.Framework;

namespace MiaPlaza.Test.ExpressionUtilsTest {
	public class CachedExpressionCompilerTestEvaluator : IExpressionEvaluator {

		public object Evaluate(Expression unparametrizedExpression) {
			var lambda = Expression.Lambda(unparametrizedExpression);
			return this.EvaluateLambda(lambda)();
		}

		public VariadicArrayParametersDelegate EvaluateLambda(LambdaExpression lambdaExpression) {
			var result = ((IExpressionEvaluator)CachedExpressionCompiler.Instance).EvaluateLambda(lambdaExpression);
			Assert.IsTrue(CachedExpressionCompiler.Instance.IsCached(lambdaExpression));
			return result;
		}

		public DELEGATE EvaluateTypedLambda<DELEGATE>(Expression<DELEGATE> expression) where DELEGATE : class {

			var result = this.EvaluateLambda(expression);
			return result.WrapDelegate<DELEGATE>();
		}
	}
}
