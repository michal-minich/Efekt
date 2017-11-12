using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

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


        public ClassBody Modules { get; set; }
        
        private Prog(TextWriter outputWriter, TextWriter errorWriter)
        {
            Interpreter = new Interpreter();
            RemarkList = new RemarkList(this);

            OutputWriter = outputWriter;
            OutputPrinter = new Printer(new PlainTextCodeWriter(OutputWriter), false);

            ErrorWriter = errorWriter;
            ErrorPrinter = new Printer(new PlainTextCodeWriter(ErrorWriter), true);

            Modules = new ClassBody(new List<Var>());
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
                var numSeparators = shortest.Split('\\').Length - 2;
                foreach (var cfp in codeFilesPaths)
                {
                    var startIndex = "C:\\".Length;
                    var f = cfp.Substring(startIndex, cfp.Length - ".ef".Length - startIndex);
                    var sections = f.Split('\\').Skip(numSeparators).ToList();
                    var e = parseFile(prog, cfp);
                    addMod(prog, sections, e);
                }
            }

            var start = new FnApply(getStartQualifiedName(prog), new FnArguments());
            prog.RootElement = new FnApply(
                new Fn(new FnParameters(), new Sequence(prog.Modules.Cast<Element>().Append(start).ToList())),
                new FnArguments());
            return prog;
        }

        private static Exp getStartQualifiedName(Prog prog)
        {
            var candidates = new List<List<string>>();
            findStart(prog, prog.Modules, candidates, new List<string>());
            if (candidates.Count != 1)
                throw new Exception();
            var fn = candidates[0]
                .Append("start")
                .Select(section => new Ident(section, TokenType.Ident))
                .Cast<Exp>()
                .Aggregate((a, b) => new MemberAccess(a, (Ident) b));
            return fn;
        }


        private static void findStart(Prog prog, ClassBody modules, List<List<string>> candidates, List<string> path)
        {
            foreach (var m in modules)
            {
                var i = m.Ident.Name;
                if (i == "start")
                    candidates.Add(path);
                if (m.Exp is New n)
                {
                    findStart(prog, n.Body, candidates, path.Append(i).ToList());
                }
            }
        }


        private static void addMod(Prog prog, IReadOnlyList<string> sections, Element mod)
        {
            var mods = prog.Modules;
            foreach (var s in sections.Skip(1))
            {
                var m = findParentModule(mods, s);
                if (m == null)
                {
                    var empty = getEmptyModule(s);
                    mods.Add(empty);
                    mods = ((New) empty.Exp).Body;
                }
                else
                {
                    mods = m;
                }
            }
            var lastSection = sections.Last();
            var m2 = findParentModule(mods, lastSection);
            if (m2 != null)
                throw new Exception();
            mods.Add(getNewModule(lastSection, mod));
        }


        [CanBeNull]
        private static ClassBody findParentModule(IEnumerable<Var> mods, string name)
        {
            foreach (var m in mods)
                if (m.Ident.Name == name)
                    return ((New) m.Exp).Body;
            return null;
        }


        private static Var getEmptyModule(string name)
        {
            return new Var(new Ident(name, TokenType.Ident), new New(new ClassBody(new List<Var>())));
        }



        private static Var getNewModule(string name, Element body)
        {
            if (body is Sequence seq)
            {
                var vars = seq.Cast<Var>().ToArray();
                return new Var(new Ident(name, TokenType.Ident), new New(new ClassBody(vars)));
            }
            throw new Exception();
        }


        public sealed class ModuleNode
        {
            public readonly string Name;
            public readonly Element Parsed;
            public readonly List<ModuleNode> Children = new List<ModuleNode>();

            public ModuleNode(string name, Element parsed)
            {
                Name = name;
                Parsed = parsed;
            }
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
    }
}