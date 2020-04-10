using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace MiaPlaza.ExpressionUtils {
	/// <summary>
	/// Tools to help comparing expressions
	/// </summary>
	public static class ExpressionComparing {
		class ExpressionStructureComparisonException : InvalidOperationException {
			public ExpressionStructureComparisonException(Expression a, Expression b, Exception inner)
				: base($"Could not compare '{a}' and '{b}'", inner) { }
		}

		/// <summary>
		/// A visitor that computes the hash of an expression, optionally looking only to a certain depth.
		/// </summary>
		class HashCodeVisitor : ExpressionVisitor {
			public readonly int? MaxDepth;
			public readonly bool IgnoreConstants;

			public int ResultHash = Hashing.FnvOffset;
			private int currentDepth = 0;

			public HashCodeVisitor(bool ignoreConstants, int? maxDepth) {
				MaxDepth = maxDepth;
				IgnoreConstants = ignoreConstants;
			}

			public override Expression Visit(Expression node) {
				if (node == null) {
					return null;
				}

				Hashing.Hash(ref ResultHash, (int)node.NodeType);

				currentDepth++;

				if (MaxDepth == null || MaxDepth > currentDepth) {
					base.Visit(node);
				}

				Hashing.Hash(ref ResultHash, -1);
				currentDepth--;

				return node;
			}

			protected override Expression VisitConstant(ConstantExpression node) {
				if (IgnoreConstants) {
					Hashing.Hash(ref ResultHash, -1);
				} else {
					Hashing.Hash(ref ResultHash, node.Value?.GetHashCode() ?? -1);
				}
				return base.VisitConstant(node);
			}

			protected override Expression VisitMember(MemberExpression node) {
				Hashing.Hash(ref ResultHash, node.Member.GetHashCode());
				return base.VisitMember(node);
			}

			protected override Expression VisitLambda<T>(Expression<T> node) {
				Hashing.Hash(ref ResultHash, node.ReturnType.GetHashCode());
				return base.VisitLambda<T>(node);
			}

			protected override Expression VisitParameter(ParameterExpression node) {
				Hashing.Hash(ref ResultHash, node.Type.GetHashCode());
				return base.VisitParameter(node);
			}

			protected override Expression VisitMethodCall(MethodCallExpression node) {
				Hashing.Hash(ref ResultHash, node.Method.GetHashCode());
				return base.VisitMethodCall(node);
			}
		}

		/// <summary>
		/// A visitor that compares two expressions with each other.
		/// </summary>
		class ExpressionComparingVisitor : ExpressionResultVisitor<bool> {
			public readonly bool IgnoreConstantsValues;
			Expression other;

			public ExpressionComparingVisitor(Expression other, bool ignoreConstantValues) {
				this.other = other;
				IgnoreConstantsValues = ignoreConstantValues;
			}

			protected bool Compare(Expression a, Expression b) {
				other = b;
				return GetResultFromExpression(a);
			}

			public override bool GetResultFromExpression(Expression expression) {
#if DEBUG
				if (expression == null && other == null) {
					return true;
				}
#else
				if (ReferenceEquals(expression, other)) {
					return true;
				}
#endif

				if (expression == null || other == null) {
					return false;
				}

				if (expression.NodeType != other.NodeType) {
					return false;
				}

				if (expression.Type != other.Type) {
					return false;
				}

				return base.GetResultFromExpression(expression);
			}

			protected override bool GetResultFromBinary(BinaryExpression a) {
				var b = (BinaryExpression)other;
				return a.IsLifted == b.IsLifted
					&& a.IsLiftedToNull == b.IsLiftedToNull
					&& a.Method == b.Method
					&& Compare(a.Left, b.Left)
					&& Compare(a.Right, b.Right)
					&& Compare(a.Conversion, b.Conversion);
			}
			protected override bool GetResultFromBlock(BlockExpression a) { throw new NotImplementedException(); }
			protected override bool GetResultFromCatchBlock(CatchBlock a) { throw new NotImplementedException(); }
			protected override bool GetResultFromConditional(ConditionalExpression a) {
				var b = (ConditionalExpression)other;
				return Compare(a.Test, b.Test)
					&& Compare(a.IfTrue, b.IfTrue)
					&& Compare(a.IfFalse, b.IfFalse);
			}
			protected override bool GetResultFromConstant(ConstantExpression a) {
				var b = (ConstantExpression)other;
				return IgnoreConstantsValues || Equals(a.Value, b.Value);
			}
			protected override bool GetResultFromDebugInfo(DebugInfoExpression a) { throw new NotImplementedException(); }
			protected override bool GetResultFromDefault(DefaultExpression a) => true;
			protected override bool GetResultFromDynamic(DynamicExpression a) { throw new NotImplementedException(); }
			protected override bool GetResultFromElementInit(ElementInit a) { throw new NotImplementedException(); }
			protected override bool GetResultFromExtension(Expression a) { throw new NotImplementedException(); }
			protected override bool GetResultFromGoto(GotoExpression a) { throw new NotImplementedException(); }
			protected override bool GetResultFromIndex(IndexExpression a) {
				var b = (IndexExpression)other;
				return a.Indexer == b.Indexer
					&& Compare(a.Object, b.Object)
					&& a.Arguments.SequenceEqualOrBothNull(b.Arguments, Compare);
			}
			protected override bool GetResultFromInvocation(InvocationExpression a) {
				var b = (InvocationExpression)other;
				return a.Expression == b.Expression
					&& a.Arguments.SequenceEqualOrBothNull(b.Arguments, Compare);
			}
			protected override bool GetResultFromLabel(LabelExpression a) { throw new NotImplementedException(); }
			protected override bool GetResultFromLabelTarget(LabelTarget a) { throw new NotImplementedException(); }
			protected override bool GetResultFromLambda<D>(Expression<D> a) {
				var b = (Expression<D>)other;
				return a.ReturnType == b.ReturnType
					&& a.Parameters.SequenceEqualOrBothNull(b.Parameters, Compare)
					&& Compare(a.Body, b.Body);
			}
			protected override bool GetResultFromListInit(ListInitExpression a) {
				var b = (ListInitExpression)other;
				return Compare(a.NewExpression, b.NewExpression)
					&& a.Initializers.SequenceEqualOrBothNull(b.Initializers, (ia, ib) =>
						ia.AddMethod == ib.AddMethod
						&& ia.Arguments.SequenceEqualOrBothNull(ib.Arguments, Compare));
			}
			protected override bool GetResultFromLoop(LoopExpression a) { throw new NotImplementedException(); }
			protected override bool GetResultFromMember(MemberExpression a) {
				var b = (MemberExpression)other;
				return a.Member == b.Member
					&& Compare(a.Expression, b.Expression);
			}
			protected override bool GetResultFromMemberInit(MemberInitExpression a) {
				var b = (MemberInitExpression)other;
				return a.Bindings.SequenceEqualOrBothNull(b.Bindings, (ba, bb) =>
					ba.BindingType == bb.BindingType
					&& ba.Member == bb.Member)
					&& a.NewExpression == b.NewExpression;
			}
			protected override bool GetResultFromMethodCall(MethodCallExpression a) {
				var b = (MethodCallExpression)other;
				return a.Method == b.Method
					&& Compare(a.Object, b.Object)
					&& a.Arguments.SequenceEqualOrBothNull(b.Arguments, Compare);
			}
			protected override bool GetResultFromNew(NewExpression a) {
				var b = (NewExpression)other;
				return a.Constructor == b.Constructor
					&& a.Members.SequenceEqualOrBothNull(b.Members)
					&& a.Arguments.SequenceEqualOrBothNull(b.Arguments, Compare);
			}
			protected override bool GetResultFromNewArray(NewArrayExpression a) {
				var b = (NewArrayExpression)other;
				return a.Expressions.SequenceEqualOrBothNull(b.Expressions, Compare);
			}
			protected override bool GetResultFromParameter(ParameterExpression a) {
				var b = (ParameterExpression)other;
				return a.IsByRef == b.IsByRef
					&& a.Name == b.Name;
			}
			protected override bool GetResultFromRuntimeVariables(RuntimeVariablesExpression a) { throw new NotImplementedException(); }
			protected override bool GetResultFromSwitch(SwitchExpression a) { throw new NotImplementedException(); }
			protected override bool GetResultFromSwitchCase(SwitchCase a) { throw new NotImplementedException(); }
			protected override bool GetResultFromTry(TryExpression a) { throw new NotImplementedException(); }
			protected override bool GetResultFromTypeBinary(TypeBinaryExpression a) {
				var b = (TypeBinaryExpression)other;
				return a.TypeOperand == b.TypeOperand
					&& Compare(a.Expression, b.Expression);
			}
			protected override bool GetResultFromUnary(UnaryExpression a) {
				var b = (UnaryExpression)other;
				return a.IsLifted == b.IsLifted
					&& a.IsLiftedToNull == b.IsLiftedToNull
					&& a.Method == b.Method
					&& Compare(a.Operand, b.Operand);
			}
		}

		public sealed class StructuralComparer : IEqualityComparer<Expression> {
			public readonly int? HashCodeExpressionDepth;
			public readonly bool IgnoreConstantsValues;

			public StructuralComparer(bool ignoreConstantsValues = false, int? hashCodeExpressionDepth = 5) {
				IgnoreConstantsValues = ignoreConstantsValues;
				HashCodeExpressionDepth = hashCodeExpressionDepth;
			}

			public int GetHashCode(Expression tree) => GetNodeTypeStructureHashCode(tree, IgnoreConstantsValues, HashCodeExpressionDepth);

			bool IEqualityComparer<Expression>.Equals(Expression x, Expression y) => StructuralIdentical(x, y, IgnoreConstantsValues);
		}

		/// <summary>
		/// Visits the first <paramref name="hashCodeExpressionDepth"/> layers of the expression tree and calculates a hash code based on the <see cref="ExpressionType"/>s there.
		/// </summary>
		/// <param name="tree"></param>
		/// <param name="hashCodeExpressionDepth"></param>
		/// <returns></returns>
		public static int GetNodeTypeStructureHashCode(this Expression tree, bool ignoreConstantsValues = false, int? hashCodeExpressionDepth = null) {
			var visitor = new HashCodeVisitor(ignoreConstantsValues, hashCodeExpressionDepth);
			visitor.Visit(tree);
			return visitor.ResultHash;
		}

		/// <summary>
		/// Compares the structure (kind and order of operations, operands recursively) of expressions. Basically does a recursive member equality check
		/// </summary>
		public static bool StructuralIdentical(this Expression x, Expression y, bool ignoreConstantValues = false) {
			try {
				return new ExpressionComparingVisitor(y, ignoreConstantValues).GetResultFromExpression(x);
			} catch (Exception ex) {
				throw new ExpressionStructureComparisonException(x, y, ex);
			}
		}
	}
}
