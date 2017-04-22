using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Efekt
{
    public sealed class Program
    {
        private static void Main(string[] args)
        {
            Contract.Requires(args != null);

            SyntaxElement se =
                new StatementList(
                    new Var(new Ident("x"), new Fn(
                        new IdentList(),
                        new StatementList(new Return(new Int(123))))),
                    new Return(new FnApply(new Ident("x"), new ExpList()))
                );

            if (se is StatementList body)
                se = new FnApply(
                    new Fn(new IdentList(), body),
                    new ExpList());

            var w = new ConsoleWriter();
            var cw = new CodeTextWriter(w);

            Console.WriteLine("TOKENS");
            var tr = new Tokenizer();
            var ts = tr.Tokenize("fn { var x = fn { return 1_2_3 } return x }").ToList(); // fn { var x = fn { return 1_2_3 } return x() }()
            foreach (var t in ts)
            {
                Console.Write(t.Type);
                Console.WriteLine(": '" + t.Text + "'");
            }

            Console.WriteLine("PARSE");
            var p = new Parser();
            var pse = p.Parse(ts);
            CodeWriter.Write(pse, cw);

            Console.WriteLine();
            Console.WriteLine("CODE");
            CodeWriter.Write(se, cw);

            var i = new Interpreter();
            var res = i.Eval(se);

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("RES");
            CodeWriter.Write(res, cw);

            Console.ReadLine();
        }
    }
}