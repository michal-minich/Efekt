using System;
using System.Linq;

namespace Efekt
{
    internal static class Tests
    {
        private static readonly Tokenizer t = new Tokenizer();
        private static readonly Parser p = new Parser();
        private static readonly Interpreter i = new Interpreter();
        private static readonly StringWriter sw = new StringWriter();
        private static readonly CodeTextWriter ctw = new CodeTextWriter(sw);
        private static readonly CodeWriter cw = new CodeWriter(ctw);

        public static void RunAllTests()
        {
            //test("var a = + return a(1, 2)", "3");
            
            error("~");
            test("1", "1");
            test("(1)", "1");

            // var
            test("var a = 1 return a", "1");

            // assign
            test("var a = 1 a = 2 return a", "2");

            // scope
            test("var a = 1 { var a = 2 return a }", "2");
            test("var a = 1 { var a = 2 } return a", "1");
            test("var a = 1 { a = 2 } return a", "2");
            test("var a = 1 { a = 2 } return a a = 3", "2");
            test("var a = 1 { var a = 2 } return a", "1");
            test("var a = 1 { var a = 2 } return a a = 3", "1");
            test("var x = fn { return 1_2_3 } return x()", "123");
            test("var a = 1 var b = 2 { a = 3 b = a } return b", "3");

            // if
            test("if true then 1 else 2", "1");
            test("if false then 1 else 2", "2");

            // loop
            test("loop { break } return 1", "1");
            test("var a = 1 loop { a = 2 break (a = 3) } return a", "2");
            test("var a = 1 var b = false loop { if b then break a = 2 b = true } return a", "2");

            // builtins
            test("1 + 2", "3");
            test("(1 + 2)", "3");
            test("var a = + return a(1, 2)", "3");
            test("var a = (+) return a(1, 2)", "3");
            test("print(1)", "<Void>", "Int: 1");

            // fn
            test("var a = fn { return 1 } return a()", "1");
            test("var a = fn b { return b } return a(1)", "1");
            test("var a = fn a { return a } return a(1)", "1");
            test("var b = 2 var a = fn b { return b } return a(1)", "1");
            test("var x = fn a, b { return a } return x(1, 2)", "1");
            test("var x = fn a, b { return b } return x(1, 2)", "2");
            test("fn { return 1 }()", "1");
            test("fn a { return a }(1)", "1");
            test("fn a, b { return b }((1), (2))", "2");
            test("fn a { return fn b { return a + b } }(1)(2)", "3");

            // labda
            const string t = "var t = fn tt { return fn y { return tt } }";
            const string f = " var f = fn ff { return fn y { return y } }";
            const string and = " var andX = fn p { return fn q { return p(q)(p) } }";
            const string or = " var orX = fn p { return fn q { return p(p)(q) } }";
            const string ifthen = " var ifthen = fn p { return fn a { return fn b { return p(a)(b) } } }";
            const string not = " var not = fn b { return ifthen(b)(f)(t) }";
            const string bools = t + f + and + or + ifthen + not + " return ";
            test(bools + "t", removeVar(t));
            test(bools + "andX(t)(t)", removeVar(t));
            test(bools + "andX(t)(f)", removeVar(f));
            test(bools + "andX(f)(t)", removeVar(f));
            test(bools + "andX(f)(f)", removeVar(f));
            test(bools + "orX(t)(t)", removeVar(t));
            test(bools + "orX(t)(f)", removeVar(t));
            test(bools + "orX(f)(t)", removeVar(t));
            test(bools + "orX(f)(f)", removeVar(f));
            test(bools + "not(t)", removeVar(f));
            test(bools + "not(f)", removeVar(t));
            test(bools + "not(andX(t)(f))", removeVar(t));
            test(bools + "not(orX(t)(f))", removeVar(f));
            test(bools + "andX(not(t))(not(f))", removeVar(f));
            test(bools + "orX(not(t))(not(f))", removeVar(t));

            // closure
            const string adder =
                "var adder = fn a { var state = a return fn { state = (state + 1) return state } }";
            test(adder + " var a = adder(10) a() return a()", "12");
            test(adder + " var a = adder(10) var b = adder(100) a() b() return a()", "12");
            test(adder + " var a = adder(10) var b = adder(100) b() a() return b()", "102");
        }

        static string removeVar(string t) => t.SubstringAfter("= ");

        private static void error(string code)
        {
            return;
            Exception e = null;
            try
            {
                test(code, null);
            }
            catch (Exception ex)
            {
                e = ex;
            }
            if (e == null)
                throw new Exception();
        }

        
        // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
        private static void test(string code, string expectedResult, string expectedOutput = "")
        // ReSharper restore ParameterOnlyUsedForPreconditionCheck.Local
        {
            var tokens = t.Tokenize(code).ToList();
            if (!(tokens.Count == 0 && code.Length == 0
                  || tokens.Count > 1 && code.Length > 0
                  || tokens.Count == 1 && code.Length == 1))
                throw new Exception();
            var se = p.Parse(tokens);
            var r = i.Eval(se);
            cw.Write(r);
            var val = sw.GetAndReset();
            if (val != expectedResult)
                throw new Exception();
            var acutalOutput = Builtins.Writer.GetAndReset();
            if (acutalOutput != expectedOutput)
                throw new Exception();
        }
    }
}