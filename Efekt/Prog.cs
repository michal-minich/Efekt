using System.IO;

namespace Efekt
{
    public sealed class Prog
    {
        public Interpreter Interpreter { get; }
        public RemarkList RemarkList { get; }

        public TextWriter OutputWriter { get; }
        public Printer OutputPrinter { get; }

        public TextWriter ErrorWriter { get; }
        public Printer ErrorPrinter { get; }


        public Element RootElement { get; private set; }


        private Prog(TextWriter outputWriter, TextWriter errorWriter)
        {
            Interpreter = new Interpreter();
            RemarkList = new RemarkList(this);

            OutputWriter = outputWriter;
            OutputPrinter = new Printer(new PlainTextCodeWriter(OutputWriter), false);

            ErrorWriter = errorWriter;
            ErrorPrinter = new Printer(new PlainTextCodeWriter(ErrorWriter), true);
        }


        public static Prog Init(TextWriter outputWriter, TextWriter errorWriter, string asIfFilePath, string codeText)
        {
            var prog = new Prog(outputWriter, errorWriter);
            var ts = new Tokenizer().Tokenize(codeText);
            var e = new Parser(prog.RemarkList).Parse(asIfFilePath, ts);
            prog.RootElement = transform(e);
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


        private static Element transformToModule(Element e, string filePath)
        {
            if (e is Exp)
                return transform(e);
            var name = Path.GetFileNameWithoutExtension(filePath);
            var ident = new Ident(name, TokenType.Ident);
            var var = new Var(ident, new New(new ClassBody(null)));
            return var;
        }

        private static Element transform(Element e)
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