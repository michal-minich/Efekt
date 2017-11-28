namespace Efekt
{
    public static class ElementExtensions
    {
        public static B As<B>(this Element element, Element inExp, Prog prog) where B : Exp
        {
            return element is B e
                ? e
                : throw prog.RemarkList.Except.DifferentTypeExpected(element, typeof(B).Name, inExp);
        }

        public static Int AsInt(this Exp exp, Exp inExp, Prog prog)
        {
            return exp.As<Int>(inExp, prog);
        }

        public static Arr AsArr(this Exp exp, Exp inExp, Prog prog)
        {
            return exp.As<Arr>(inExp, prog);
        }

        public static Value AsValue(this Exp exp, Exp inExp, Prog prog)
        {
            return exp.As<Value>(inExp, prog);
        }

        public static Bool AsBool(this Exp exp, Exp inExp, Prog prog)
        {
            return exp.As<Bool>(inExp, prog);
        }

        public static Fn AsFn(this Exp exp, Exp inExp, Prog prog)
        {
            return exp.As<Fn>(inExp, prog);
        }

        public static Obj AsObj(this Exp exp, Element inExp, Prog prog)
        {
            return exp.As<Obj>(inExp, prog);
        }

        public static FnApply AsFnApply(this Element exp, Exp inExp, Prog prog)
        {
            return exp.As<FnApply>(inExp, prog);
        }

        public static string ToDebugString(this Element e)
        {
            var sw = new StringWriter();
            new Printer(new PlainTextCodeWriter(sw), false).Write(e);
            return sw.GetAndReset();
        }
    }
}