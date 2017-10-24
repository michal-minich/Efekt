using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    [CanBeNull]
    internal delegate Element ParseElement();


    [CanBeNull]
    internal delegate Element ParseOpElement(Exp prev);


    internal sealed class Parser : ElementIterator
    {
        internal Parser()
        {
            Parsers = new List<ParseElement>
            {
                ParseIdent,
                ParseInt,
                ParseBool,
                ParseVar,
                ParseFn,
                ParseWhen,
                ParseLoop,
                ParseBreak,
                ParseReturn,
                ParseSequence,
                ParseSingleBraced,
                ParseArr,
                ParseNew
            };

            OpOparsers = new List<ParseOpElement>
            {
                ParseFnApply,
                ParseOpApply
            };
        }
        

        private Sequence ParseMandatorySequence()
        {
            var elb = ParseBracedList('}', false);
            if (elb == null)
                throw Error.Fail();
            return new Sequence(elb.Items);
        }

        
        private ClassBody ParseClassBody()
        {
            return new ClassBody(ParseBracedList('}', false).Items.Cast<Var>().ToArray());
        }


        private FnArguments ParseFnArguments(char end)
        {
            return new FnArguments(ParseBracedList(end, true).Items.Cast<Exp>().ToArray());
        }


        private FnParameters ParseFnParameters()
        {
            return new FnParameters(ParseList('{', true).Items.Cast<Ident>().ToArray());
        }


        private ElementListBuilder ParseBracedList(char endBrace, bool isComaSeparated)
        {
            char end;
            var t = Text[0];
            if (t == '(')
                end = ')';
            else if (t == '{')
                end = '}';
            else if (t == '[')
                end = ']';
            else
                throw Error.Fail();
            if (endBrace != end)
                throw Error.Fail();
            Ti.Next();
            var elb = ParseList(endBrace, isComaSeparated);
            if (Text[0] != end)
                throw Error.Fail();
            Ti.Next();
            return elb;
        }

        private ElementListBuilder ParseList(char end, bool isComaSeparated)
        {
            var elb = new ElementListBuilder();
            while (Ti.HasWork)
            {
                if (Text[0] == end)
                    break;
                var e = ParseOne();
                elb.Add(e);
                if (isComaSeparated)
                {
                    if (Text == ",")
                        Ti.Next();
                    else
                        break;
                }
            }
            return elb;
        }


        [CanBeNull]
        private Sequence ParseSequence()
        {
            if (Text[0] != '{')
                return null;
            return ParseMandatorySequence();
        }


        [CanBeNull]
        private Loop ParseLoop()
        {
            if (Text != "loop")
                return null;
            Ti.Next();
            var s = ParseMandatorySequence();
            return new Loop(s);
        }


        [CanBeNull]
        private Fn ParseFn()
        {
            if (Text != "fn")
                return null;
            Ti.Next();
            var p = Text == "{" ? new FnParameters() : ParseFnParameters();
            var s = ParseMandatorySequence();
            if (s == null)
                throw Error.Fail();
            return new Fn(p, s);
        }


        [CanBeNull]
        private Element ParseOpApply(Exp prev)
        {
            if (Type != TokenType.Op)
                return null;
            var opText = Text;
            Ti.Next();
            var second = ParseOne(false);
            if (opText == ".")
            {
                if (second is Ident i)
                    return new MemberAccess(prev, i);
                throw Error.Fail();
            }
            var e2 = second as Exp;
            if (e2 == null)
                throw Error.Fail();
            if (opText == "=")
                return new Assign(prev, e2);
            return new FnApply(new Ident(opText, TokenType.Op), new FnArguments(new[] { prev, e2 }));
        }


        [CanBeNull]
        private FnApply ParseFnApply(Exp prev)
        {
            if (Text[0] != '(')
                return null;
            var args = ParseFnArguments(')');
            return new FnApply(prev, args);
        }


        [CanBeNull]
        private ArrConstructor ParseArr()
        {
            if (Text[0] != '[')
                return null;
            var args = ParseFnArguments(']');
            return new ArrConstructor(args);
        }


        [CanBeNull]
        private Element ParseSingleBraced()
        {
            if (Text[0] != '(')
                return null;
            var elb = ParseBracedList(')', false);
            if (elb.Items.Count == 1)
                return elb.Items.First();
            throw Error.Fail();
        }


        [CanBeNull]
        private New ParseNew()
        {
            if (Text != "new")
                return null;
            Ti.Next();
            var body = ParseClassBody();
            return new New(body);
        }


        [CanBeNull]
        private Break ParseBreak()
        {
            if (Text != "break")
                return null;
            Ti.Next();
            return Break.Instance;
        }


        [CanBeNull]
        private Ident ParseIdent()
        {
            if (Type != TokenType.Ident && Type != TokenType.Op)
                return null;
            var i = new Ident(Text, Type);
            Ti.Next();
            return i;
        }


        [CanBeNull]
        private Int ParseInt()
        {
            if (Type != TokenType.Int)
                return null;
            var i = new Int(int.Parse(Text.Replace("_", "")));
            Ti.Next();
            return i;
        }


        [CanBeNull]
        private Bool ParseBool()
        {
            switch (Text)
            {
                case "true":
                    Ti.Next();
                    return Bool.True;
                case "false":
                    Ti.Next();
                    return Bool.False;
                default:
                    return null;
            }
        }


        [CanBeNull]
        private Var ParseVar()
        {
            if (Text != "var")
                return null;
            Ti.Next();
            var se = ParseOne();
            if (se is Ident i2)
                return new Var(i2, Void.Instance);
            if (se is Assign a)
            {
                if (a.To is Ident i)
                    return new Var(i, a.Exp);
                throw Error.Fail();
            }
            throw Error.Fail();
        }


        [CanBeNull]
        private Return ParseReturn()
        {
            if (Text != "return")
                return null;
            var lineIndexOnReturn = Ti.LineIndex;
            Ti.Next();
            if (Ti.Finished || lineIndexOnReturn != Ti.LineIndex)
                return new Return(Void.Instance);
            var se = ParseOne();
            if (se is Exp exp)
                return new Return(exp);
            throw Error.Fail();
        }
        

        [CanBeNull]
        private When ParseWhen()
        {
            if (Text != "if")
                return null;
            Ti.Next();
            var test = ParseOne();
            var testExp = test as Exp;
            if (testExp == null)
                throw Error.Fail();
            if (Text != "then")
                throw Error.Fail();
            Ti.Next();
            var then = ParseOne();
            Element otherwise;
            if (Text == "else")
            {
                Ti.Next();
                otherwise = ParseOne();
            }
            else
            {
                otherwise = null;
            }
            return new When(testExp, then, otherwise);
        }
    }
}