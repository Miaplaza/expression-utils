using System.Linq.Expressions;

namespace MiaPlaza.ExpressionUtils {
	public abstract class ExpressionResultVisitor<T> {
		sealed class DispatchVisitor : ExpressionVisitor {
			readonly ExpressionResultVisitor<T> resultVisitor;

			public T Result;

			public DispatchVisitor(ExpressionResultVisitor<T> resultVisitor) {
				this.resultVisitor = resultVisitor;
			}

			protected sealed override Expression VisitBinary(BinaryExpression node) {
				Result = resultVisitor.GetResultFromBinary(node);
				return node;
			}

			protected sealed override Expression VisitBlock(BlockExpression node) {
				Result = resultVisitor.GetResultFromBlock(node);
				return node;
			}

			protected sealed override CatchBlock VisitCatchBlock(CatchBlock node) {
				Result = resultVisitor.GetResultFromCatchBlock(node);
				return node;
			}

			protected sealed override Expression VisitConditional(ConditionalExpression node) {
				Result = resultVisitor.GetResultFromConditional(node);
				return node;
			}

			protected sealed override Expression VisitConstant(ConstantExpression node) {
				Result = resultVisitor.GetResultFromConstant(node);
				return node;
			}

			protected sealed override Expression VisitDebugInfo(DebugInfoExpression node) {
				Result = resultVisitor.GetResultFromDebugInfo(node);
				return node;
			}

			protected sealed override Expression VisitDefault(DefaultExpression node) {
				Result = resultVisitor.GetResultFromDefault(node);
				return node;
			}

			protected sealed override Expression VisitDynamic(DynamicExpression node) {
				Result = resultVisitor.GetResultFromDynamic(node);
				return node;
			}

			protected sealed override ElementInit VisitElementInit(ElementInit node) {
				Result = resultVisitor.GetResultFromElementInit(node);
				return node;
			}

			protected sealed override Expression VisitExtension(Expression node) {
				Result = resultVisitor.GetResultFromExtension(node);
				return node;
			}

			protected sealed override Expression VisitGoto(GotoExpression node) {
				Result = resultVisitor.GetResultFromGoto(node);
				return node;
			}

			protected sealed override Expression VisitIndex(IndexExpression node) {
				Result = resultVisitor.GetResultFromIndex(node);
				return node;
			}

			protected sealed override Expression VisitInvocation(InvocationExpression node) {
				Result = resultVisitor.GetResultFromInvocation(node);
				return node;
			}

			protected sealed override Expression VisitLabel(LabelExpression node) {
				Result = resultVisitor.GetResultFromLabel(node);
				return node;
			}

			protected sealed override LabelTarget VisitLabelTarget(LabelTarget node) {
				Result = resultVisitor.GetResultFromLabelTarget(node);
				return node;
			}

			protected sealed override Expression VisitLambda<D>(Expression<D> node) {
				Result = resultVisitor.GetResultFromLambda<D>(node);
				return node;
			}

			protected sealed override Expression VisitListInit(ListInitExpression node) {
				Result = resultVisitor.GetResultFromListInit(node);
				return node;
			}

			protected sealed override Expression VisitLoop(LoopExpression node) {
				Result = resultVisitor.GetResultFromLoop(node);
				return node;
			}

			protected sealed override Expression VisitMember(MemberExpression node) {
				Result = resultVisitor.GetResultFromMember(node);
				return node;
			}

			protected sealed override Expression VisitMemberInit(MemberInitExpression node) {
				Result = resultVisitor.GetResultFromMemberInit(node);
				return node;
			}

			protected sealed override Expression VisitMethodCall(MethodCallExpression node) {
				Result = resultVisitor.GetResultFromMethodCall(node);
				return node;
			}

			protected sealed override Expression VisitNew(NewExpression node) {
				Result = resultVisitor.GetResultFromNew(node);
				return node;
			}

			protected sealed override Expression VisitNewArray(NewArrayExpression node) {
				Result = resultVisitor.GetResultFromNewArray(node);
				return node;
			}

			protected sealed override Expression VisitParameter(ParameterExpression node) {
				Result = resultVisitor.GetResultFromParameter(node);
				return node;
			}

			protected sealed override Expression VisitRuntimeVariables(RuntimeVariablesExpression node) {
				Result = resultVisitor.GetResultFromRuntimeVariables(node);
				return node;
			}

			protected sealed override Expression VisitSwitch(SwitchExpression node) {
				Result = resultVisitor.GetResultFromSwitch(node);
				return node;
			}

			protected sealed override SwitchCase VisitSwitchCase(SwitchCase node) {
				Result = resultVisitor.GetResultFromSwitchCase(node);
				return node;
			}

			protected sealed override Expression VisitTry(TryExpression node) {
				Result = resultVisitor.GetResultFromTry(node);
				return node;
			}

			protected sealed override Expression VisitTypeBinary(TypeBinaryExpression node) {
				Result = resultVisitor.GetResultFromTypeBinary(node);
				return node;
			}

			protected sealed override Expression VisitUnary(UnaryExpression node) {
				Result = resultVisitor.GetResultFromUnary(node);
				return node;
			}
		}

		readonly DispatchVisitor visitor;

		public ExpressionResultVisitor() {
			visitor = new DispatchVisitor(this);
		}

		public virtual T GetResultFromExpression(Expression expression) {
			visitor.Visit(expression);
			return visitor.Result;
		}

		protected abstract T GetResultFromBinary(BinaryExpression node);

		protected abstract T GetResultFromBlock(BlockExpression node);

		protected abstract T GetResultFromCatchBlock(CatchBlock node);

		protected abstract T GetResultFromConditional(ConditionalExpression node);

		protected abstract T GetResultFromConstant(ConstantExpression node);

		protected abstract T GetResultFromDebugInfo(DebugInfoExpression node);

		protected abstract T GetResultFromDefault(DefaultExpression node);

		protected abstract T GetResultFromDynamic(DynamicExpression node);

		protected abstract T GetResultFromElementInit(ElementInit node);

		protected abstract T GetResultFromExtension(Expression node);

		protected abstract T GetResultFromGoto(GotoExpression node);

		protected abstract T GetResultFromIndex(IndexExpression node);

		protected abstract T GetResultFromInvocation(InvocationExpression node);

		protected abstract T GetResultFromLabel(LabelExpression node);

		protected abstract T GetResultFromLabelTarget(LabelTarget node);

		protected abstract T GetResultFromLambda<D>(Expression<D> node);

		protected abstract T GetResultFromListInit(ListInitExpression node);

		protected abstract T GetResultFromLoop(LoopExpression node);

		protected abstract T GetResultFromMember(MemberExpression node);
		
		protected abstract T GetResultFromMemberInit(MemberInitExpression node);
		
		protected abstract T GetResultFromMethodCall(MethodCallExpression node);

		protected abstract T GetResultFromNew(NewExpression node);

		protected abstract T GetResultFromNewArray(NewArrayExpression node);

		protected abstract T GetResultFromParameter(ParameterExpression node);

		protected abstract T GetResultFromRuntimeVariables(RuntimeVariablesExpression node);

		protected abstract T GetResultFromSwitch(SwitchExpression node);

		protected abstract T GetResultFromSwitchCase(SwitchCase node);

		protected abstract T GetResultFromTry(TryExpression node);

		protected abstract T GetResultFromTypeBinary(TypeBinaryExpression node);

		protected abstract T GetResultFromUnary(UnaryExpression node);
	}
}
