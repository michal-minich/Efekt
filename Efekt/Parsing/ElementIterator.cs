using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Efekt
{
    public abstract class ElementIterator
    {
        protected Func<Element, Element> ParseOpApplyFn;
        protected List<ParseElement> Parsers;
        protected TokenIterator Ti;
        protected string Text => Ti.Current.Text;
        protected TokenType Type => Ti.Current.Type;
        protected readonly RemarkList RemarkList;

        public ElementIterator(RemarkList remarkList)
        {
            RemarkList = remarkList;
        }

        public Element Parse(string filePath, IEnumerable<Token> tokens)
        {
            Ti = new TokenIterator(filePath, tokens);
            var elb = new ElementListBuilder();
            Ti.Next();
            while (Ti.HasWork)
            {
                var e = ParseOne();
                elb.Add(e);
            }
            var first = elb.Items.FirstOrDefault();
            return first is Exp ? first : new Sequence(elb.Items.Cast<SequenceItem>().ToList());
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
                    e = ParseOpApplyFn(e);
                return e;
            }
            throw new Exception();
        }
    }
}