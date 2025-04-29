namespace Valeria.Lexer;

public enum TokenType {

    /* TYPES */
    NUMBER_LIT, IDENT, STRING_LIT,


    COMMA, LBRACKET, RBRACKET, ASSIGN_COLON,
    LPREN, RPREN, EQUAL, LBRACE, RBRACE, COLON,

    /* LOGICAL */
    EQUAL_EQUAL, NOT_EQUAL, GREATER,

    /* MATH */
    PLUS, MINUS, STAR, SLASH, NOT,

    /* CONTROLL FLOW */

    IF, ELSE, FN, RETURN, FOR,

    /* BUILT-IN FUNCTIONS */
    PRINT, PRINTLN,

    EOF
}

public class TokenPosition {

    public TokenPosition() { }

    public TokenPosition(string fileName, uint line, uint @char) {
        FileName = fileName;
        Line = line;
        Char = @char;
    }

    public string FileName { get; private set; } = "";
    public uint Line { get; private set; } = 0;
    public uint Char { get; private set; } = 0;

    public override string ToString() {
        return $"{FileName}:{Line}:{Char}";
    }

}

public class Token {

    public Token(string value, TokenType type, TokenPosition tokenPosition) {
        Value = value;
        Type = type;
        TokenPosition = tokenPosition;
    }

    public string Value { get; private set; }
    public TokenType Type { get; private set; }
    public TokenPosition TokenPosition { get; private set; }

    public override string ToString() {
        return $"`{Value.ToString()}` ?{Type.ToString()} ({TokenPosition.ToString()})";
    }

}