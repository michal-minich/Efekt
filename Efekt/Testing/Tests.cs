using System;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Efekt.Tests
{
    public static class Tests
    {
        [Conditional("DEBUG")]
        public static void RunAllTests()
        {
            //error("~");
            test(" ", "<Void>");
            test("1", "1");
            test("(1)", "1");

            // return
            test("return 1", "1");

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

            // basic ops and braces
            test("(1 + 2) * 10", "30");
            test("10 * (1 + 2)", "30");
            test("(1 + (2 * 10))", "21");
            test("(10 * 1) + 2", "12");

            // member access
            var obj1 = "var o = new { var a = 1 var b = new { var c = ' ' var c2 = fn { new { var d = 4 var d2 = fn { 5 } } } } } return ";
            test(obj1 + "o", "<Obj>");
            test(obj1 + "o.b", "<Obj>");
            test(obj1 + "(o.b).c", " ");
            test(obj1 + "(o.b).c2", "fn { new {var d = 4\r\nvar d2 = fn { 5 }\r\n} }");
            test(obj1 + "((o.b).c2()).d", "4");
            test(obj1 + "((o.b).c2()).d2", "fn { 5 }");
            test(obj1 + "((o.b).c2()).d2()", "5");

            // op precedence
            test("1 + 2 * 10", "21");
            test("0 * 1 + 2", "2");
            test("1 + 2 * 10 + 7", "28");
            test("2 + 3 * 5 + 7 * 11", "94");
            test("2 * 3 + 5 * 7 + 11", "52");
            test("2 * 3 + 5 + 7", "18");
            test("2 + 3 * 5 * 7", "107");
            test("2 + 3 + 5 * 7 + 11 + 13 * 17", "272");
            test("2 * 3 * 5 + 7 * 11 * 13 + 17", "1048");
            test("var a = 1 + 2 * 10 return a", "21");
            test("var a = 0 * 1 + 2 return a", "2");
            test("var a = new { var b = 1 } a.b = 1 + 2 * 10 return a.b", "21");
            test("var a = new { var b = 1 } a.b = 0 * 1 + 2 return a.b", "2");

            // fn apply after op or dot
            test("var id = fn a { a } return id(1) + id(2)", "3");
            test("var f = fn a { fn { a } } return f(1)() + f(2)()", "3");
            test("var f = fn { fn a { a } } return f()(1) + f()(2)", "3");
            test("var f = fn a { fn b { a + b } } return f(1)(10) + f(100)(1000)", "1111");
            test("var o = new { var f = fn a { fn b { a + b } } } return o.f(1)(10) + o.f(100)(1000)", "1111");

            // fn apply & braced & curly
            //test("var p = fn a { print(a) }\r\n(1).p()", "<Void>", "1"/*, false*/);
            //test("var p = fn a { print(a) }\r\n(1).p()\r\n(2).p()", "<Void>", "12"/*, false*/);

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
            test("let a = + return a(1, 2)", "3");
            test("let a = (+) return a(1, 2)", "3");
            test("let + = fn a, b { a * b } return 3 + 2", "6");
            test("print(1)", "<Void>", "1");

            // fn
            test("var a = fn { 1 } return a()", "1");
            test("var a = fn b { b } return a(1)", "1");
            test("var a = fn a { a } return a(1)", "1");
            test("var b = 2 var a = fn b { b } return a(1)", "1");
            test("var x = fn a, b { a } return x(1, 2)", "1");
            test("var x = fn a, b { b } return x(1, 2)", "2");
            test("fn { return 1 }()", "1");
            test("fn a { return a }(1)", "1");
            test("fn a, b { return b }((1), (2))", "2");
            test("fn a { return fn b { a + b } }(1)(2)", "3");

            // lambda
            const string tt = "fn tt { return fn y { return tt } }";
            const string ff = "fn ff { return fn y { return y } }";
            const string tr = "var t = " + tt;
            const string f = "var f = " + ff;
            const string and = " var andX = fn p { return fn q { return p(q)(p) } }";
            const string or = " var orX = fn p { return fn q { return p(p)(q) } }";
            const string ifthen = " var ifthen = fn p { return fn a { return fn b { return p(a)(b) } } }";
            const string not = " var not = fn b { return ifthen(b)(f)(t) }";
            const string bools = tr + f + and + or + ifthen + not + " return ";
            test(bools + "t", tt);
            test(bools + "andX(t)(t)", tt);
            test(bools + "andX(t)(f)", ff);
            test(bools + "andX(f)(t)", ff);
            test(bools + "andX(f)(f)", ff);
            test(bools + "orX(t)(t)", tt);
            test(bools + "orX(t)(f)", tt);
            test(bools + "orX(f)(t)", tt);
            test(bools + "orX(f)(f)", ff);
            test(bools + "not(t)", ff);
            test(bools + "not(f)", tt);
            test(bools + "not(andX(t)(f))", tt);
            test(bools + "not(orX(t)(f))", ff);
            test(bools + "andX(not(t))(not(f))", ff);
            test(bools + "orX(not(t))(not(f))", tt);

            // closure
            const string adder =
                "var adder = fn a { var state = a return fn { state = (state + 1) return state } }";
            test(adder + " var a = adder(10) a() return a()", "12");
            test(adder + " var a = adder(10) var b = adder(100) a() b() return a()", "12");
            test(adder + " var a = adder(10) var b = adder(100) b() a() return b()", "102");

            // comments
            test("/**/", "<Void>");
            test("//", "<Void>");
            test("//1", "<Void>");
            test("// 1", "<Void>");
            test("/**/ 1", "1");
            test("/**/1", "1");
            test("/**/1/*2*///3", "1");
            test("var a = 1 return /*2*/ a", "1");
            test("return //\n  1 + 1", "<Void>");
            test("var a = 1 return /*a*/ 2", "2");
            test("var /*a = 1*/a = 2  return a", "2");
            test("var a = //1\n2 return a", "2");
            test("// return  1", "<Void>");
            test("var a = 1 return a /**/", "1");
            test("//print(1)\r\n//print(2)", "<Void>");

            // array
            test("var c = 1 + 2 return [1, 2, c]", "[1, 2, 3]");
            test("var c = 1 + 2 return [c + 1, c + 1, c + 1]", "[4, 4, 4]");

            // objects
            test("var a = new { var a = 1 } return a.a", "1");
            test("var a = new { var a = 1 } a.a = a.a + 2 return a.a", "3");
            test("var a = new { var a = 1 var b = 3} return a.a + a.b", "4");
            test("var a = new { var a = 1 } var b = new { var b = 3 } a.a = a.a + b.b return a.a", "4");
            test("var mk = fn (v) { new { var a = v } } var a = mk(1) var b = mk(3) a.a = 4 return a.a + b.a", "7");

            // import

            // types
            RunTypeTests();
            
            Console.WriteLine("All tests executed");
        }


        private static void RunTypeTests()
        {
            // simple literals
            type("1", "Int");
            type("'a'", "Char");
            type("true", "Bool");

            // text literal
            type("\"abc123\"", "Text");
            type("\"abc\n123\"", "Text");
            type("\"abc\\n123\"", "Text");

            // fn literal
            type("fn { }", "Fn() -> Void");
            type("fn { 1 }", "Fn() -> Int");
            type("fn { return 1 }", "Fn() -> Int");
            //type("fn a { }", "Fn(Any) -> Void"); // Fn(Unknown) -> Void
            type("fn a { a + 1 }", "Fn(Int) -> Int");
            type("fn a { return a + 1 }", "Fn(Int) -> Int");
            type2("var id = fn a { a } typeof(id)", "Fn(Any) -> Any");
            type2("var id = fn a { a } typeof(id(1))", "Any");

            // fn application
            type2("var id = fn a { a } var int1 = 1 typeof(id(int1))", "Any");
            type2("var a = fn a { a + 1 } typeof(a(1))", "Int");

            // array literal
            type("[1 + 1]", "Arr(Int)");
            type("[1, 2, 3, 4]", "Arr(Int)");
            type2("var i = 1 typeof([i])", "Arr(Int)");
            type2("var i = 1 typeof([i + 1])", "Arr(Int)");
            type("[fn a { return a + 1 }]", "Arr(Fn(Int) -> Int)");
            type2("var id = fn a { a } typeof([id(1)])", "Arr(Any)");

            // object literal
            type2("var i = 1 typeof(new { var f = i })", "Obj(f : Int)");

            // change type from void
            type2("var void\n typeof(void)", "Void");
            type2("var b\n b = true typeof(b)", "Bool");

            // based on usage in 'if'
            type("fn (b) { if b then 1 else 2 }", "Fn(Bool) -> Int");
            type("fn { var a a = 1 return a}", "Fn() -> Int");

            // operators
            type("1 + 2", "Int");
            type("1 < 2", "Bool");

            // passing type between identifiers
            type2("var i = 1 var i2 = i typeof(i)", "Int");
            type2("var i = 1 typeof(i + 2)", "Int");

            // member access
            // type2("fn a { var x = a.b typeof(a) }", "Obj(b : Any)"); // Void
            // type2("fn a { a.b = 1 typeof(a) }", "Obj(b : Int)"); // Void

            // back transitive type
            //type("fn a { var b = a b = b + 1 return a }", "Fn(Int) -> Int");
        }


        private static void type(
            string code,
            string expectedResult)
        {
            type2("typeof(" + code + ")", expectedResult);
        }


        private static void type2(
            string code,
            string expectedResult)
        {
            test(code, "<Void>", expectedResult + "\r\n", true);
        }

        /*
        private static void error(string code)
        {
            
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
                throw Error.Fail();
           
        }
        */


        // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
        private static void test(
                string code,
                string expectedResult,
                string expectedOutput = "",
                bool checkTypes = true)
            // ReSharper restore ParameterOnlyUsedForPreconditionCheck.Local
        {
            var outputWriter = new StringWriter();
            var errorWriter = new StringWriter();
            var prog = Prog.Init(outputWriter, errorWriter, "unittest.ef", code, checkTypes);
            var res = prog.Interpreter.Eval(prog);
            var val = res.ToDebugString();
            if (val != expectedResult)
                throw new Exception();
            var acutalOutput = outputWriter.GetAndReset();
            if (acutalOutput != expectedOutput)
                throw new Exception();
        }
    }
}