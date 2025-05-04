namespace Valeria.Lexer;
public class Lexer {

    string _fileName;
    List<Token> _tokens = new();
    uint _currentPos = 0;
    uint _currentLine = 1;
    uint _currentChar = 0;

    public Lexer(string fileName) {
        _fileName = fileName;
    }

    public Dictionary<string, TokenType> Keywords = new() {
        {"if", TokenType.IF},
        {"else", TokenType.ELSE},
        {"fn", TokenType.FN},
        {"return", TokenType.RETURN},
        {"for", TokenType.FOR},
        {"print", TokenType.PRINT},
        {"printLn", TokenType.PRINTLN},
        {"in", TokenType.IN},
        {"nil", TokenType.NIL}
    };

    char shift(char[] src) {
        _currentChar++;
        return src[_currentPos++];
    }

    void AddToken(string value, TokenType type, char[] src) {
        _tokens.Add(new Token(
            value: value,
            type: type,
            tokenPosition: new TokenPosition(
                _fileName,
                _currentLine,
                _currentChar
            )
        ));
    }

    public List<Token> Lex(string sourceCode) {
        char[] src = sourceCode.ToCharArray();

        while (_currentPos < src.Length) {
            if (Char.IsWhiteSpace(src[_currentPos]) && src[_currentPos] != '\n') {
                shift(src);
            }
            else if (src[_currentPos] == '\n') {
                shift(src);
                _currentLine++;
                _currentChar = 0;
            }
            else if (src[_currentPos] == '#') {
                while (_currentPos < src.Length && src[_currentPos] != '\n') {
                    shift(src);
                }
                _currentLine++;
                shift(src);
            }
            else if (src[_currentPos] == '.') {
                if (_currentPos + 1 < src.Length && src[_currentPos + 1] == '.') {
                    AddToken("..", TokenType.DOT_DOT, src);
                    shift(src);
                    shift(src);
                }
                else {
                    AddToken(".", TokenType.DOT, src);
                    shift(src);
                }
            }
            else if (src[_currentPos] == '(') {
                AddToken("(", TokenType.LPREN, src);
                shift(src);
            }
            else if (src[_currentPos] == ')') {
                AddToken(")", TokenType.RPREN, src);
                shift(src);
            }
            else if (src[_currentPos] == '[') {
                AddToken("[", TokenType.LBRACKET, src);
                shift(src);
            }
            else if (src[_currentPos] == ']') {
                AddToken("]", TokenType.RBRACKET, src);
                shift(src);
            }
            else if (src[_currentPos] == '{') {
                AddToken("{", TokenType.LBRACE, src);
                shift(src);
            }
            else if (src[_currentPos] == '}') {
                AddToken("}", TokenType.RBRACE, src);
                shift(src);
            }
            else if (src[_currentPos] == ',') {
                AddToken(",", TokenType.COMMA, src);
                shift(src);
            }
            else if (src[_currentPos] == ':') {
                if (_currentPos + 1 < src.Length && src[_currentPos + 1] == '=') {
                    AddToken(":=", TokenType.ASSIGN_COLON, src);
                    shift(src);
                    shift(src);
                }
                else {
                    AddToken(":", TokenType.COLON, src);
                    shift(src);
                }
            }
            else if (src[_currentPos] == '+') {
                AddToken("+", TokenType.PLUS, src);
                shift(src);
            }
            else if (src[_currentPos] == '-') {
                AddToken("-", TokenType.MINUS, src);
                shift(src);
            }
            else if (src[_currentPos] == '*') {
                AddToken("*", TokenType.STAR, src);
                shift(src);
            }
            else if (src[_currentPos] == '/') {
                AddToken("/", TokenType.SLASH, src);
                shift(src);
            }
            else if (src[_currentPos] == '>') {
                AddToken(">", TokenType.GREATER, src);
                shift(src);
            }
            else if (src[_currentPos] == '<') {
                AddToken("<", TokenType.LESS, src);
                shift(src);
            }
            else if (src[_currentPos] == '!') {
                if (_currentPos + 1 < src.Length && src[_currentPos + 1] == '=') {
                    AddToken("!=", TokenType.NOT_EQUAL, src);
                    shift(src);
                    shift(src);
                }
                else {
                    AddToken("!", TokenType.NOT, src);
                    shift(src);
                }
            }

            else if (src[_currentPos] == '=') {
                if (_currentPos + 1 < src.Length && src[_currentPos + 1] == '=') {
                    AddToken("==", TokenType.EQUAL_EQUAL, src);
                    shift(src);
                    shift(src);
                }
                else {
                    AddToken("=", TokenType.EQUAL, src);
                    shift(src);
                }
            }
            else if (src[_currentPos] == '"') {
                shift(src); // Skip opening quote
                string buffer = "";
                while (_currentPos < src.Length && src[_currentPos] != '"') {
                    buffer += src[_currentPos];
                    shift(src);
                }
                if (_currentPos >= src.Length || src[_currentPos] != '"') {
                    Token errorToken = new Token(
                        value: "\"",
                        type: TokenType.EOF,
                        tokenPosition: new TokenPosition(_fileName, _currentLine, _currentChar)
                    );
                    throw new PipeException($"String literals need closing `\"`!", errorToken, "LEXING");
                }
                shift(src); // Skip closing quote
                AddToken(buffer, TokenType.STRING_LIT, src);
            }
            else if (Char.IsNumber(src[_currentPos])) {
                string buffer = "";
                bool hasDot = false;

                while (_currentPos < src.Length) {
                    // Check for range operator case FIRST
                    if (src[_currentPos] == '.' &&
                        _currentPos + 1 < src.Length &&
                        src[_currentPos + 1] == '.') {
                        break;
                    }

                    if (src[_currentPos] == '.') {
                        if (hasDot) break; // Only allow one decimal point
                        hasDot = true;
                    }
                    else if (!Char.IsNumber(src[_currentPos])) {
                        break;
                    }

                    buffer += src[_currentPos];
                    shift(src);
                }

                AddToken(buffer, TokenType.NUMBER_LIT, src);
            }
            else if (Char.IsLetterOrDigit(src[_currentPos]) || '_' == src[_currentPos]) {
                string buffer = "";
                while (_currentPos < src.Length && (Char.IsLetterOrDigit(src[_currentPos]) || src[_currentPos] == '_')) {
                    buffer += src[_currentPos];
                    shift(src);
                }
                if (!Keywords.TryGetValue(buffer, out var type)) {
                    type = TokenType.IDENT;
                }
                AddToken(buffer, type, src);
            }
            else {
                string invalidChar = src[_currentPos].ToString();

                Token errorToken = new Token(
                    value: invalidChar,
                    type: TokenType.EOF, // or a special TokenType.ERROR
                    tokenPosition: new TokenPosition(_fileName, _currentLine, _currentChar)
                );

                throw new PipeException($"Undefined token: '{invalidChar}'", errorToken, "LEXING");
            }
        }

        _tokens.Add(new Token("EOF", TokenType.EOF, new TokenPosition()));
        return _tokens;
    }
}