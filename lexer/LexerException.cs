namespace Valeria.Lexer;

public class LexerException : Exception {
    public Token Token { get; }

    public LexerException(string message, Token token)
        : base(message) {
        Token = token;
    }

    public string FormatError(string[] sourceLines) {
        int lineIndex = (int)Token.TokenPosition.Line - 1;
        string line = sourceLines[lineIndex];
        string pointerLine = new string('-', (int)Token.TokenPosition.Char - 1) + "^";

        // Define red color using ANSI escape codes
        string redColor = "\u001b[31m";  // ANSI escape code for red
        string resetColor = "\u001b[0m"; // Reset color to default

        // Add color formatting to the undefined token part
        string formattedMessage = $"🟥 LEXING ERROR | [{Token.TokenPosition}] 🟥{resetColor}\n" +
                                  $"{line} | line: {Token.TokenPosition.Line}\n" +
                                  $"{pointerLine}\n" +
                                  $"{redColor}{Message}{resetColor}\n";  // Apply red color to the error message

        return formattedMessage;
    }

}