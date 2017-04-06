using System;
using System.Collections.Generic;
using System.Linq;

namespace Efekt
{
    public sealed class Program
    {
        private static void Main(string[] args)
        {
            SyntaxElement se =
                new ElementList<SyntaxElement>(
                    new List<SyntaxElement>
                    {
                        new Var(new Ident("x"), new Fn(
                            new ElementList<Ident>(new List<Ident>()),
                            new ElementList<SyntaxElement>(new List<SyntaxElement> {new Return(new Int(123))}))),
                        new Return(new FnApply(new Ident("x"), new ElementList<ExpElement>(new List<ExpElement>())))
                    });

            if (se is ElementList<SyntaxElement> body)
                se = new FnApply(
                    new Fn(new ElementList<Ident>(new List<Ident>()), body),
                    new ElementList<ExpElement>(new List<ExpElement>()));

            Console.WriteLine("TOKENS");
            var tr = new Tokenizer();
            var ts = tr.Tokenize("fn { var x = fn { return 1_2_3 } return x() }()").ToList();
            foreach (var t in ts)
            {
                Console.Write(t.Type);
                Console.WriteLine(": '" + t.Text + "'");
            }

            Console.WriteLine();
            Console.WriteLine("CODE");
            var w = new ConsoleWriter();
            var cw = new CodeTextWriter(w);
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