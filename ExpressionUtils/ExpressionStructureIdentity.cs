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
		public sealed class StructuralComparer : IEqualityComparer<Expression> {
			public readonly int HashCodeExpressionDepth;
			public readonly bool IgnoreConstantsValues;

			public StructuralComparer(bool ignoreConstantsValues = false, int hashCodeExpressionDepth = 5) {
				IgnoreConstantsValues = ignoreConstantsValues;
				HashCodeExpressionDepth = hashCodeExpressionDepth;
			}
			
			int IEqualityComparer<Expression>.GetHashCode(Expression tree) => 
				GetNodeTypeStructureHashCode(tree, IgnoreConstantsValues, HashCodeExpressionDepth);
			
			bool IEqualityComparer<Expression>.Equals(Expression x, Expression y) => x.StructuralIdentical(y, IgnoreConstantsValues);
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

		class ExpressionStructureComparisonException : InvalidOperationException {
			public ExpressionStructureComparisonException(Expression a, Expression b, Exception inner)
				: base($"Could not compare '{a}' and '{b}'", inner) { }
		}

		/// <summary>
		/// Compares the structure (kind and order of operations, operands recursively) of expressions. Basically does a recursive member equality check
		/// </summary>
		public static bool StructuralIdentical(this Expression a, Expression b, bool ignoreConstantValues = false) {
			if (ReferenceEquals(a, b)) {
				return true;
			}

			if (a == null || b == null) {
				return false;
			}

			try {
				if (a.NodeType != b.NodeType
					|| a.Type != b.Type) {
					return false;
				}

				// The DLR seems not to be able to correctly determine the precise type of lambda expressions
				// (although it would be good enough to just handle them as LambdaExpressions).
				// Theferore, we keep them out of dynamic dispatch. See 9047.
				if (a is LambdaExpression) {
					return compareNodeDetails(a as LambdaExpression, b as LambdaExpression, ignoreConstantValues);
				} else {
					return compareNodeDetails(a as dynamic, b as dynamic, ignoreConstantValues);
				}
			} catch (Exception ex) {
				throw new ExpressionStructureComparisonException(a, b, ex);
			}
		}

		private static bool compareNodeDetails(BinaryExpression a, BinaryExpression b, bool ignoreConstantValues) {
			return a.IsLifted == b.IsLifted
				&& a.IsLiftedToNull == b.IsLiftedToNull
				&& a.Method == b.Method
				&& a.Left.StructuralIdentical(b.Left, ignoreConstantValues)
				&& a.Right.StructuralIdentical(b.Right, ignoreConstantValues)
				&& a.Conversion.StructuralIdentical(b.Conversion, ignoreConstantValues);
		}

		private static bool compareNodeDetails(BlockExpression a, BlockExpression b, bool ignoreConstantValues) {
			throw new NotSupportedException();
		}

		private static bool compareNodeDetails(ConditionalExpression a, ConditionalExpression b, bool ignoreConstantValues) {
			return a.Test.StructuralIdentical(b.Test, ignoreConstantValues)
				&& a.IfTrue.StructuralIdentical(b.IfTrue, ignoreConstantValues)
				&& a.IfFalse.StructuralIdentical(b.IfFalse, ignoreConstantValues);
		}

		private static bool compareNodeDetails(ConstantExpression a, ConstantExpression b, bool ignoreConstantValues) {
			return object.Equals(a.Value, b.Value);
		}

		private static bool compareNodeDetails(DebugInfoExpression a, DebugInfoExpression b, bool ignoreConstantValues) {
			throw new NotSupportedException();
		}

		private static bool compareNodeDetails(DefaultExpression a, DefaultExpression b, bool ignoreConstantValues) {
			return true;
		}

		private static bool compareNodeDetails(DynamicExpression a, DynamicExpression b, bool ignoreConstantValues) {
			throw new NotSupportedException();
		}

		private static bool compareNodeDetails(GotoExpression a, GotoExpression b, bool ignoreConstantValues) {
			throw new NotSupportedException();
		}

		private static bool compareNodeDetails(IndexExpression a, IndexExpression b, bool ignoreConstantValues) {
			return a.Indexer == b.Indexer
				&& a.Object.StructuralIdentical(b.Object, ignoreConstantValues)
				&& a.Arguments.SequenceEqualOrBothNull(b.Arguments, (x, y) => StructuralIdentical(x, y, ignoreConstantValues));
		}

		private static bool compareNodeDetails(InvocationExpression a, InvocationExpression b, bool ignoreConstantValues) {
			return a.Expression == b.Expression
				&& a.Arguments.SequenceEqualOrBothNull(b.Arguments, (x, y) => StructuralIdentical(x, y, ignoreConstantValues));
		}

		private static bool compareNodeDetails(LabelExpression a, LabelExpression b, bool ignoreConstantValues) {
			throw new NotSupportedException();
		}

		private static bool compareNodeDetails(LambdaExpression a, LambdaExpression b, bool ignoreConstantValues) {
			return a.ReturnType == b.ReturnType
				&& a.Parameters.SequenceEqualOrBothNull(b.Parameters, (x, y) => StructuralIdentical(x, y, ignoreConstantValues))
				&& a.Body.StructuralIdentical(b.Body, ignoreConstantValues);
		}

		private static bool compareNodeDetails(ListInitExpression a, ListInitExpression b, bool ignoreConstantValues) {
			return a.NewExpression.StructuralIdentical(b.NewExpression, ignoreConstantValues)
				&& a.Initializers.SequenceEqualOrBothNull(b.Initializers, (ia, ib) =>
					ia.AddMethod == ib.AddMethod
					&& ia.Arguments.SequenceEqualOrBothNull(ib.Arguments, (x, y) => StructuralIdentical(x, y, ignoreConstantValues)));
		}

		private static bool compareNodeDetails(LoopExpression a, LoopExpression b, bool ignoreConstantValues) {
			throw new NotSupportedException();
		}

		private static bool compareNodeDetails(MemberExpression a, MemberExpression b, bool ignoreConstantValues) {
			return a.Member == b.Member
				&& a.Expression.StructuralIdentical(b.Expression, ignoreConstantValues);
		}

		private static bool compareNodeDetails(MemberInitExpression a, MemberInitExpression b, bool ignoreConstantValues) {
			return a.Bindings.SequenceEqualOrBothNull(b.Bindings, (ba, bb) =>
				ba.BindingType == bb.BindingType
				&& ba.Member == bb.Member)
				&& a.NewExpression == b.NewExpression;
		}

		private static bool compareNodeDetails(MethodCallExpression a, MethodCallExpression b, bool ignoreConstantValues) {
			return a.Method == b.Method
				&& a.Object.StructuralIdentical(b.Object, ignoreConstantValues)
				&& a.Arguments.SequenceEqualOrBothNull(b.Arguments, (x, y) => StructuralIdentical(x, y, ignoreConstantValues));
		}

		private static bool compareNodeDetails(NewArrayExpression a, NewArrayExpression b, bool ignoreConstantValues) {
			return a.Expressions.SequenceEqualOrBothNull(b.Expressions, (x, y) => StructuralIdentical(x, y, ignoreConstantValues));
		}

		private static bool compareNodeDetails(NewExpression a, NewExpression b, bool ignoreConstantValues) {
			return a.Constructor == b.Constructor
				&& a.Members.SequenceEqualOrBothNull(b.Members)
				&& a.Arguments.SequenceEqualOrBothNull(b.Arguments, (x, y) => StructuralIdentical(x, y, ignoreConstantValues));
		}

		private static bool compareNodeDetails(ParameterExpression a, ParameterExpression b, bool ignoreConstantValues) {
			return a.IsByRef == b.IsByRef
				&& a.Name == b.Name;
		}

		private static bool compareNodeDetails(RuntimeVariablesExpression a, RuntimeVariablesExpression b, bool ignoreConstantValues) {
			throw new NotSupportedException();
		}

		private static bool compareNodeDetails(SwitchExpression a, SwitchExpression b, bool ignoreConstantValues) {
			throw new NotSupportedException();
		}

		private static bool compareNodeDetails(TryExpression a, TryExpression b, bool ignoreConstantValues) {
			throw new NotSupportedException();
		}

		private static bool compareNodeDetails(TypeBinaryExpression a, TypeBinaryExpression b, bool ignoreConstantValues) {
			return a.TypeOperand == b.TypeOperand
				&& a.Expression.StructuralIdentical(b.Expression, ignoreConstantValues);
		}

		private static bool compareNodeDetails(UnaryExpression a, UnaryExpression b, bool ignoreConstantValues) {
			return a.IsLifted == b.IsLifted
				&& a.IsLiftedToNull == b.IsLiftedToNull
				&& a.Method == b.Method
				&& a.Operand.StructuralIdentical(b.Operand, ignoreConstantValues);
		}
	}
}
