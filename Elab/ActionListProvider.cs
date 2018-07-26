using System;
using System.Collections.Generic;
using Efekt;
using Char = Efekt.Char;
using Void = Efekt.Void;

namespace Elab
{
    public interface IElementAction
    {
        string Name { get; }
        bool CanIvoke(Element element);
        void Invoke(Element element);
    }


    public class RemoveAction : IElementAction
    {
        public string Name => "Remove";

        public bool CanIvoke(Element element)
        {
            return element.Parent is Sequence;
        }


        public void Invoke(Element element)
        {
            var p = (Sequence)element.Parent;
            var el = (SequenceItem)element;
            p.Remove(el);
        }
    }


    public static class ActionListProvider
    {
        public static IReadOnlyList<IElementAction> GetAvailableActions(Element el)
        {
            var remove = new RemoveAction();
            var noAction = new List<IElementAction>();

            switch (el)
            {
                case AnySpec anySpec:
                    return noAction;
                case ArrConstructor arrConstructor:
                    return new [] { remove };
                case Assign assign:
                    return new [] { remove };
                case Attempt attempt:
                    return new [] { remove };
                case Bool b:
                    return new [] { remove };
                case BoolSpec boolSpec:
                    return noAction;
                case Break @break:
                    return new [] { remove };
                case Builtin builtin:
                    return new [] { remove };
                case Continue @continue:
                    return new [] { remove };
                case Fn fn:
                    return new [] { remove };
                case FnApply fnApply:
                    return new [] { remove };
                case FnSpec fnSpec:
                    return noAction;
                case Char c:
                    return new [] { remove };
                case CharSpec charSpec:
                    return noAction;
                case Ident ident:
                    return new[] { remove };
                case Import import:
                    return new [] { remove };
                case Int i:
                    return new [] { remove };
                case IntSpec intSpec:
                    return noAction;
                case Let @let:
                    return new [] { remove };
                case Loop loop:
                    return new [] { remove };
                case MemberAccess memberAccess:
                    return new [] { remove };
                case New @new:
                    return new [] { remove };
                case NotSetSpec notSetSpec:
                    return new [] { remove };
                case Obj obj:
                    return new [] { remove };
                case ObjSpec objSpec:
                    return noAction;
                case Param param:
                    return new [] { remove };
                case Return @return:
                    return new [] { remove };
                case Sequence sequence:
                    return new [] { remove };
                case Text text:
                    return new [] { remove };
                case TextSpec textSpec:
                    return noAction;
                case Toss toss:
                    return new [] { remove };
                case Var @var:
                    return new [] { remove };
                case Void @void:
                    return new [] { remove };
                case VoidSpec voidSpec:
                    return noAction;
                case When @when:
                    return new [] { remove };
                case Arr arr:
                    return new [] { remove };
                case ArrSpec arrSpec:
                    return noAction;
                default:
                    throw new Exception();
            }
        }
    }
}