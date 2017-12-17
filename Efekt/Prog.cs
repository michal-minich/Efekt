using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        
        internal Prog(TextWriter outputWriter, TextWriter errorWriter)
        {
            Interpreter = new Interpreter();
            RemarkList = new RemarkList(this);

            OutputWriter = outputWriter;
            OutputPrinter = new Printer(new PlainTextCodeWriter(outputWriter), false);

            ErrorWriter = errorWriter;
            ErrorPrinter = new Printer(new PlainTextCodeWriter(errorWriter), true);
        }


        public static Prog Init(TextWriter outputWriter, TextWriter errorWriter, string asIfFilePath, string codeText)
        {
            var prog = new Prog(outputWriter, errorWriter);
            var ts = new Tokenizer().Tokenize(codeText);
            var e = new Parser(prog.RemarkList).Parse(asIfFilePath, ts);
            prog.RootElement = transform(e);
            new Namer(prog).Name();
            new StructureValidator(prog).Validate();
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
            var modules = new List<ClassItem>();
            foreach (var fp in rootPaths)
            {
                var codeFilesPaths = getAllCodeFiles(fp, prog.RemarkList);
                var shortest = codeFilesPaths.OrderBy(cfp => cfp.Length).First();
                var numSeparators = shortest.Split('\\').Length - 2;
                foreach (var cfp in codeFilesPaths)
                {
                    var startIndex = "C:\\".Length;
                    var f = cfp.Substring(startIndex, cfp.Length - ".ef".Length - startIndex);
                    var sections = f.Split('\\').Skip(numSeparators).ToList();
                    var e = parseFile(prog, cfp);
                    Sequence moduleBody;
                    if (e is Sequence seq)
                        moduleBody = seq;
                    else
                        moduleBody = new Sequence(new[] {e}.AsSequenceItems(prog.RemarkList));
                    addMod(sections, moduleBody, modules, prog);
                }
            }

            var start = new FnApply(getStartQualifiedName(modules, prog.RemarkList), new FnArguments());
            var seqItems = modules.Cast<SequenceItem>().Append(start).ToList();
            prog.RootElement = new FnApply(
                new Fn(new FnParameters(), new Sequence(seqItems)),
                new FnArguments());
            new Namer(prog).Name();
            new StructureValidator(prog).Validate();
            return prog;
        }

        private static QualifiedIdent getStartQualifiedName(List<ClassItem> modules, RemarkList remarkList)
        {
            var candidates = new List<List<string>>();
            findStart(modules, candidates, new List<string>());
            if (candidates.Count != 1)
                throw remarkList.CannotFindStartFunction();
            var fn = candidates[0]
                .Append("start")
                .Select(section => (QualifiedIdent) new Ident(section, TokenType.Ident))
                .Aggregate((a, b) => new MemberAccess(a, (Ident) b));
            return fn;
        }


        private static void findStart(IEnumerable<ClassItem> classBody, List<List<string>> candidates, List<string> path)
        {
            foreach (var ci in classBody)
            {
                if (ci is Declr d)
                {
                    var i = d.Ident.Name;
                    if (i == "start")
                        candidates.AddValue(path);
                    if (d.Exp is New n)
                    {
                        findStart(n.Body, candidates, path.Append(i).ToList());
                    }
                }       
            }
        }


        private static void addMod(IReadOnlyList<string> sections, Sequence moduleBody, List<ClassItem> modules, Prog prog)
        {
            C.Nn(modules);

            foreach (var s in sections.Skip(1))
            {
                var m = findParentModule(modules, s);
                if (m == null)
                {
                    modules.AddValue(new Let(new Ident(s, TokenType.Ident), new New(new ClassBody(new List<ClassItem>()))));
                    modules = new List<ClassItem>();
                }
                else
                {
                    modules = m.ToList();
                }
            }
            var lastSection = sections.Last();
            var m2 = findParentModule(modules, lastSection);
            if (m2 != null)
                throw new Exception();
            modules.AddValue(getNewModule(lastSection, moduleBody, prog));
        }


        [CanBeNull]
        private static ClassBody findParentModule(IEnumerable<ClassItem> mods, string name)
        {
            foreach (var ci in mods)
                if (ci is Declr v && v.Ident.Name == name)
                    return ((New) v.Exp).Body;
            return null;
        }


        private static Let getNewModule(string name, Sequence moduleBody, Prog prog)
        {
            var preludeImport = new Import(new Ident("prelude", TokenType.Ident));
            if (name != "prelude")
                moduleBody.InsertImport(preludeImport);
            return new Let(new Ident(name, TokenType.Ident), 
                new New(new ClassBody(moduleBody.AsClassItems(prog.RemarkList))));
        }

        
        private static IReadOnlyList<string> getAllCodeFiles(string rootPath, RemarkList remarkList)
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
                    throw remarkList.OlnyEfFilesAreSupported(rootPath);
                }
                files.AddValue(rootPath);
            }
            else
            {
                throw remarkList.CouldNotLocateFileOrFolder(rootPath);
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
            if (e is SequenceItem si)
                e = new Sequence(new List<SequenceItem> {si});
            if (e is Sequence body2)
                return new FnApply(
                    new Fn(new FnParameters(), body2),
                    new FnArguments());
            throw new NotSupportedException();
        }
    }
}