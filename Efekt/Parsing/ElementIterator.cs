using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Efekt
{
    internal abstract class ElementIterator
    {
        protected List<ParseOpElement> OpOparsers;
        protected List<ParseElement> Parsers;
        protected TokenIterator Ti;

        protected string Text => Ti.Current.Text;
        protected TokenType Type => Ti.Current.Type;
        protected readonly RemarkList remarkList;

        public ElementIterator(RemarkList remarkList)
        {
            this.remarkList = remarkList;
        }

        internal Element Parse(string filePath, IEnumerable<Token> tokens)
        {
            Ti = new TokenIterator(filePath, tokens);
            var elb = new ElementListBuilder();
            Ti.Next();
            while (Ti.HasWork)
            {
                var e = ParseOne();
                elb.Add(e);
            }
            var seq = new Sequence(elb.Items);
            return seq.Count == 1 ? seq[0] : seq;
        }


        [NotNull]
        protected Element ParseOne(bool withOps = true)
        {
            foreach (var p in Parsers)
            {
                C.Nn(p);
                var e = p();
                if (e == null)
                    continue;
                if (withOps)
                    e = parseWithOp(e);
                return e;
            }
            throw new Exception();
        }

        private Element parseWithOp(Element e)
        {
            if (Ti.Finished)
                return e;
            Exp prev;
            if (e is Exp e3)
                prev = e3;
            else if (e is Assign a)
                prev = a.Exp;
            else
                return e;
            foreach (var opar in OpOparsers)
            {
                var e2 = opar(prev);
                if (e2 == null)
                    continue;
                if (e is Assign a)
                {
                    if (e2 is Exp ee)
                        return parseWithOp(new Assign(a.To, ee));
                    throw remarkList.StructureValidator.SecondOperandMustBeExpression(e2);
                }
                return parseWithOp(e2);
            }
            return e;
        }
    }
}