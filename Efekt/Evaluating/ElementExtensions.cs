namespace Efekt
{
    internal static class ElementExtensions
    {
        public static B As<B>(this Exp element, Remark remark, Exp inExp) where B : Exp
        {
            return element is B e ? e : throw remark.Error.DifferentTypeExpected(element, typeof(B).Name, inExp);
        }

        public static Int AsInt(this Exp exp, Remark remark, Exp inExp)
        {
            return exp.As<Int>(remark, inExp);
        }

        public static Arr AsArr(this Exp exp, Remark remark, Exp inExp)
        {
            return exp.As<Arr>(remark, inExp);
        }

        public static Value AsValue(this Exp exp, Remark remark, Exp inExp)
        {
            return exp.As<Value>(remark, inExp);
        }

        public static Bool AsBool(this Exp exp, Remark remark, Exp inExp)
        {
            return exp.As<Bool>(remark, inExp);
        }

        public static Fn AsFn(this Exp exp, Remark remark, Exp inExp)
        {
            return exp.As<Fn>(remark, inExp);
        }

        public static Obj AsObj(this Exp exp, Remark remark, Exp inExp)
        {
            return exp.As<Obj>(remark, inExp);
        }
    }
}