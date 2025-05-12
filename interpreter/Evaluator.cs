using System.Text.RegularExpressions;
using Valeria.Parser;

namespace Valeria.Interpreter;

public class Evaluator {

    Stack<Dictionary<string, object>> _scopes = new();
    Dictionary<string, object> CurrentScope => _scopes.Peek();

    public Evaluator() {
        var global = new Dictionary<string, object> {
            { "true", 1.0 },
            { "false", 0.0 }
        };
        _scopes.Push(global);
    }

    public object Evaluate(IASTNode expr) => expr switch {
        FunctionDeclaration fd => EvaluateFunctionDeclaration(fd),
        CallExpression call => EvaluateCall(call),
        ReturnStatement ret => throw new ReturnException(Evaluate(ret.Expression)),
        NumberExpression num => num.Number,
        NullExpression => null!,
        StringExpression str => EvaluateString(str),
        Block block => EvaluateBlock(block),
        LetExpression let => EvaluateLet(let),
        BinExpression bin => EvaluateBinary(bin),
        PrintCall trace => EvaluatePrint(trace),
        IdentExpression ident => EvaluateIdent(ident),
        AssignExpression assign => EvaluateAssign(assign),
        IfStmt @if => EvaluateIf(@if),
        WhileLoop loop => EvaluateWhile(loop),
        IndexExpression index => EvaluateIndex(index),
        ArrayExpression array => EvaluateArray(array),
        ForRangeLoop forLoop => EvaluateForRange(forLoop),
        ExpressionStatement exprStmt => Evaluate(exprStmt.Expression),
        _ => throw new Exception("Unknown expression type")
    };

    object EvaluateFunctionDeclaration(FunctionDeclaration fd) {
        // capture the function in the current scope
        CurrentScope[fd.Name] = fd;
        return null!;
    }

    // custom exception to unwind to the call site
    public class ReturnException : Exception {
        public object Value { get; }
        public ReturnException(object value) {
            Value = value;
        }
    }

    object EvaluateCall(CallExpression call) {
        // resolve the function object
        var funcObj = Evaluate(call.Callee);
        if (funcObj is FunctionDeclaration fd) {
            // evaluate arguments
            var argValues = call.Arguments.Select(Evaluate).ToList();

            // new call scope
            var callScope = new Dictionary<string, object>();
            for (int i = 0; i < fd.Parameters.Count; i++) {
                callScope[fd.Parameters[i]] = argValues[i];
            }
            _scopes.Push(callScope);
            try {
                // execute body
                Evaluate(fd.Body);
                return null!; // if no return, returns null
            }
            catch (ReturnException ret) {
                return ret.Value;
            }
            finally {
                _scopes.Pop();
            }
        }
        throw new Exception("Attempt to call a non-function");
    }


    object EvaluateArray(ArrayExpression array) {
        var result = new List<object>();
        foreach (var elem in array.Elements) {
            result.Add(Evaluate(elem));
        }
        return result;
    }

    object EvaluateIndex(IndexExpression indexExpr) {
        var target = Evaluate(indexExpr.Target);
        var idx = ExpectNumber(Evaluate(indexExpr.Index));

        if (target is List<object> list) {
            int i = (int)idx;
            if (i < 0 || i >= list.Count)
                throw new Exception("Array index out of bounds");
            return list[i];
        }

        throw new Exception("Cannot index non-array value");
    }

    object EvaluateString(StringExpression strExpr) {
        string raw = strExpr.String;

        string result = Regex.Replace(raw, @"\$\{(\w+)(?:\[(\w+)\])?\}", match => {
            string varName = match.Groups[1].Value;
            string? indexOrNull = match.Groups[2].Success ? match.Groups[2].Value : null;

            foreach (var scope in _scopes) {
                if (scope.TryGetValue(varName, out var value)) {
                    if (indexOrNull != null) {
                        if (value is List<object> list) {
                            object indexVal;
                            // Try to parse as number first
                            if (int.TryParse(indexOrNull, out int literalIndex)) {
                                indexVal = literalIndex;
                            }
                            else {
                                // Look up variable index
                                indexVal = _scopes
                                    .Select(s => s.TryGetValue(indexOrNull, out var v) ? v : null)
                                    .FirstOrDefault(v => v != null)
                                    ?? throw new Exception($"Undefined index variable: {indexOrNull}");
                            }

                            int idx = (int)ExpectNumber(indexVal);
                            if (idx < 0 || idx >= list.Count)
                                throw new Exception($"Index {idx} out of bounds for variable '{varName}'");
                            return list[idx]?.ToString() ?? "nil";
                        }
                        else {
                            throw new Exception($"Variable '{varName}' is not an array");
                        }
                    }
                    else {
                        return value?.ToString() ?? "nil";
                    }
                }
            }

            throw new Exception($"Undefined variable in string interpolation: {varName}");
        });

        return result;
    }



    object EvaluateWhile(WhileLoop loop) {
        while (ExpectNumber(Evaluate(loop.Condition)) != 0) {
            foreach (var stmt in loop.Body) {
                Evaluate(stmt);
            }
        }
        return 0.0;
    }

    object EvaluateForRange(ForRangeLoop forLoop) {
        double start = ExpectNumber(Evaluate(forLoop.Start));
        double end = ExpectNumber(Evaluate(forLoop.End));

        for (double i = start; i < end; i++) {
            var iterationScope = new Dictionary<string, object>();
            iterationScope[forLoop.VarName] = i;

            _scopes.Push(iterationScope);

            foreach (var stmt in forLoop.Body) {
                Evaluate(stmt);
            }

            _scopes.Pop(); // Pop the iteration scope
        }

        return 0.0;
    }

    object EvaluateBlock(Block block) {
        _scopes.Push(new Dictionary<string, object>()); // New scope
        object? result = null;

        foreach (var stmt in block.Statements) {
            result = Evaluate(stmt); // can store result if needed
        }

        _scopes.Pop(); // Exit scope
        return result!;
    }

    object EvaluatePrint(PrintCall print) {
        object result = Evaluate(print.Expression);
        Console.WriteLine(result);
        return result;
    }

    object EvaluateAssign(AssignExpression assign) {
        foreach (var scope in _scopes) {
            if (scope.ContainsKey(assign.VarName)) {
                object value = Evaluate(assign.Expression);
                scope[assign.VarName] = value;
                return value;
            }
        }
        throw new Exception($"Variable '{assign.VarName}' is not declared");
    }

    object EvaluateIdent(IdentExpression ident) {
        foreach (var scope in _scopes) {
            if (scope.TryGetValue(ident.Name, out var value)) {
                return value;
            }
        }
        throw new Exception($"Undefined variable: {ident.Name}");
    }


    object EvaluateLet(LetExpression let) {
        if (let.Name == "true" || let.Name == "false")
            throw new Exception($"Cannot redefine built-in variable '{let.Name}'");

        if (CurrentScope.ContainsKey(let.Name))
            throw new Exception($"Variable '{let.Name}' already declared in this scope");

        object value = Evaluate(let.Value);
        CurrentScope[let.Name] = value;
        return value;
    }

    double ExpectNumber(object value) {
        if (value is double num)
            return num;
        throw new Exception($"Expected number, got {value.GetType().Name}");
    }

    object EvaluateIf(IfStmt ifExpr) {
        var conditionValue = Evaluate(ifExpr.Condition);
        double conditionNumber = ExpectNumber(conditionValue);

        if (conditionNumber != 0) {
            foreach (var statement in ifExpr.ThenBranch)
                Evaluate(statement);
        }
        else if (ifExpr.ElseBranch != null) {
            foreach (var statement in ifExpr.ElseBranch)
                Evaluate(statement);
        }
        return 0.0;
    }

    object EvaluateBinary(BinExpression bin) {
        object leftObj = Evaluate(bin.Left);
        object rightObj = Evaluate(bin.Right);
        double left = ExpectNumber(leftObj);
        double right = ExpectNumber(rightObj);

        return bin.Opr switch {
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" => left / right,
            "==" => left == right ? 1.0 : 0.0,
            "!=" => left != right ? 1.0 : 0.0,
            ">" => left > right ? 1.0 : 0.0,
            "<" => left < right ? 1.0 : 0.0,
            _ => throw new Exception($"Unknown operator {bin.Opr}")
        };
    }
}