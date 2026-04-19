using System;
using System.Diagnostics;
using UnityEngine.Assertions;

namespace Framwork.Utils
{
    public static class ExceptionUtils
    {
        [Conditional("UNITY_EDITOR")]
        public static void ThrowIfArgNull(object obj, string paramName)
        {
            if (obj == null) throw new ArgumentNullException(paramName);
        }

        [Conditional("UNITY_EDITOR")]
        public static void ThrowIfNull(object obj, string message)
        {
            if (obj == null) throw new NullReferenceException(message);
        }

        [Conditional("UNITY_EDITOR")]
        public static void ThrowIfTrue(bool flag, string message)
        {
            if (flag) throw new AssertionException(message, string.Empty);
        }

        [Conditional("UNITY_EDITOR")]
        public static void ThrowIfFalse(bool flag, string message)
        {
            if (!flag) throw new AssertionException(message, string.Empty);
        }
    }
}