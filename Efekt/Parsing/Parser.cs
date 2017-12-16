using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    [CanBeNull]
    public delegate Element ParseElement();


    public sealed class Parser
    {
        private readonly List<ParseElement> parsers;
        private TokenIterator ti;
        private string text => ti.Current.Text;
        private TokenType type => ti.Current.Type;
        private readonly RemarkList remarkList;


        private readonly Stack<int> startLineIndex = new Stack<int>();
        private readonly Stack<int> startColumnIndex = new Stack<int>();

        //private static readonly List<string> rightAssociativeOps = new List<string>
        //    {":", "="};

        private static readonly Dictionary<string, int> opPrecedence
            = new Dictionary<string, int>
            {
                ["."] = 160,
                ["("] = 150,
                //[":"] = 140,
                ["*"] = 130,
                ["/"] = 130,
                ["+"] = 120,
                ["-"] = 120,
                ["<"] = 100,
                [">"] = 100,
                [">="] = 100,
                ["<="] = 100,
                ["=="] = 60,
                ["!="] = 60,
                ["and"] = 20,
                ["or"] = 10,
                ["="] = 3
            };


        private void markStart()
        {
            startLineIndex.Push(ti.LineIndex);
            startColumnIndex.Push(ti.ColumnIndex);
        }


        private void markStart(Element firstInComplex)
        {
            startLineIndex.Push(firstInComplex.LineIndex);
            startColumnIndex.Push(firstInComplex.ColumnIndex);
        }


        private void pop()
        {
            startLineIndex.Pop();
            startColumnIndex.Pop();
        }


        private T post<T>(T element) where T : Element
        {
            element.FilePath = ti.FilePath;
            element.LineIndex = startLineIndex.Pop();
            element.ColumnIndex = startColumnIndex.Pop();
            element.LineIndexEnd = endLineIndex;//Ti.LineIndex;
            element.ColumnIndexEnd = endColumnIndex;//Ti.ColumnIndex;
            return element;
        }



        private int endLineIndex;
        private int endColumnIndex;
        private void next()
        {
            endLineIndex = ti.LineIndex;
            endColumnIndex = ti.ColumnIndex + text.Length;
            ti.Next();
        }


        public Parser(RemarkList remarkList)
        {
            this.remarkList = remarkList;
            parsers = new List<ParseElement>
            {
                ParseIdent,
                ParseInt,
                ParseChar,
                ParseText,
                ParseBool,
                ParseVarOrLet,
                ParseFn,
                ParseWhen,
                ParseLoop,
                ParseBreak,
                ParseContinue,
                ParseReturn,
                ParseThrow,
                ParseTry,
                ParseSequence,
                ParseSingleBraced,
                ParseArr,
                ParseNew,
                ParseImport
            };
        }


        private Sequence ParseMandatorySequence()
        {
            markStart();
            return post(new Sequence(ParseBracedList<SequenceItem>('{', false)));
        }


        private ClassBody ParseClassBody()
        {
            return new ClassBody(ParseBracedList<ClassItem>('{', false));
        }


        private FnArguments ParseFnArguments(char startBrace)
        {
            return new FnArguments(ParseBracedList<Exp>(startBrace, true));
        }


        private FnParameters ParseFnParameters()
        {
            return new FnParameters(ParseList<Ident>('{', true).Select(i => new Param(i)).ToList());
        }

        private bool crossedLine;
        private List<T> ParseBracedList<T>(char startBrace, bool isComaSeparated) where T : class, Element
        {
            char endBrace;
            if (startBrace == '(')
                endBrace = ')';
            else if (startBrace == '{')
                endBrace = '}';
            else if (startBrace == '[')
                endBrace = ']';
            else
                throw new NotSupportedException();

            if (text[0] != startBrace)
                throw remarkList.OpeningBraceIsExpected(ti, startBrace);
            next();
            var list = ParseList<T>(endBrace, isComaSeparated);
            if (text[0] != endBrace)
                throw remarkList.ClosingBraceDoesNotMatchesOpening(ti, startBrace, text);
            next();
            crossedLine = ti.CrossedLine;
            return list;
        }

        private List<T> ParseList<T>(char end, bool isComaSeparated) where T : class, Element
        {
            var list = new List<T>();
            while (ti.HasWork)
            {
                if (text[0] == end)
                    break;
                var e = ParseOne();
                var t = e as T;
                if (t == null)
                    throw remarkList.ExpectedDifferentElement(e, typeof(T));
                list.AddValue(t);
                if (isComaSeparated)
                    if (text == ",")
                        next();
                    else
                        break;
            }

            return list;
        }


        [CanBeNull]
        private Sequence ParseSequence()
        {
            if (text[0] != '{')
                return null;
            return ParseMandatorySequence();
        }


        [CanBeNull]
        private Loop ParseLoop()
        {
            if (text != "loop")
                return null;
            markStart();
            next();
            var s = ParseMandatorySequence();
            return post(new Loop(s));
        }


        [CanBeNull]
        private Fn ParseFn()
        {
            if (text != "fn")
                return null;
            markStart();
            next();
            var p = text == "{" ? new FnParameters() : ParseFnParameters();
            var s = ParseMandatorySequence();
            return post(new Fn(p, s));
        }

        public Element Parse(string filePath, IEnumerable<Token> tokens)
        {
            ti = new TokenIterator(filePath, tokens, remarkList);
            var list = new List<Element>();
            next();
            while (ti.HasWork)
            {
                var e = ParseOne();
                list.AddValue(e);
            }
            var first = list.FirstOrDefault();
            return list.Count == 1 && first is Exp 
                ? first 
                : new Sequence(list.AsSequenceItems(remarkList));
        }


        [NotNull]
        private Element ParseOne(bool withOps = true, bool withFn = true)
        {
            foreach (var p in parsers)
            {
                C.Assert(p != null);
                var e = p();
                if (e == null)
                    continue;
                if (withOps || text == ".")
                    e = ParseWithOp(e);
                if (withFn && !crossedLine && e is Exp exp)
                    e = ParseFnApply(exp);
                crossedLine = false;
                return e;
            }

            return ParseInvalid();
        }


        private Element ParseInvalid()
        {
            markStart();
            var t = text;
            next();
            return post(new Invalid(t));
        }


        private Element ParseWithOp(Element e)
        {
            if (e is Exp exp)
                e = ParseFnApply(exp);

            Exp prev;
            if (e is Exp e3)
                prev = e3;
            else if (e is Assign a)
                prev = a.Exp;
            else
                return e;
            
            var e2 = ParseOpApply(prev);
            if (e2 == null)
                return e;

            if (e is Assign aa)
            {
                if (e2 is Exp ee)
                    return ParseWithOp(new Assign(aa.To, ee));
                throw remarkList.OnlyExpressionCanBeAssigned(e2);
            }
            
            return ParseWithOp(e2);
        }


        [CanBeNull]
        private Element ParseOpApply(Exp prev)
        {
            var e = ParseOpApply1(prev);
            if (e is MemberAccess)
                return e;
            if (e is Exp exp)
            {
                var e2 = ParseFnApply(exp);
                return e2;
            }

            return e;
        }



        [CanBeNull]
        private Element ParseOpApply1(Exp prev)
        {
            if (type != TokenType.Op)
                return null;
            markStart();
            var opText = text;
            next();
            var ident = post(new Ident(opText, TokenType.Op));
            markStart(prev);
            var second = ParseOne(false, opText != ".");
            if (opText == ".")
            {
                if (second is Ident i)
                    return post(new MemberAccess(prev, i));
                throw remarkList.ExpectedIdentifierAfterDot(second);
            }
            var e2 = second as Exp;
            if (opText == "=")
            {
                if (e2 == null)
                    throw remarkList.OnlyExpressionCanBeAssigned(second);
                if (prev is AssignTarget at)
                    return post(new Assign(at, e2));
                throw remarkList.AssignTargetIsInvalid(prev);
            }
            if (e2 == null)
                throw remarkList.SecondOperandMustBeExpression(second);
            if (!prev.IsBraced
                && prev is FnApply fna
                && fna.Fn is Ident prevOp
                && prevOp.TokenType == TokenType.Op
                && opPrecedence[prevOp.Name] < opPrecedence[opText])
            {
                var firstArg = fna.Arguments.First();
                var secondArg = fna.Arguments.Skip(1).First();
                var x = post(new FnApply(ident, new FnArguments(new List<Exp> {secondArg, e2})));
                fna.Arguments = new FnArguments(new List<Exp> {firstArg, x});
                fna.LineIndex = firstArg.LineIndex;
                fna.ColumnIndex = firstArg.ColumnIndex;
                fna.LineIndexEnd = e2.LineIndexEnd;
                fna.ColumnIndexEnd = e2.ColumnIndexEnd;
                return fna;
            }
            var fna3 = post(new FnApply(ident, new FnArguments(new List<Exp> {prev, e2})));
            fna3.LineIndex = prev.LineIndex;
            fna3.ColumnIndex = prev.ColumnIndex;
            fna3.LineIndexEnd = e2.LineIndexEnd;
            fna3.ColumnIndexEnd = e2.ColumnIndexEnd;
            return fna3;
        }


        [NotNull]
        private Exp ParseFnApply(Exp prev)
        {
            if (crossedLine || text[0] != '(')
                return prev;
            markStart(prev);
            var args = ParseFnArguments('(');
            var fna = post(new FnApply(prev, args));
            return crossedLine ? fna : ParseFnApply(fna);
        }


        [CanBeNull]
        private ArrConstructor ParseArr()
        {
            if (text[0] != '[')
                return null;
            markStart();
            var args = ParseFnArguments('[');
            return post(new ArrConstructor(args));
        }


        [CanBeNull]
        private Element ParseSingleBraced()
        {
            if (text[0] != '(')
                return null;
            markStart();
            var list = ParseBracedList<Element>('(', false);
            if (list.Count == 1)
            {
                var e = post(list.First());
                e.IsBraced = true;
                return e;
            }
            throw remarkList.ExpectedOnlyOneExpressionInsideBraces(list);
        }


        [CanBeNull]
        private New ParseNew()
        {
            if (text != "new")
                return null;
            markStart();
            next();
            var body = ParseClassBody();
            return post(new New(body));
        }


        [CanBeNull]
        private Import ParseImport()
        {
            if (text != "import")
                return null;
            markStart();
            next();
            var e = ParseOne();
            if (e is QualifiedIdent qi)
                return post(new Import(qi));
            throw remarkList.ExpectedQualifiedIdentAfterImport(e);
        }


        [CanBeNull]
        private Break ParseBreak()
        {
            if (text != "break")
                return null;
            markStart();
            next();
            return post(new Break());
        }


        [CanBeNull]
        private Continue ParseContinue()
        {
            if (text != "continue")
                return null;
            markStart();
            next();
            return post(new Continue());
        }


        [CanBeNull]
        private Ident ParseIdent()
        {
            if (type != TokenType.Ident && type != TokenType.Op)
                return null;
            markStart();
            var i = new Ident(text, type);
            next();
            return post(i);
        }


        [CanBeNull]
        private Int ParseInt()
        {
            if (type != TokenType.Int)
                return null;
            markStart();
            var i = new Int(int.Parse(text.Replace("_", "")));
            next();
            return post(i);
        }


        [CanBeNull]
        private Char ParseChar()
        {
            if (type != TokenType.Char)
                return null;
            markStart();
            var txt = replaceEsapes(text);
            if (txt.Length != 3)
                throw remarkList.CharShouldHaveOnlyOneChar(ti);
            var ch = txt[1];
            next();
            return post(new Char(ch));
        }


        [CanBeNull]
        private Text ParseText()
        {
            if (type != TokenType.Text)
                return null;
            markStart();
            var txt = replaceEsapes(text);
            var t = txt.Substring(1, txt.Length - 2);
            next();
            return post(new Text(t));
        }

        private string replaceEsapes(string txt)
        {
            return txt.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t")
                .Replace("\\0", "\0").Replace("\\\'", "\'").Replace("\\\"", "\"").Replace("\\\\", "\\");

        }
        /*
            if (ch != 'n' && ch != 'r' && ch != 't' && ch != '0' && ch != '\'' && ch != '"'
                    && ch != '\\')
                    tokType = TokenType.Invalid;
           */

        [CanBeNull]
        private Bool ParseBool()
        {
            markStart();
            if (text == "true")
            {
                next();
                return post(new Bool(true));
            }
            if (text == "false")
            {
                next();
                return post(new Bool(false));
            }
            pop();
            return null;
        }


        [CanBeNull]
        private Element ParseVarOrLet()
        {
            bool isVar;
            if (text == "var")
                isVar = true;
            else if (text == "let")
                isVar = false;
            else
                return null;
            markStart();
            next();
            var se = ParseOne();
            if (se is Ident i2)
            {
                return post(isVar ? (Element)new Var(i2, Void.Instance) : new Let(i2, Void.Instance));
            }
            if (se is Assign a)
            {
                if (a.To is Ident i)
                    return post(isVar ? (Element) new Var(i, a.Exp) : new Let(i, a.Exp));
                throw remarkList.OnlyIdentifierCanBeDeclared(a.To);
            }
            throw remarkList.InvalidElementAfterVar(se);
        }


        [CanBeNull]
        private Return ParseReturn()
        {
            if (text != "return")
                return null;
            markStart();
            next();
            if (ti.Finished || ti.CrossedLine)
                return post(new Return(Void.Instance));
            var se = ParseOne();
            if (se is Exp exp)
                return post(new Return(exp));
            throw remarkList.ExpectedExpression(se);
        }


        [CanBeNull]
        private Toss ParseThrow()
        {
            if (text != "throw")
                return null;
            markStart();
            next();
            var se = ParseOne();
            if (se is Exp exp)
                return post(new Toss(exp));
            throw remarkList.ExpectedExpression(se);
        }


        [CanBeNull]
        private Attempt ParseTry()
        {
            if (text != "try")
                return null;
            markStart();
            next();
            var body = ParseMandatorySequence();

            Sequence grab;
            if (text == "catch")
            {
                next();
                grab = ParseMandatorySequence();
            }
            else
            {
                grab = null;
            }

            Sequence atLast;
            if (text == "finally")
            {
                next();
                atLast = ParseMandatorySequence();
            }
            else
            {
                atLast = null;
            }

            return post(new Attempt(body, grab, atLast));
        }


        [CanBeNull]
        private When ParseWhen()
        {
            if (text != "if")
                return null;
            markStart();
            next();
            var test = ParseOne();
            var testExp = test as Exp;
            if (testExp == null || testExp is Ident i && i.Name == "then")
                throw remarkList.ExpectedTestExpression(ti); // TODO the position is at testExp, should be at 'if'
            if (text != "then")
                throw remarkList.ExpectedWordThen(testExp);
            next();
            var then = ParseOne();
            Element otherwise;
            if (text == "else")
            {
                next();
                otherwise = ParseOne();
            }
            else
            {
                otherwise = null;
            }
            return post(new When(testExp, then, otherwise));
        }
    }
}