using Valeria.Lexer;

namespace Valeria.Parser;
public class Parser {

    List<IASTNode> _program = new();
    List<Token> _tokens;
    int _index = 0;

    public Parser(List<Token> tokens) {
        _tokens = tokens;
    }

    public List<IASTNode> ParseProgram() {
        while (_index < _tokens.Count) {
            if (_tokens[_index].Type == TokenType.PRINT) {
                _program.Add(ParsePrint());
            }
            else if (Expect(_index, TokenType.IDENT) && Expect(_index + 1, TokenType.COLON) && Expect(_index + 2, TokenType.FN)) {
                _program.Add(ParseFunctionDeclaration());
            }
            else if (Expect(_index, TokenType.IF)) {
                _program.Add(ParseIf());
            }
            else if (Expect(_index, TokenType.LBRACE)) {
                _program.Add(ParseBlock());
            }
            else if (Expect(_index, TokenType.FOR)) {
                _program.Add(ParseFor());
            }
            else if (Expect(_index, TokenType.IDENT) && Expect(_index + 1, TokenType.ASSIGN_COLON)) {
                _program.Add(ParseLet());
            }
            else if (Expect(_index, TokenType.IDENT) && Expect(_index + 1, TokenType.EQUAL)) {
                _program.Add(ParseAssignment());
            }
            // Add this new condition for top-level function calls
            else if (Expect(_index, TokenType.IDENT) && Expect(_index + 1, TokenType.LPREN)) {
                var callExpr = ParseExpr();
                _program.Add(new ExpressionStatement(callExpr));
            }
            else {
                _index++;
            }
        }
        return _program;
    }
    FunctionDeclaration ParseFunctionDeclaration() {
        // name
        string name = _tokens[_index].Value;
        _index += 2; // skip IDENT and ':'
        _index++;    // skip 'fn'

        // parameters
        Expect(_index, TokenType.LPREN);
        _index++;
        var parameters = new List<string>();
        if (!Expect(_index, TokenType.RPREN)) {
            do {
                parameters.Add(_tokens[_index].Value);
                _index++;
            } while (Expect(_index, TokenType.COMMA) && (_index++ >= 0));
        }
        Expect(_index, TokenType.RPREN);
        _index++; // skip ')'

        // equals
        Expect(_index, TokenType.EQUAL);
        _index++;

        // body
        var body = ParseBlock();
        return new FunctionDeclaration(name, parameters, body);
    }


    IASTNode ParseIndexing(IASTNode target) {
        while (Expect(_index, TokenType.LBRACKET)) {
            _index++; // skip '['
            IASTNode indexExpr = ParseExpr();
            if (!Expect(_index, TokenType.RBRACKET))
                throw new Exception("Expected ']' in indexing");
            _index++; // skip ']'
            target = new IndexExpression(target, indexExpr);
        }
        return target;
    }



    IfStmt ParseIf() {
        if (!Expect(_index, TokenType.IF)) {
            throw new Exception("Expected 'if' keyword");
        }
        _index++; // Move past 'if'

        // Parse condition (e.g., x == 10)
        IASTNode condition = ParseExpr();

        // Parse the "then" block
        if (!Expect(_index, TokenType.LBRACE)) {
            throw new Exception("Expected '{' to start the if block");
        }
        List<IASTNode> thenBranch = ParseBlock().Statements;

        List<IASTNode>? elseBranch = null;

        // Check for optional 'else' block
        if (Expect(_index, TokenType.ELSE)) {
            _index++; // Move past 'else'

            if (!Expect(_index, TokenType.LBRACE)) {
                throw new Exception("Expected '{' to start the else block");
            }
            elseBranch = ParseBlock().Statements;
        }

        return new IfStmt(condition, thenBranch, elseBranch);
    }

    Block ParseBlock() {
        if (!Expect(_index, TokenType.LBRACE))
            throw new Exception("Expected '{'");

        _index++; // skip '{'
        List<IASTNode> stmts = new();

        while (!Expect(_index, TokenType.RBRACE)) {
            if (_index >= _tokens.Count)
                throw new Exception("Unclosed block '{'");

            if (_tokens[_index].Type == TokenType.PRINT)
                stmts.Add(ParsePrint());
            else if (Expect(_index, TokenType.IF))
                stmts.Add(ParseIf());
            else if (Expect(_index, TokenType.FOR))
                stmts.Add(ParseFor());
            else if (Expect(_index, TokenType.RETURN)) {
                _index++;
                var expr = ParseExpr();
                stmts.Add(new ReturnStatement(expr));
            }

            else if (Expect(_index, TokenType.LBRACE))
                stmts.Add(ParseBlock());
            else if (Expect(_index, TokenType.IDENT) && Expect(_index + 1, TokenType.ASSIGN_COLON))
                stmts.Add(ParseLet());
            else if (Expect(_index, TokenType.IDENT) && Expect(_index + 1, TokenType.EQUAL))
                stmts.Add(ParseAssignment());
            else if (Expect(_index, TokenType.IDENT) && Expect(_index + 1, TokenType.LPREN)) {
                var callExpr = ParseExpr();
                _program.Add(new ExpressionStatement(callExpr));
            }
            else
                _index++;
        }

        _index++; // skip '}'
        return new Block(stmts);
    }

    bool Expect(int index, TokenType type) {
        return index < _tokens.Count && _tokens[index].Type == type;
    }

    IASTNode ParseFor() {
        if (!Expect(_index, TokenType.FOR)) {
            throw new Exception("Expected 'for'");
        }
        _index++; // skip 'for'

        // Case: for i in 0..9
        if (Expect(_index, TokenType.IDENT) && Expect(_index + 1, TokenType.IN)) {
            string varName = _tokens[_index].Value;
            _index += 2; // skip ident and 'in'

            IASTNode start = ParseExpr();
            if (!Expect(_index, TokenType.DOT_DOT)) {
                throw new Exception("Expected '..' in range-based for");
            }
            _index++; // skip '..'
            IASTNode end = ParseExpr();

            if (!Expect(_index, TokenType.LBRACE))
                throw new Exception("Expected '{' after for range loop");

            List<IASTNode> body = ParseBlock().Statements;
            return new ForRangeLoop(varName, start, end, body);
        }

        // Case: for i > 9 { ... }
        IASTNode condition = ParseExpr();

        if (!Expect(_index, TokenType.LBRACE))
            throw new Exception("Expected '{' after for condition");

        List<IASTNode> whileBody = ParseBlock().Statements;
        return new WhileLoop(condition, whileBody);
    }

    IASTNode ParseExpr() {
        var left = ParseTerm();
        while (_index < _tokens.Count && (
               Expect(_index, TokenType.PLUS)
            || Expect(_index, TokenType.MINUS)
            || Expect(_index, TokenType.EQUAL_EQUAL)
            || Expect(_index, TokenType.NOT_EQUAL)
            || Expect(_index, TokenType.GREATER)
            || Expect(_index, TokenType.LESS)
        )) {
            string op = _tokens[_index].Value;
            _index++;
            var right = ParseTerm();
            left = new BinExpression(left, op, right);
        }
        return left;
    }

    PrintCall ParsePrint() {
        _index++; // skip the 'print' token

        if (Expect(_index, TokenType.LPREN)) {
            _index++; // skip '('
            IASTNode expr = ParseExpr();
            if (Expect(_index, TokenType.RPREN)) {
                _index++; // skip ')'
                return new PrintCall(expr);
            }
            throw new PipeException($"Undefined token: ')'", _tokens[_index], "PARSING");
        }
        else {
            IASTNode expr = ParseExpr();
            return new PrintCall(expr);
        }
    }

    IASTNode ParseFactor() {
        if (_index >= _tokens.Count) {
            throw new Exception("Unexpected end of input while parsing factor.");
        }
        if (Expect(_index, TokenType.NIL)) {
            _index++; // skip 'nil'
            return new NullExpression(); // a new AST node type
        }

        if (Expect(_index, TokenType.LPREN)) {
            _index++;
            var expr = ParseExpr();
            if (!Expect(_index, TokenType.RPREN)) {
                throw new Exception("Expected ')'");
            }
            _index++;
            return expr;
        }

        if (Expect(_index, TokenType.IDENT)) {
            IASTNode ident = new IdentExpression(_tokens[_index].Value);
            _index++;

            // function call?
            if (Expect(_index, TokenType.LPREN)) {
                _index++; // skip '('
                var args = new List<IASTNode>();
                if (!Expect(_index, TokenType.RPREN)) {
                    do {
                        args.Add(ParseExpr());
                    } while (Expect(_index, TokenType.COMMA) && (_index++ >= 0));
                }
                Expect(_index, TokenType.RPREN);
                _index++;
                return new CallExpression(ident, args);
            }

            return ident;
        }

        // Array literal: [expr, expr, ...]
        if (Expect(_index, TokenType.LBRACKET)) {
            _index++; // skip '['
            var elements = new List<IASTNode>();

            if (!Expect(_index, TokenType.RBRACKET)) {
                elements.Add(ParseExpr());
                while (Expect(_index, TokenType.COMMA)) {
                    _index++; // skip ','
                    elements.Add(ParseExpr());
                }
            }

            if (!Expect(_index, TokenType.RBRACKET)) {
                throw new Exception("Expected ']'");
            }
            _index++; // skip ']'

            return new ArrayExpression(elements);
        }

        // Number literal
        if (Expect(_index, TokenType.NUMBER_LIT)) {
            var number = new NumberExpression(int.Parse(_tokens[_index].Value));
            _index++;
            return number;
        }

        // String literal
        if (Expect(_index, TokenType.STRING_LIT)) {
            var str = new StringExpression(_tokens[_index].Value);
            _index++;
            return str;
        }

        // Variable or indexing expression
        if (Expect(_index, TokenType.IDENT)) {
            IASTNode ident = new IdentExpression(_tokens[_index].Value);
            _index++;

            // Support for indexing: x[expr]
            while (Expect(_index, TokenType.LBRACKET)) {
                _index++; // skip '['
                var indexExpr = ParseExpr();
                if (!Expect(_index, TokenType.RBRACKET)) {
                    throw new Exception("Expected ']'");
                }
                _index++; // skip ']'
                ident = new IndexExpression(ident, indexExpr);
            }

            return ident;
        }

        throw new Exception($"Expected expression, found {_tokens[_index].Type}");
    }


    IASTNode ParseTerm() {
        var left = ParseFactor();
        while (_index < _tokens.Count && (Expect(_index, TokenType.STAR) || Expect(_index, TokenType.SLASH))) {
            string op = _tokens[_index].Value;
            _index++;
            var right = ParseFactor();
            left = new BinExpression(left, op, right);
        }
        return left;
    }

    IASTNode ParseAssignment() {
        if (Expect(_index, TokenType.IDENT) && Expect(_index + 1, TokenType.EQUAL)) {
            string varName = _tokens[_index].Value;
            _index += 2; // Move past IDENT and EQUAL
            IASTNode expr = ParseExpr();
            return new AssignExpression(varName, expr);
        }
        throw new Exception("Invalid assignment syntax.");
    }

    LetExpression ParseLet() {
        if (Expect(_index, TokenType.IDENT) && Expect(_index + 1, TokenType.ASSIGN_COLON)) {
            string varName = _tokens[_index].Value;
            _index += 2; // Move past IDENT and :=
            IASTNode expr = ParseExpr();
            return new LetExpression(varName, expr);
        }
        throw new Exception("Invalid variable declaration syntax.");
    }

}