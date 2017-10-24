using System;
using System.IO;

namespace Efekt
{
    public sealed class Prog
    {
        private static readonly Tokenizer t = new Tokenizer();
        private static readonly Parser p = new Parser();
        private static readonly Interpreter i = new Interpreter();
        private static readonly PlainTextCodeWriter ctw = new PlainTextCodeWriter(new ConsoleWriter());
        private static readonly Printer cw = new Printer(ctw);

        public Element RootElement { get; private set; }
        public string FilePath { get; private set; }


        public static Prog Init(Element parsedElement)
        {
            var prog = new Prog();
            prog.FilePath = "";
            prog.RootElement = Tranform(parsedElement);
            return prog;
        }


        public static Prog Load(string filePath)
        {
            C.Nn(filePath);
            var prog = new Prog();
            prog.FilePath = filePath;
            var code = File.ReadAllText(filePath);

            var ts = t.Tokenize(code);
            var e = p.Parse(ts);
            prog.RootElement = Tranform(e);
            return prog;
        }


        public void Run()
        {
            var res = i.Eval(this);
            var text = Builtins.Writer.GetAndReset();
            Console.WriteLine(text);
            Console.Write("Output: ");
            cw.Write(res);
        }


        public static Element Tranform(Element e)
        {
            if (e is Exp exp)
                e = new Sequence(new[] {new Return(exp)});

            if (e is Sequence body)
                e = new FnApply(
                    new Fn(new FnParameters(), body),
                    new FnArguments());
            return e;
        }
    }
}
