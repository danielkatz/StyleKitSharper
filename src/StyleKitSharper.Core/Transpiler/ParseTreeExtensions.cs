using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StyleKitSharper.Core.Transpiler
{
    public static class ParseTreeExtensions
    {
        public static IEnumerable<IParseTree> Ancestors(this IParseTree node)
        {
            for (var current = node; current.Parent != null; current = current.Parent)
            {
                yield return current;
            }
        }

        public static IEnumerable<IParseTree> Descendants(this IParseTree node)
        {
            for (int i = 0; i < node.ChildCount; i++)
            {
                yield return node.GetChild(i);

                var descendants = node.GetChild(i).Descendants();
                foreach (var descendant in descendants)
                {
                    yield return descendant;
                }
            }
        }
    }
}