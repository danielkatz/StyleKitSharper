using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static JavaParser;
using Humanizer;
using System.Text.RegularExpressions;

namespace StyleKitSharper.Web.Transpiler
{
    public class StyleKitVisitor
    {
        private static readonly string[] usings = new string[] {
            "System",
            "System.Linq",
            "Android.Graphics",
            "System.Collections.Generic",
        };

        private readonly TokenStreamRewriter _rewriter;

        public StyleKitVisitor(ITokenStream tokens)
        {
            _rewriter = new TokenStreamRewriter(tokens);
        }

        public void Visit(IParseTree node)
        {
            switch (node)
            {
                case CompilationUnitContext ctx:
                    VisitCompilationUnit(ctx);
                    break;
                case ClassDeclarationContext ctx:
                    VisitClassDeclaration(ctx);
                    break;
                case ClassBodyDeclarationContext ctx:
                    VisitClassBodyDeclaration(ctx);
                    break;
                case MethodDeclarationContext ctx:
                    VisitMethodDeclaration(ctx);
                    break;
                case FieldDeclarationContext ctx:
                    VisitFieldDeclaration(ctx);
                    break;
                case TypeTypeContext ctx:
                    VisitTypeType(ctx);
                    break;
                case ExpressionContext ctx:
                    VisitExpression(ctx);
                    break;
                default:
                    break;
            }

            for (int i = 0; i < node.ChildCount; i++)
            {
                Visit(node.GetChild(i));
            }
        }

        private void VisitCompilationUnit(CompilationUnitContext ctx)
        {
            var package = ctx.packageDeclaration();
            if (package != null)
            {
                var qualifiedName = package.qualifiedName().GetText();

                _rewriter.Replace(package.Start, package.Stop, $"namespace {qualifiedName} {{");
                _rewriter.InsertAfter(ctx.Stop, "\n}");
            }

            var imports = ctx.importDeclaration();
            if (imports.Length > 0)
            {
                _rewriter.Replace(
                    imports.First().Start,
                    imports.Last().Stop,
                    string.Join("\n", usings.Select(x => $"using {x};")));
            }
        }

        private void VisitClassDeclaration(ClassDeclarationContext ctx)
        {
            var className = ctx.children[1];

            if (className.GetText() == "PaintCodeColor")
            {
                var extendsToken = (TerminalNodeImpl)ctx.children[2];
                var colorClass = (TypeTypeContext)ctx.children[3];
                _rewriter.Delete(extendsToken.Symbol);
                _rewriter.Delete(colorClass.Start);
            }
        }

        private void VisitClassBodyDeclaration(ClassBodyDeclarationContext ctx)
        {
            var privateModifier = ctx.modifier().FirstOrDefault(x => x.GetText() == "private");
            var publicModifier = ctx.modifier().FirstOrDefault(x => x.GetText() == "public");
            var staticModifier = ctx.modifier().FirstOrDefault(x => x.GetText() == "static");
            if (publicModifier == null)
            {
                if (privateModifier == null)
                {
                    _rewriter.InsertBefore(ctx.Start, "internal ");
                }
                else if (staticModifier != null)
                {
                    _rewriter.Replace(privateModifier.Start, privateModifier.Stop, "internal");
                }
            }
        }

        private void VisitMethodDeclaration(MethodDeclarationContext ctx)
        {
            var methodNameNode = ctx.children.OfType<TerminalNodeImpl>()
                .Where(x => x.Symbol.Type == Identifier).Single();

            _rewriter.Replace(methodNameNode.Symbol, methodNameNode.GetText().Pascalize());
        }

        private void VisitFieldDeclaration(FieldDeclarationContext ctx)
        {
            var fieldNameNodes = ctx.Descendants().OfType<VariableDeclaratorIdContext>().ToList();

            foreach (var node in fieldNameNodes)
            {
                var identifier = node.Identifier();
                _rewriter.Replace(identifier.Symbol, identifier.GetText().Pascalize());
            }
        }

        private void VisitTypeType(TypeTypeContext ctx)
        {
            var primitiveType = ctx.primitiveType();
            if (primitiveType != null)
            {
                if (primitiveType.GetText() == "boolean")
                {
                    _rewriter.Replace(primitiveType.Start, primitiveType.Stop, "bool");
                }
            }
        }

        private void VisitExpression(ExpressionContext ctx)
        {
            //if (ctx.children.Count >= 3)
            //{
            //    if (ctx.children[0] is ExpressionContext leftExpression
            //        && ctx.children[1].GetText() == "(")
            //    {
            //        if ((ctx.children[2].GetText() == ")")
            //            || (ctx.children[2] is ExpressionListContext && ctx.children[3].GetText() == ")"))
            //        {
            //            var identifier = leftExpression.Identifier();
            //            if (identifier != null)
            //            {
            //                _rewriter.Replace(identifier.Symbol, identifier.GetText().Pascalize());
            //            }
            //        }
            //    }
            //}
            var identifier = ctx.Identifier();
            if (identifier != null)
            {
                _rewriter.Replace(identifier.Symbol, identifier.GetText().Pascalize());
            }
        }

        public string GetResult()
        {
            return _rewriter.GetText();
        }
    }
}
