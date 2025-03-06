using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils {
	/// <summary>
	/// Rewrites a <see cref="LambdaExpression"/> in form of its body and parameters to accept a single parameter <see cref="IReadOnlyList{T}"/> instead of multiple parameters, making handling of the compiled delegates much easier.
	/// </summary>
	internal class ParameterListRewriter : ExpressionVisitor {
		public static Expression<ParameterListDelegate> RewriteLambda(Expression body, IReadOnlyCollection<ParameterExpression> parameters) {
			var visitor = new ParameterListRewriter(parameters);
			var rewrittenBody = visitor.Visit(body);
			return Expression.Lambda<ParameterListDelegate>(
				body: Expression.Convert(rewrittenBody, typeof(object)),
				parameters: visitor.parameter);
		}

		private ParameterListRewriter(IEnumerable<ParameterExpression> parameters) {
			var indexMap = new Dictionary<ParameterExpression, int>();

			int i = 0;
			foreach (var parameter in parameters) {
				indexMap[parameter] = i++;
			}

			this.indexMap = indexMap;
		}

		private readonly IReadOnlyDictionary<ParameterExpression, int> indexMap;
		private readonly ParameterExpression parameter = Expression.Parameter(typeof(IReadOnlyList<object>));

		private static readonly PropertyInfo listIndexer = typeof(IReadOnlyList<object>).GetProperty("Item");

		protected override Expression VisitParameter(ParameterExpression originalNode) {
			int index;
			if (!indexMap.TryGetValue(originalNode, out index)) {
				return originalNode;
			}

			return Expression.Convert(
				Expression.MakeIndex(
					instance: parameter,
					indexer: listIndexer,
					arguments: new[] { Expression.Constant(indexMap[originalNode]) }),
				type: originalNode.Type);
		}
	}
}
