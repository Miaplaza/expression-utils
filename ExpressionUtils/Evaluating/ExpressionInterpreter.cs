using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MiaPlaza.ExpressionUtils.Evaluating {
	using ParameterMap = IReadOnlyDictionary<ParameterExpression, object>;

	/// <summary>
	/// An evaluator that visits the expression tree and determines every operations result via 
	/// reflection / dynamic. 
	/// </summary>
	/// <remarks>
	/// While the delegates from this technique are quite slow, the overall interpretation-process
	/// takes less time than any other method. The <see cref="ExpressionInterpreter"/> is as of
	/// now quite limited in the expressions it can evaluate, but it suffices in general.
	/// </remarks>
	public class ExpressionInterpreter : IExpressionEvaluator {
		public static ExpressionInterpreter Instance = new ExpressionInterpreter();

		private ExpressionInterpreter() { }

		private static readonly ParameterMap emptyMap = new Dictionary<ParameterExpression, object>();

		object IExpressionEvaluator.Evaluate(Expression unparametrizedExpression) => Interpret(unparametrizedExpression);
		public object Interpret(Expression exp) {
			try {
				return new ExpressionInterpretationVisitor(emptyMap).GetResultFromExpression(exp);
			} catch (Exception e) {
				throw new DynamicEvaluationException(exp, e);
			}
		}

		VariadicArrayParametersDelegate IExpressionEvaluator.EvaluateLambda(LambdaExpression lambdaExpression) => InterpretLambda(lambdaExpression);
		public VariadicArrayParametersDelegate InterpretLambda(LambdaExpression lambda) => args => {
			var parameters = new Dictionary<ParameterExpression, object>();
			
			for (int i = 0; i < lambda.Parameters.Count; ++i) {
				parameters[lambda.Parameters[i]] = args[i];
			}

			try {
				return new ExpressionInterpretationVisitor(parameters).GetResultFromExpression(lambda.Body);
			} catch (Exception e) {
				throw new DynamicEvaluationException(lambda, e);
			}
		};

		class ExpressionInterpretationVisitor : ExpressionResultVisitor<object> {
			readonly ParameterMap parameters;

			public ExpressionInterpretationVisitor(ParameterMap parameters) {
				this.parameters = parameters;
			}

			public override object GetResultFromExpression(Expression exp) {
				if (exp == null) {
					return null;
				}
				return base.GetResultFromExpression(exp);
			}

			protected override object GetResultFromBinary(BinaryExpression exp) {
				dynamic left = GetResultFromExpression(exp.Left);
				if (exp.Method != null) {
					return exp.Method.Invoke(null, new[] { left, GetResultFromExpression(exp.Right) });
				} else {
					switch (exp.NodeType) {
						case ExpressionType.AndAlso:
							return (bool)left && (bool)GetResultFromExpression(exp.Right);
						case ExpressionType.OrElse:
							return (bool)left || (bool)GetResultFromExpression(exp.Right);
						case ExpressionType.Coalesce:
							return left ?? GetResultFromExpression(exp.Right);
						default:
							break;
					}
					dynamic right = GetResultFromExpression(exp.Right);

					switch (exp.NodeType) {
						case ExpressionType.Add:
						case ExpressionType.AddChecked:
							return left + right;
						case ExpressionType.Subtract:
						case ExpressionType.SubtractChecked:
							return left - right;
						case ExpressionType.Multiply:
						case ExpressionType.MultiplyChecked:
							return left * right;
						case ExpressionType.Divide:
							return left / right;
						case ExpressionType.Modulo:
							return left % right;
						case ExpressionType.ExclusiveOr:
							return left ^ right;
						case ExpressionType.And:
							return left & right;
						case ExpressionType.Or:
							return left | right;
						case ExpressionType.LessThan:
							return left < right;
						case ExpressionType.LessThanOrEqual:
							return left <= right;
						case ExpressionType.Equal:
							return left == right;
						case ExpressionType.NotEqual:
							return left != right;
						case ExpressionType.GreaterThanOrEqual:
							return left >= right;
						case ExpressionType.GreaterThan:
							return left > right;
						case ExpressionType.LeftShift:
							return left << right;
						case ExpressionType.RightShift:
							return left >> right;
						default:
							throw new NotImplementedException(exp.NodeType.ToString());
					}
				}
			}

			protected override object GetResultFromBlock(BlockExpression exp) {
				throw new NotSupportedException("Not supported by expressions as of now.");
			}

			protected override object GetResultFromCatchBlock(CatchBlock exp) {
				throw new NotImplementedException();
			}

			protected override object GetResultFromConditional(ConditionalExpression exp) {
				if ((bool)GetResultFromExpression(exp.Test)) {
					return GetResultFromExpression(exp.IfTrue);
				} else {
					return GetResultFromExpression(exp.IfFalse);
				}
			}

			protected override object GetResultFromConstant(ConstantExpression exp) => exp.Value;

			protected override object GetResultFromDebugInfo(DebugInfoExpression exp) {
				throw new NotImplementedException("Never encountered any of these.");
			}

			protected override object GetResultFromDefault(DefaultExpression exp) {
				throw new NotImplementedException("Should never be necessary in expressions.");
			}

			protected override object GetResultFromDynamic(DynamicExpression exp) {
				throw new NotSupportedException("No need for dynamic in expressions.");
			}

			protected override object GetResultFromElementInit(ElementInit exp) {
				throw new NotImplementedException();
			}

			protected override object GetResultFromExtension(Expression exp) {
				throw new NotSupportedException("Not supported by expressions as of now.");
			}

			protected override object GetResultFromGoto(GotoExpression exp) {
				throw new NotSupportedException("Not supported by expressions as of now.");
			}

			protected override object GetResultFromIndex(IndexExpression exp) {
				var obj = GetResultFromExpression(exp.Object);

				if (exp.Indexer != null) {
					var indices = exp.Arguments
						.Select(a => GetResultFromExpression(a))
						.ToArray();
					return exp.Indexer.GetValue(obj, indices);
				} else if (obj is Array) {
					if (exp.Arguments[0].Type == typeof(long)) { // All array access indices must have same type!
						var indices = exp.Arguments
							.Select(a => (long)GetResultFromExpression(a))
							.ToArray();

						return ((Array)obj).GetValue(indices);
					} else { // .. and it's either long or int.
						var indices = exp.Arguments
							.Select(a => (int)GetResultFromExpression(a))
							.ToArray();

						return ((Array)obj).GetValue(indices);
					}
				} else {
					throw new NotSupportedException("Unknown index-access!");
				}
			}

			protected override object GetResultFromInvocation(InvocationExpression exp) {
				throw new NotImplementedException("Never encountered any of these.");
			}

			protected override object GetResultFromLabel(LabelExpression exp) {
				throw new NotSupportedException("Not supported by expressions as of now.");
			}

			protected override object GetResultFromLabelTarget(LabelTarget exp) {
				throw new NotImplementedException();
			}

			protected override object GetResultFromLambda<D>(Expression<D> exp) {
				return exp;
			}

			protected override object GetResultFromListInit(ListInitExpression exp) {
				throw new NotImplementedException("Never encountered any of these.");
			}

			protected override object GetResultFromLoop(LoopExpression exp) {
				throw new NotSupportedException("Not supported by expressions as of now.");
			}

			protected override object GetResultFromMember(MemberExpression exp) {
				var obj = GetResultFromExpression(exp.Expression);

				if (exp.Member is PropertyInfo) {
					return ((PropertyInfo)exp.Member).GetValue(obj);
				} else if (exp.Member is FieldInfo) {
					return ((FieldInfo)exp.Member).GetValue(obj);
				} else {
					throw new InvalidOperationException("There are only fields and properties");
				}
			}

			protected override object GetResultFromMemberInit(MemberInitExpression exp) {
				throw new NotImplementedException("Never encountered any of these.");
			}

			protected override object GetResultFromMethodCall(MethodCallExpression exp) {
				var obj = GetResultFromExpression(exp.Object);

				var arguments = exp.Arguments
					.Select(a => GetResultFromExpression(a))
					.ToArray();

				return exp.Method.Invoke(obj, arguments);
			}

			protected override object GetResultFromNew(NewExpression exp) {
				var args = exp.Arguments
					.Select(a => GetResultFromExpression(a))
					.ToArray();

				return exp.Constructor.Invoke(args);
			}

			protected override object GetResultFromNewArray(NewArrayExpression exp) {
				var array = Array.CreateInstance(exp.Type.GetElementType(), exp.Expressions.Count);

				for (int i = 0; i < array.Length; ++i) {
					array.SetValue(value: GetResultFromExpression(exp.Expressions[i]), index: i);
				}

				return array;
			}

			protected override object GetResultFromParameter(ParameterExpression exp) => parameters[exp];

			protected override object GetResultFromRuntimeVariables(RuntimeVariablesExpression exp) {
				throw new NotImplementedException("Never encountered any of these.");
			}

			protected override object GetResultFromSwitch(SwitchExpression exp) {
				throw new NotSupportedException("Not supported by expressions as of now.");
			}

			protected override object GetResultFromSwitchCase(SwitchCase exp) {
				throw new NotImplementedException();
			}

			protected override object GetResultFromTry(TryExpression exp) {
				throw new NotSupportedException("Not supported by expressions as of now.");
			}

			protected override object GetResultFromTypeBinary(TypeBinaryExpression exp) {
				throw new NotImplementedException("Never encountered any of these.");
			}

			protected override object GetResultFromUnary(UnaryExpression exp) {
				dynamic op = GetResultFromExpression(exp.Operand);
				if (exp.Method != null) {
					return exp.Method.Invoke(null, new[] { op });
				} else {
					switch (exp.NodeType) {
						case ExpressionType.Not:
							return !op;
						case ExpressionType.Negate:
							return -op;
						case ExpressionType.OnesComplement:
							return ~op;
						case ExpressionType.UnaryPlus:
							return +op;
						case ExpressionType.ConvertChecked:
							return convert(op, exp.Type);
						case ExpressionType.Convert:
							return uncheckedConvert(op, exp.Type);
						case ExpressionType.TypeAs: {
								if (exp.Type.IsAssignableFrom(exp.Operand.Type)) {
									return exp.Operand;
								} else {
									return null;
								}
							}
						default:
							throw new NotImplementedException(exp.NodeType.ToString());
					}
				}
			}
			
			static readonly Predicate<Type> numericTypes = new HashSet<Type>() {
				typeof(byte),
				typeof(sbyte),
				typeof(ushort),
				typeof(short),
				typeof(uint),
				typeof(int),
				typeof(ulong),
				typeof(long),
			}.Contains;

			static readonly Predicate<Type> unsignedTypes = new HashSet<Type>() {
				typeof(byte),
				typeof(ushort),
				typeof(uint),
				typeof(ulong),
			}.Contains;

			static readonly IReadOnlyDictionary<Type, dynamic> maxValues = new Dictionary<Type, dynamic>() {
				{ typeof(byte), byte.MaxValue },
				{ typeof(sbyte), sbyte.MaxValue },
				{ typeof(ushort), ushort.MaxValue },
				{ typeof(short), short.MaxValue },
				{ typeof(uint), uint.MaxValue },
				{ typeof(int), int.MaxValue },
				{ typeof(ulong), ulong.MaxValue },
				{ typeof(long), long.MaxValue },
			};

			private static object uncheckedConvert(dynamic original, Type target) {
				if (original == null) {
					if (target.IsValueType && Nullable.GetUnderlyingType(target) == null) {
						throw new InvalidCastException($"Cannot assign null to {target.FullName}.");
					}

					return null;
				}

				if (numericTypes(original.GetType()) && numericTypes(target)) {
					dynamic maxValue;
					if (maxValues.TryGetValue(target, out maxValue)) {
						if (original > maxValue) {
							original %= maxValue + 1;
						} else if (unsignedTypes(target)) {
							if (original < 0) {
								original %= maxValue + 1;
								//.net modulo returns negative results for negative input, applying 
								// the mathematical mod operation on the absolute value.
								original += maxValue + 1;
							}
						}
					}
				}
				return convert(original, target);
			}

			private static object convert(object original, Type target) {
				if (original == null) {
					if (target.IsValueType && Nullable.GetUnderlyingType(target) == null) {
						throw new InvalidCastException($"Cannot assign null to {target.FullName}.");
					}

					return null;
				}

				if (Nullable.GetUnderlyingType(target) != null) {
					original = convert(original, Nullable.GetUnderlyingType(target));
				}

				if (target.IsAssignableFrom(original.GetType())) {
					return original;
				} else {
					try {
						return Convert.ChangeType(original, target);
					} catch (OverflowException ex) {
						throw new OverflowException($"Value: {original}", ex);
					}
				}
			}
		}
	}
}
