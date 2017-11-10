using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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


        private List<Var> Modules { get; set; }

        private Prog(TextWriter outputWriter, TextWriter errorWriter)
        {
            Interpreter = new Interpreter();
            RemarkList = new RemarkList(this);

            OutputWriter = outputWriter;
            OutputPrinter = new Printer(new PlainTextCodeWriter(OutputWriter), false);

            ErrorWriter = errorWriter;
            ErrorPrinter = new Printer(new PlainTextCodeWriter(ErrorWriter), true);

            Modules = new List<Var>();
        }


        public static Prog Init(TextWriter outputWriter, TextWriter errorWriter, string asIfFilePath, string codeText)
        {
            var prog = new Prog(outputWriter, errorWriter);
            var ts = new Tokenizer().Tokenize(codeText);
            var e = new Parser(prog.RemarkList).Parse(asIfFilePath, ts);
            prog.RootElement = transform(e);
            return prog;
        }

        private static Element parseFile(Prog prog, string filePath)
        {
            var codeText = File.ReadAllText(filePath);
            var ts = new Tokenizer().Tokenize(codeText);
            return new Parser(prog.RemarkList).Parse(filePath, ts);
        }

        public static Prog Load(TextWriter outputWriter, TextWriter errorWriter, string filePath)
        {
            return Init(outputWriter, errorWriter, filePath, File.ReadAllText(filePath));
        }

        public static Prog Load2(TextWriter outputWriter, TextWriter errorWriter, IReadOnlyList<string> rootPaths)
        {
            rootPaths = rootPaths.Select(Path.GetFullPath).ToList();
            var prog = new Prog(outputWriter, errorWriter);

            foreach (var fp in rootPaths)
            {
                var codeFilesPaths = getAllCodeFiles(fp);
                var shortest = codeFilesPaths.OrderBy(cfp => cfp.Length).First();
                var numSeparators = shortest.Split('\\').Length;
                foreach (var cfp in codeFilesPaths)
                {
                    var sections = cfp.Split('\\').Skip(numSeparators).ToList();
                    var e = parseFile(prog, cfp);
                    var m = transformToModule(e, sections);
                    prog.Modules.Add(m);
                }
            }
            prog.RootElement = new FnApply(
                new Fn(new FnParameters(), new Sequence(prog.Modules)),
                new FnArguments());
            return prog;
        }

        public sealed class ModuleNode
        {
            public readonly string Name;
            public readonly List<ModuleNode> Children = new List<ModuleNode>();
            public ModuleNode(string name) => Name = name;
        }

        private static IReadOnlyList<ModuleNode> getModulePaths(IEnumerable<string> codeFilesPaths, IReadOnlyList<string> rootPaths)
        {
            var rootMods = new List<ModuleNode>();
            foreach (var cfp in codeFilesPaths)
            {
                var items = filePathToModulePath(cfp);
                var mods = rootMods;
                foreach (var i in items)
                {
                    var mod = mods.Find(m => m.Name == i);
                    if (mod == null)
                    {
                        mod = new ModuleNode(i);
                        mods.Add(mod);
                    }
                    mods = mod.Children;
                }
            }
            return rootMods;
        }


        private static string[] filePathToModulePath(string cfp)
        {
            var f = cfp.Substring("C:\\".Length, cfp.Length - ".ef".Length - 1);
            var items = f.Split('\\');
            return items;
        }


        private static IReadOnlyList<string> getAllCodeFiles(string rootPath)
        {
            var files = new List<string>();
            if (Directory.Exists(rootPath))
            {
                files.AddRange(Directory.EnumerateFiles(rootPath, "*.ef", SearchOption.AllDirectories));
            }
            else if (File.Exists(rootPath))
            {
                if (!rootPath.EndsWith(".ef"))
                {
                    throw new Exception("File is not supported '" + rootPath + "'.");
                }
                files.Add(rootPath);
            }
            else
            {
                throw new Exception("Could not locate file system entry '" + rootPath + "'.");
            }

            return files.Distinct().ToList();
        }


        public Exp Run()
        {
            var res = Interpreter.Eval(this);
            return res;
        }


        private static Element transform(Element e)
        {
            if (e is Exp exp)
                return exp;
            if (e is Stm stm)
                e = new Sequence(new[] {stm});
            if (e is Sequence body2)
                return new FnApply(
                    new Fn(new FnParameters(), body2),
                    new FnArguments());
            throw new NotSupportedException();
        }


        private static Var transformToModule(Element e, IReadOnlyList<string> modulePath)
        {
            if (e is Sequence seq)
            {
                var vars = seq.Cast<Var>().ToArray();
                var ident = new Ident("TODO", TokenType.Ident);
                var var = new Var(ident, new New(new ClassBody(vars)));
                return var;
            }
            throw new Exception();
        }
    }
}