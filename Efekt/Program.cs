using System;
using System.Linq;

namespace Efekt
{
    public static class Program
    {
        private static readonly Tokenizer t = new Tokenizer();
        private static readonly Parser p = new Parser();
        private static readonly Interpreter i = new Interpreter();
        private static readonly CodeTextWriter ctw = new CodeTextWriter(new ConsoleWriter());
        private static readonly CodeWriter cw = new CodeWriter(ctw);

        private static void Main(string[] args)
        {
            C.Nn(args);

            Tests.RunAllTests();

            const string code = "";
            debug(code);

            Console.ReadLine();
        }

        private static void debug(string code)
        {
            var ts = t.Tokenize(code).ToList();
            Console.WriteLine("TOKENS");
            foreach (var tok in ts)
            {
                Console.Write(tok.Type.ToString().PadRight(8));
                Console.WriteLine("'" + tok.Text + "'");
            }

            Console.WriteLine();
            Console.WriteLine("PARSE");
            var pse = p.Parse(ts);
            cw.Write(pse);

            Console.WriteLine();
            Console.WriteLine("EVAL");
            var res = i.Eval(pse);
            cw.Write(res);
        }
    }
}