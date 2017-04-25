using System;

namespace Efekt
{
    internal static class Tests
    {
        public static void RunAllTests()
        {
            test("return 1", "1");
        }

        private static void test(string code, string expected)
        {
            var t = new Tokenizer();
            var tokens = t.Tokenize(code);
            var p = new Parser();
            var se = p.Parse(tokens);
            var i = new Interpreter();
            var r = i.Eval(se);
            var sw = new StringWriter();
            var ctw = new CodeTextWriter(sw);
            CodeWriter.Write(r, ctw);
            var val = sw.GetAndReset();
            if (val != expected)
                throw new Exception();
        }
    }
}