using System.IO;

namespace Efekt
{
    public sealed class Prog
    {
        public readonly Interpreter Interpreter;
        public readonly TextWriter OutputWriter;
        public readonly TextWriter ErrorWriter;
        public readonly Printer OutputPrinter;
        public readonly Printer ErrorPrinter;
        public readonly RemarkList RemarkList;

        public Element RootElement { get; private set; }

        private Prog(TextWriter outputWriter, TextWriter errorWriter)
        {
            Interpreter = new Interpreter();
            RemarkList = new RemarkList(this);
            OutputWriter = outputWriter;
            ErrorWriter = errorWriter;
            ErrorPrinter = OutputPrinter;
            OutputPrinter = new Printer(new PlainTextCodeWriter(OutputWriter));
        }


        public static Prog Init(TextWriter outputWriter, TextWriter errorWriter, string asIfFilePath, string codeText)
        {
            var prog = new Prog(outputWriter, errorWriter);
            var ts = new Tokenizer().Tokenize(codeText);
            var e = new Parser(prog.RemarkList).Parse(asIfFilePath, ts);
            prog.RootElement = Transform(e);
            return prog;
        }


        public static Prog Load(TextWriter outputWriter, TextWriter errorWriter, string filePath)
        {
            return Init(outputWriter, errorWriter, filePath, File.ReadAllText(filePath));
        }


        public Exp Run()
        {
            var res = Interpreter.Eval(this);
            return res;
        }


        public static Element Transform(Element e)
        {
            if (e is Exp exp)
                e = new Sequence(new[] {new Return(exp)});

            if (e is Sequence body2)
                e = new FnApply(
                    new Fn(new FnParameters(), body2),
                    new FnArguments());

            return e;
        }
    }
}