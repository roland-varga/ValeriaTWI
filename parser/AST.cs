namespace Valeria.Parser;
public interface IASTNode { }


public class ExpressionStatement : IASTNode {
    public IASTNode Expression { get; }
    public ExpressionStatement(IASTNode expr) {
        Expression = expr;
    }
}

// A function declaration: `name : fn(a,b) = { … }`
public class FunctionDeclaration : IASTNode {
    public string Name { get; }
    public List<string> Parameters { get; }
    public Block Body { get; }

    public FunctionDeclaration(string name, List<string> parameters, Block body) {
        Name = name;
        Parameters = parameters;
        Body = body;
    }
}

// A function call: `add(34,35)`
public class CallExpression : IASTNode {
    public IASTNode Callee { get; }
    public List<IASTNode> Arguments { get; }

    public CallExpression(IASTNode callee, List<IASTNode> args) {
        Callee = callee;
        Arguments = args;
    }
}

// A return statement inside a function body
public class ReturnStatement : IASTNode {
    public IASTNode Expression { get; }
    public ReturnStatement(IASTNode expr) {
        Expression = expr;
    }
}

public class NullExpression : IASTNode { }

public class ArrayExpression(List<IASTNode> elements) : IASTNode {
    public List<IASTNode> Elements = elements;

}

public class IndexExpression(IASTNode target, IASTNode index) : IASTNode {
    public IASTNode Target = target;
    public IASTNode Index = index;
}

public class NumberExpression(double number) : IASTNode {
    public double Number = number;

    public override string ToString() {
        return $"({Number.ToString()})";
    }
}

public class StringExpression(string @string) : IASTNode {
    public string String = @string;

    public override string ToString() {
        return $"(`{@string}`)";
    }
}

public class PrintCall(IASTNode node) : IASTNode {
    public IASTNode Expression = node;

    public override string ToString() {
        return $"<print{Expression.ToString()}>";
    }
}

public class BinExpression(IASTNode left, string opr, IASTNode right) : IASTNode {

    public IASTNode Left = left;
    public string Opr = opr;
    public IASTNode Right = right;

    public override string ToString() {
        return $"[{Left.ToString()}{Opr}{Right.ToString()}]";
    }

}

public class IdentExpression(string name) : IASTNode {
    public string Name = name;

    public override string ToString() {
        return Name;
    }
}

public class VariableExpression(string name) : IASTNode {
    private string _name = name;
    public string Name { get => name; set => name = value; }

    public override string ToString() {
        return name;
    }
}

public class AssignExpression(string varName, IASTNode expression) : IASTNode {
    public string VarName = varName;
    public IASTNode Expression = expression;

    public override string ToString() {
        return $"{VarName} = {Expression}";
    }
}

public class LetExpression(string name, IASTNode value) : IASTNode {
    public string Name = name;
    public IASTNode Value = value;

    public override string ToString() {
        return $"({Name} := {Value})";
    }
}

public class Block(List<IASTNode> statements) : IASTNode {
    public List<IASTNode> Statements = statements;
}

public class IfStmt(IASTNode condition, List<IASTNode> thenBranch, List<IASTNode>? elseBranch = null) : IASTNode {
    public IASTNode Condition = condition;
    public List<IASTNode> ThenBranch = thenBranch;
    public List<IASTNode>? ElseBranch = elseBranch;

    public override string ToString() {
        string thenStr = string.Join("; ", ThenBranch.Select(e => e.ToString()));
        string elseStr = ElseBranch != null
            ? " else { " + string.Join("; ", ElseBranch.Select(e => e.ToString())) + " }"
            : "";

        return $"if ({Condition}) {{ {thenStr} }}{elseStr}";
    }
}

public class WhileLoop(IASTNode condition, List<IASTNode> body) : IASTNode {
    public IASTNode Condition = condition;
    public List<IASTNode> Body = body;

}

public class ForRangeLoop(string varName, IASTNode start, IASTNode end, List<IASTNode> body) : IASTNode {
    public string VarName = varName;
    public IASTNode Start = start;
    public IASTNode End = end;
    public List<IASTNode> Body = body;
}
