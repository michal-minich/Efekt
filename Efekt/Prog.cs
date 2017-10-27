using System;
using System.IO;

namespace Efekt
{
    public sealed class Prog
    {
        public static Prog Instance { get; private set; }

        private static readonly Tokenizer t = new Tokenizer();
        private static readonly Interpreter i = new Interpreter();
        private static readonly PlainTextCodeWriter ctw = new PlainTextCodeWriter(new ConsoleWriter());
        private static readonly Printer cw = new Printer(ctw);

        public Element RootElement { get; private set; }
        public Remark Remark { get; }

        private Prog(TextWriter remarkWriter)
        {
            Remark = new Remark(remarkWriter);
            Instance = this;
        }


        public static Prog Init(TextWriter remarkWriter, Element parsedElement)
        {
            var prog = new Prog(remarkWriter);
            prog.RootElement = Tranform(parsedElement, prog.Remark);
            return prog;
        }


        public static Prog Load(TextWriter remarkWriter, string filePath)
        {
            C.Nn(filePath);
            var prog = new Prog(remarkWriter);
            var code = File.ReadAllText(filePath);

            var ts = t.Tokenize(code);
            var e = new Parser(prog.Remark).Parse(filePath, ts);
            prog.RootElement = Tranform(e, prog.Remark);
            return prog;
        }


        public void Run()
        {
            var res = i.Eval(this);
            var text = Builtins.Writer.GetAndReset();
            Console.WriteLine(text);
            if (res != Void.Instance)
            {
                Console.Write("Output: ");
                cw.Write(res);
            }
        }


        public static Element Tranform(Element e, Remark remark)
        {
            if (e is Exp exp)
                e = new Sequence(new[] {new Return(exp)});

            if (e is Sequence body2)
                e = new FnApply(
                    new Fn(new FnParameters(), body2),
                    new FnArguments());

            return e;
        }


        public static Element Tranform2(Element e, Remark remark)
        {
            Sequence seq;

            if (e is Exp exp)
                return e;
            else if (e is Sequence s)
                seq = s;
            else
                seq = new Sequence(new[] {e});

            return new Sequence(new Element[]
            {
                new Var(new Ident("start", TokenType.Ident), new Fn(new FnParameters(), seq)),
            });
        }
    }
}