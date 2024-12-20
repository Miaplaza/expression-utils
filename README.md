# Processing, Compilation, and Execution of Expression Trees at Runtime

C# provides the ability to write lambda expressions such as `n => n / 2`. The
right hand of such an expression can be processed at runtime. This project provides tools for such processing.

## Features

* Comparison of expressions based on their structure
* Fast evaluation of expression trees without leaking memory
    * Interpretation
    * Cached Compilation
* Partial Evaluation
* Combining expressions via inlining
* Extraction of constants
* Substitution of parameters

## NuGet

`ExpressionUtils` is distributed via NuGet to make it easier to embed into your project. The package name is '[MiaPlaza.ExpressionUtils](https://www.nuget.org/packages/MiaPlaza.ExpressionUtils)'.

## Examples

Some motivating examples to show the library functions:

### Comparison

```csharp
Expression<Func<int, bool>> isEvenExpression = x => x % 2 == 0;
Expression<Func<int, bool>> isEvenExpression2 = x => x % 2 == 0;

Console.Write(isEvenExpression == isEvenExpression2); // false
Console.Write(isEvenExpression.StructuralIdentical(isEvenExpression2)); // true

Expression<Func<int, bool>> isUnevenExpression = x => x % 2 == 1;

Console.Write(isEvenExpression.StructuralIdentical(isUnevenExpression, ignoreConstantValues: true)); // true
```

### Evaluation

```csharp
Expression<Func<int, bool>> isEvenExpression = x => x % 2 == 0;

var isEvenDelegate = CachedExpressionCompiler.Instance.CachedCompileLambda(isEvenExpression);

Console.Write(isEvenDelegate(5)); // false
```

### Partial Evaluation

This collapses all subtrees to constants that are independent from the expression's parameters. This also removes 
closures and references.

```csharp
int modul = 0;
Expression<Func<int, bool>> modulExpression = x => x % modul == 0;

modul = 2;
var isMultipleOfTwoExpression = PartialEvaluator.PartialEval(
    modulExpression,
    ExpressionInterpreter.Instance);

Expression<Func<int, bool>> isEvenExpression = x => x % 2 == 0;

Console.Write(isEvenExpression.StructuralIdentical(modulExpression)); // false
Console.Write(isEvenExpression.StructuralIdentical(isMultipleOfTwoExpression)); // true

modul = 3;
var isMultipleOfThreeExpression = PartialEvaluator.PartialEval(
    modulExpression,
    ExpressionInterpreter.Instance);

Console.Write(isEvenExpression.StructuralIdentical(isMultipleOfThreeExpression)); // false
```

## Maintainer's Notice

To create a new Release:

* bump the version numbers in the project file
* create a Release here on github
* `dotnet pack -c Release`
* `cd ExpressionUtils/bin/Release/`
* Create an API key for **miaplaza** at https://www.nuget.org/account/apikeys
* `dotnet nuget push MiaPlaza.ExpressionUtils.….nupgk -k YOUR_KEY_HERE -s -s https://api.nuget.org/v3/index.json`
