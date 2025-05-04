using Valeria.Interpreter;
using Valeria.Lexer;
using Valeria.Parser;

namespace Valeria;
class Program {
    static void Main(string[] args) {
        try {
            Valeria.Lexer.Lexer lexer = new Valeria.Lexer.Lexer(@"C:\Users\Csakr\Desktop\North\Valeria\examples\main.va");
            List<Token> tokens = lexer.Lex(File.ReadAllText(@"C:\Users\Csakr\Desktop\North\Valeria\examples\main.va"));

            Valeria.Parser.Parser parser = new Valeria.Parser.Parser(tokens);
            List<IASTNode> program = parser.ParseProgram();

            Evaluator eval = new();
            foreach (var stmt in program) {
                eval.Evaluate(stmt);
            }
            //foreach (var item in program)
            //{
            //    System.Console.WriteLine(item.ToString());
            //}

        }
        catch (PipeException ex) {
            // Read the source file for line-based error formatting
            string[] sourceLines = File.ReadAllLines(@"C:\Users\Csakr\Desktop\North\Valeria\examples\main.va");

            // Format the error and print it
            System.Console.WriteLine(ex.FormatError(sourceLines));
        }
    }
}