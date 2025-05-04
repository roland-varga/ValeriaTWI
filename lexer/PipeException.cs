namespace Valeria.Lexer;

public class PipeException : Exception {
    public Token Token { get; }
    public string ErrorType;
    public PipeException(string message, Token token, string errorType)
        : base(message) {
        Token = token;
        ErrorType = errorType;
    }

    public string FormatError(string[] sourceLines) {
        int lineIndex = (int)Token.TokenPosition.Line - 1;

        string line = lineIndex >= 0 && lineIndex < sourceLines.Length
            ? sourceLines[lineIndex]
            : "<invalid line index>";

        string pointerLine = line != "<invalid line index>" && Token.TokenPosition.Char > 0
            ? new string('-', (int)Token.TokenPosition.Char - 1) + "^"
            : "^";

        string redColor = "\u001b[31m";
        string resetColor = "\u001b[0m";

        return $"🟥 {ErrorType} ERROR | [{Token.TokenPosition}] 🟥{resetColor}\n" +
               $"{line} | line: {Token.TokenPosition.Line}\n" +
               $"{pointerLine}\n" +
               $"{redColor}{Message}{resetColor}\n";
    }


}