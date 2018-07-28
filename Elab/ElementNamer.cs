using System;
using Efekt;
using Char = Efekt.Char;
using Void = Efekt.Void;

namespace Elab
{
    public static class ElementNamer
    {
        public static string Name(Element el)
        {
            switch (el)
            {
                case AnySpec anySpec:
                    return "Any Type";
                case ArrConstructor arrConstructor:
                    return "Array Literal";
                case Assign assign:
                    return "Assignment Expression";
                case Attempt attempt:
                    return "Try Statement";
                case Bool b:
                    return "Boolean Value";
                case BoolSpec boolSpec:
                    return "Boolean Type";
                case Break @break:
                    return "Break Statement";
                case Builtin builtin:
                    return "Builtin Function";
                case Continue @continue:
                    return "Continue Statement";
                case Fn fn:
                    return "Function Expression";
                case FnApply fnApply:
                    return "Function Application Expression";
                case FnSpec fnSpec:
                    return "Function Type";
                case Char c:
                    return "Character Value";
                case CharSpec charSpec:
                    return "Character Type";
                case Ident ident:
                    return (ident.TokenType == TokenType.Op ? "Operator " : "") + "Identifier";
                case Import import:
                    return "Import Statement";
                case Int i:
                    return "Integer Value";
                case IntSpec intSpec:
                    return "Integer Type";
                case Let @let:
                    return "Let Variable Declaration";
                case Loop loop:
                    return "Loop Statement";
                case MemberAccess memberAccess:
                    return "Object Member Access Expression";
                case New @new:
                    return "New Expression";
                case NotSetSpec notSetSpec:
                    return "Not Set Type";
                case Obj obj:
                    return "Object Value";
                case ObjSpec objSpec:
                    return "Object Type";
                case Param param:
                    return "Function Parameter Declaration";
                case Return @return:
                    return "Return Statement";
                case Sequence sequence:
                    return "Sequence";
                case Text text:
                    return "Text Value";
                case TextSpec textSpec:
                    return "Text Type";
                case Toss toss:
                    return "Throw Expression";
                case Var @var:
                    return "Variable Declaration";
                case Void @void:
                    return "Void Value";
                case VoidSpec voidSpec:
                    return "Void Type";
                case When wh:
                    return wh.Then.IsSimple() && wh.Otherwise != null && wh.Otherwise.IsSimple() ? "If Expression" : "If Statement";
                case Arr arr:
                    return "Array Value";
                case ArrSpec arrSpec:
                    return "Array Type";
                default:
                    throw new Exception();
            }
        }
    }
}