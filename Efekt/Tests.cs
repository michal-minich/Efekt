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
            error("~");
            test("1", "1");
            test("var a = 1 { var a = 2 return a }", "2");
            test("var a = 1 { var a = 2 } return a", "1");
            //test("var a = 1 { a = 2 } return a", "2");
            //test("var a = 1 { a = 2 } return a a = 3", "2");
            //test("var a = 1 { var a = 2 } return a", "1");
            //test("var a = 1 { var a = 2 } return a a = 3", "1");
            test("var x = fn { return 1_2_3 } return x()", "123");
        }

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

        // ReSharper disable once UnusedParameter.Local
        private static void test(string code, string expected)
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
            if (val != expected)
                throw new Exception();
        }
    }
}