using System;
using System.Collections.Generic;

namespace Efekt
{
    public class Program
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

            Console.WriteLine("CODE");
            var w = new ConsoleWriter();
            var cw = new CodeTextWriter(w);
            CodeWriter.Write(se, cw);

            var i = new Interpreter();
            var res = i.Eval(se);

            Console.WriteLine();
            Console.WriteLine("RES");
            CodeWriter.Write(res, cw);

            Console.ReadLine();
        }
    }
}