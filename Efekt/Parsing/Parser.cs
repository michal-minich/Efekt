﻿using System.Collections.Generic;
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
        private readonly Stack<int> StartLineIndex = new Stack<int>();

        static readonly List<string> rightAssociativeOps = new List<string>
            { ":", "=" };

        static readonly Dictionary<string, int> opPrecedence
            = new Dictionary<string, int>
            {
                ["."] = 160,
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
                ["="] = 3,
            };


        private void markStart()
        {
            StartLineIndex.Push(Ti.LineIndex);
        }

        private void pop()
        {
            StartLineIndex.Pop();
        }

        private T post<T>(T element) where T : Element
        {
            element.FilePath = Ti.FilePath;
            element.LineIndex = StartLineIndex.Pop();
            return element;
        }


        internal Parser(Remark remark) : base (remark)
        {
            Parsers = new List<ParseElement>
            {
                ParseIdent,
                ParseInt,
                ParseChar,
                ParseText,
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
            markStart();
            var elb = ParseBracedList('}', false);
            if (elb == null)
                throw remark.Error.Fail();
            return post(new Sequence(elb.Items));
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
                throw remark.Error.Fail();
            if (endBrace != end)
                throw remark.Error.Fail();
            Ti.Next();
            var elb = ParseList(endBrace, isComaSeparated);
            if (Text[0] != end)
                throw remark.Error.Fail();
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
            markStart();
            Ti.Next();
            var s = ParseMandatorySequence();
            return post(new Loop(s));
        }


        [CanBeNull]
        private Fn ParseFn()
        {
            if (Text != "fn")
                return null;
            markStart();
            Ti.Next();
            var p = Text == "{" ? new FnParameters() : ParseFnParameters();
            var s = ParseMandatorySequence();
            if (s == null)
                throw remark.Error.Fail();
            return post(new Fn(p, s));
        }


        [CanBeNull]
        private Element ParseOpApply(Exp prev)
        {
            if (Type != TokenType.Op)
                return null;
            markStart();
            var opText = Text;
            Ti.Next();
            var ident = post(new Ident(opText, TokenType.Op));
            markStart();
            var second = ParseOne(false);
            if (opText == ".")
            {
                if (second is Ident i)
                    return post(new MemberAccess(prev, i));
                throw remark.Error.Fail();
            }
            var e2 = second as Exp;
            if (e2 == null)
                throw remark.Error.Fail();
            if (opText == "=")
                return post(new Assign(prev, e2));
            if (!prev.IsBraced
                && prev is FnApply fna
                && fna.Fn is Ident prevOp
                && prevOp.TokenType == TokenType.Op
                && opPrecedence[prevOp.Name] < opPrecedence[opText])
            {
                var x = post(new FnApply(ident, new FnArguments(new[] {fna.Arguments.Skip(1).First(), e2})));
                fna.Arguments = new FnArguments(new[] {fna.Arguments.First(), x});
                return fna;
            }
            return post(new FnApply(ident, new FnArguments(new[] {prev, e2})));
        }


        [CanBeNull]
        private FnApply ParseFnApply(Exp prev)
        {
            if (Text[0] != '(')
                return null;
            markStart();
            var args = ParseFnArguments(')');
            return post(new FnApply(prev, args));
        }


        [CanBeNull]
        private ArrConstructor ParseArr()
        {
            if (Text[0] != '[')
                return null;
            markStart();
            var args = ParseFnArguments(']');
            return post(new ArrConstructor(args));
        }


        [CanBeNull]
        private Element ParseSingleBraced()
        {
            if (Text[0] != '(')
                return null;
            markStart();
            var elb = ParseBracedList(')', false);
            if (elb.Items.Count == 1)
            {
                var e = post(elb.Items.First());
                e.IsBraced = true;
                return e;
            }
            throw remark.Error.Fail();
        }


        [CanBeNull]
        private New ParseNew()
        {
            if (Text != "new")
                return null;
            markStart();
            Ti.Next();
            var body = ParseClassBody();
            return post(new New(body));
        }


        [CanBeNull]
        private Break ParseBreak()
        {
            if (Text != "break")
                return null;
            markStart();
            Ti.Next();
            return post(new Break());
        }


        [CanBeNull]
        private Ident ParseIdent()
        {
            if (Type != TokenType.Ident && Type != TokenType.Op)
                return null;
            markStart();
            var i = post(new Ident(Text, Type));
            Ti.Next();
            return i;
        }


        [CanBeNull]
        private Int ParseInt()
        {
            if (Type != TokenType.Int)
                return null;
            markStart();
            var i = post(new Int(int.Parse(Text.Replace("_", ""))));
            Ti.Next();
            return i;
        }


        [CanBeNull]
        private Char ParseChar()
        {
            if (Type != TokenType.Char)
                return null;
            markStart();
            if (Text.Length != 3)
                throw remark.Error.Fail();
            var i = post(new Char(Text[1]));
            Ti.Next();
            return i;
        }


        [CanBeNull]
        private Text ParseText()
        {
            if (Type != TokenType.Text)
                return null;
            markStart();
            var i = post(new Text(Text.Substring(1, Text.Length - 2)));
            Ti.Next();
            return i;
        }


        [CanBeNull]
        private Bool ParseBool()
        {
            markStart();
            if (Text == "true")
            {
                Ti.Next();
                return post(new Bool(true));
            }
            else if (Text == "false")
            {
                Ti.Next();
                return post(new Bool(false));
            }
            pop();
            return null;
        }


        [CanBeNull]
        private Var ParseVar()
        {
            if (Text != "var")
                return null;
            markStart();
            Ti.Next();
            var se = ParseOne();
            if (se is Ident i2)
                return post(new Var(i2, Void.Instance));
            if (se is Assign a)
            {
                if (a.To is Ident i)
                    return post(new Var(i, a.Exp));
                throw remark.Error.AssignTargetIsInvalid(a.To);
            }
            throw remark.Error.Fail();
        }


        [CanBeNull]
        private Return ParseReturn()
        {
            if (Text != "return")
                return null;
            var lineIndexOnReturn = Ti.LineIndex;
            markStart();
            Ti.Next();
            if (Ti.Finished || lineIndexOnReturn != Ti.LineIndex)
                return post(new Return(Void.Instance));
            var se = ParseOne();
            if (se is Exp exp)
                return post(new Return(exp));
            throw remark.Error.Fail();
        }
        

        [CanBeNull]
        private When ParseWhen()
        {
            if (Text != "if")
                return null;
            markStart();
            Ti.Next();
            var test = ParseOne();
            var testExp = test as Exp;
            if (testExp == null)
                throw remark.Error.Fail();
            if (Text != "then")
                throw remark.Error.Fail();
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
            return post(new When(testExp, then, otherwise));
        }
    }
}