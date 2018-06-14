using System;
using System.Collections.Generic;
using System.Linq;


namespace Efekt
{
    public static class ElementExtensions
    {
        private static IEnumerable<B> ManyAs<B>(
            this IEnumerable<Element> elements, 
            Func<Element, EfektException> remarkFn) 
            where B : class , Element
        {
            var list = new List<B>();
            foreach (var e in elements)
            {
                C.Assert(e != null);
                var b = e as B;
                if (b == null)
                    throw remarkFn(e);
                list.Add(b);
            }

            return list;
        }


        public static List<ClassItem> AsClassItems(this IEnumerable<Element> elements, RemarkList remarkList)
        {
            return ManyAs<ClassItem>(elements, remarkList.ExpectedClassElement).ToList();
        }


        public static List<SequenceItem> AsSequenceItems(this IEnumerable<Element> elements, RemarkList remarkList)
        {
            return ManyAs<SequenceItem>(elements, remarkList.ExpectedSequenceElement).ToList();
        }


        public static B As<B>(this Element element, Element subject, Prog prog) where B : class, Element
        {
            C.Nn(element, subject, prog);
            C.ReturnsNn();

            var e = element as B;
            if (e != null)
                return e;

            throw prog.RemarkList.ExpectedDifferentType(subject, element, typeof(B).Name);
        }


        public static Int AsInt(this Exp exp, Exp subject, Prog prog)
        {
            return exp.As<Int>(subject, prog);
        }


        public static Arr AsArr(this Exp exp, Exp subject, Prog prog)
        {
            return exp.As<Arr>(subject, prog);
        }


        public static Value AsValue(this Exp exp, Exp subject, Prog prog)
        {
            return exp.As<Value>(subject, prog);
        }


        public static Bool AsBool(this Exp exp, Exp subject, Prog prog)
        {
            return exp.As<Bool>(subject, prog);
        }


        public static Fn AsFn(this Exp exp, Exp subject, Prog prog)
        {
            return exp.As<Fn>(subject, prog);
        }


        public static Obj AsObj(this Exp exp, Element subject, Prog prog)
        {
            return exp.As<Obj>(subject, prog);
        }


        public static FnApply AsFnApply(this Element exp, Exp subject, Prog prog)
        {
            return exp.As<FnApply>(subject, prog);
        }


        public static string ToCodeString(this Element e)
        {
            var sw = new StringWriter();
            new Printer(new PlainTextCodeWriter(sw), false).Write(e);
            return sw.GetAndReset();
        }


        public static T CopyInfoFrom<T, O>(this T @new, O old, bool skipParent = false)
            where T : Element
            where O : Element
        {
            @new.LineIndex = old.LineIndex;
            @new.ColumnIndex = old.ColumnIndex;
            @new.LineIndexEnd = old.LineIndexEnd;
            @new.ColumnIndexEnd = old.ColumnIndexEnd;
            @new.FilePath = old.FilePath;
            @new.IsBraced = old.IsBraced;
            if (!skipParent && old.Parent != null)
                @new.Parent = old.Parent;
            return @new;
        }


        public static bool IsSimple(this Element e)
        {
            return Simpler.IsSimple(e);
        }
    }
}