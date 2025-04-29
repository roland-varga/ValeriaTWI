using Valeria.Lexer;

namespace Valeria;
class Program {
    static void Main(string[] args) {
        try {
            Valeria.Lexer.Lexer lexer = new Valeria.Lexer.Lexer(@"C:\Users\Csakr\Desktop\North\Valeria\examples\main.va");
            List<Token> tokens = lexer.Lex(File.ReadAllText(@"C:\Users\Csakr\Desktop\North\Valeria\examples\main.va"));

            foreach (var item in tokens) {
                System.Console.WriteLine(item.ToString());
            }
        }
        catch (LexerException ex) {
            // Read the source file for line-based error formatting
            string[] sourceLines = File.ReadAllLines(@"C:\Users\Csakr\Desktop\North\Valeria\examples\main.va");

            // Format the error and print it
            System.Console.WriteLine(ex.FormatError(sourceLines));
        }
    }
}