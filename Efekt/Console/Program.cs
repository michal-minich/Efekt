using System;
using System.Linq;

namespace Efekt
{
    public static class Program
    {
        private static readonly Tokenizer t = new Tokenizer();
        private static readonly Interpreter i = new Interpreter();
        private static readonly PlainTextCodeWriter ctw = new PlainTextCodeWriter(new ConsoleWriter());
        private static readonly Printer cw = new Printer(ctw);

        private static void Main(string[] args)
        {
            try
            {
                C.Nn(args);

                Tests.Tests.RunAllTests();

                if (args.Length != 0)
                {
                    var prog = Prog.Load(new ConsoleWriter(), args[0]);
                    prog.Run();
                    return;
                }

                const string code = "";
                debug(code);

            }
            catch (EfektException ex)
            {
                Console.Write("Error: " + ex.Message);
            }
            Console.ReadLine();
        }


        private static void debug(string code)
        {
            var ts = t.Tokenize(code).ToList();
            Console.WriteLine("TOKENS");
            foreach (var tok in ts)
            {
                Console.Write(tok.Type.ToString().PadRight(8));
                Console.WriteLine("'" + tok.Text.Replace("\n", "\\n").Replace("\r", "\\r") + "'");
            }

            Console.WriteLine();
            Console.WriteLine("PARSE");
            var remarkWriter = new ConsoleWriter();
            var pse = new Parser(new Remark(remarkWriter)).Parse("debug.ef", ts);
            cw.Write(pse);

            Console.WriteLine();
            Console.WriteLine("EVAL");
            var prog = Prog.Init(remarkWriter, pse);
            var res = i.Eval(prog);
            cw.Write(res);

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("OUT");
            var text = Builtins.Writer.GetAndReset();
            Console.WriteLine(text);
        }
    }
}