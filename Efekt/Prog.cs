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
        public string FilePath { get; private set; }
        public Remark Remark { get; }

        public string RelativeFilePath => Utils.GetFilePathRelativeToBase(FilePath);

        private Prog(TextWriter remarkWriter)
        {
            Remark = new Remark(remarkWriter);
            Instance = this;
        }


        public static Prog Init(TextWriter remarkWriter, Element parsedElement)
        {
            var prog = new Prog(remarkWriter);
            prog.FilePath = "";
            prog.RootElement = Tranform(parsedElement);
            return prog;
        }

        
        public static Prog Load(TextWriter remarkWriter, string filePath)
        {
            C.Nn(filePath);
            var prog = new Prog(remarkWriter);
            prog.FilePath = filePath;
            var code = File.ReadAllText(filePath);

            var ts = t.Tokenize(code);
            var e = new Parser(prog.Remark).Parse(filePath, ts);
            prog.RootElement = Tranform(e);
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
