using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MiaPlaza.ExpressionUtils.Evaluating {
	class DynamicEvaluationException : InvalidOperationException {
		public readonly Expression FaultyExpression;

		public DynamicEvaluationException(Expression exp, Exception inner)
			: base("An error occured during dynamic evaluation of an expression", inner) {
			FaultyExpression = exp;
		}
	}
}
