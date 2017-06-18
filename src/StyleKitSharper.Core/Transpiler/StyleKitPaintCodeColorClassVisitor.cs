using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static JavaParser;
using Humanizer;
using System.Text.RegularExpressions;
using StyleKitSharper.Core.Properties;

namespace StyleKitSharper.Core.Transpiler
{
    public class StyleKitPaintCodeColorClassVisitor : StyleKitVisitor
    {
        private static readonly string[] PaintCodeColorClassMembers = new string[] {
            "colorByBlendingColors",
            "colorByChangingAlpha",
        };

        public StyleKitPaintCodeColorClassVisitor(StyleKitVisitor parent)
            : base(parent) { }

        protected override Func<bool> ResolveNodeVisitor(IParseTree node)
        {
            switch (node)
            {
                case PrimaryContext ctx:
                    return () => VisitPrimaryContext(ctx);
            }
            return base.ResolveNodeVisitor(node);
        }

        protected override bool VisitClassDeclaration(ClassDeclarationContext ctx)
        {
            var className = ctx.children[1];
            if (className.GetText() == "PaintCodeColor")
            {
                var extendsToken = (TerminalNodeImpl)ctx.children[2];
                var colorClass = (TypeTypeContext)ctx.children[3];
                _rewriter.Delete(extendsToken.Symbol);
                _rewriter.Delete(colorClass.Start);
            }

            return true;
        }

        private bool VisitPrimaryContext(PrimaryContext ctx)
        {
            var nodeText = ctx.GetText();

            if (PaintCodeColorClassMembers.Contains(nodeText))
            {
                _rewriter.Replace(ctx.Start, ctx.Stop, nodeText.Pascalize());
            }
            else if (ColorClassMethods.ContainsKey(nodeText))
            {
                _rewriter.Replace(ctx.Start, ctx.Stop, $"Color.{ColorClassMethods[nodeText]}");
            }
            else if (JavaConstantConventionRegex.IsMatch(nodeText))
            {
                _rewriter.Replace(ctx.Start, ctx.Stop, $"Color.{nodeText.ToLowerInvariant().Pascalize()}");
            }

            return true;
        }
    }
}
