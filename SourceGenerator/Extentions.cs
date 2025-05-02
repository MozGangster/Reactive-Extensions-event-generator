using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace RxSourceGenerator
{
    public static class Extentions
    {
        public static IEnumerable<T> GetMembersOfType<T>(this ITypeSymbol typeSymbol)
        {
            while (true)
            {
                foreach (var c in typeSymbol.GetMembers().OfType<T>()) yield return c;
                if (typeSymbol.BaseType == null) yield break;
                typeSymbol = typeSymbol.BaseType;
            }
        }
    }
}