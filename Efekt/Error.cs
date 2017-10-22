using System;
using JetBrains.Annotations;

namespace Efekt
{
    internal static class Error
    {
        [Pure]
        internal static Exception Fail()
        {
            return new Exception();
        }
    }
}
