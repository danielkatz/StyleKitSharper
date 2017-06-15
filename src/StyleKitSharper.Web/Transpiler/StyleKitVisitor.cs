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
        private static readonly string[] Usings = new string[] {
            "System",
            "System.Linq",
            "Android.Graphics",
            "System.Collections.Generic",
        };

        private static readonly Regex JavaConstantConventionRegex = new Regex(@"^[A-Z_]+[A-Z_\d]*$");

        private static readonly string[] ResizingBehaviorEnum = new string[] { "AspectFit", "AspectFill", "Stretch", "Center" };

        private static readonly Dictionary<string, string> ExpressionMappings = new Dictionary<string, string> {
            { @"Paint\.ANTI_ALIAS_FLAG", @"PaintFlags.AntiAlias" },
            { @"Paint\.Style\.FILL", @"Paint.Style.Fill" },
            { @"Path\.FillType\.EVEN_ODD", @"Path.FillType.EvenOdd" },
            { @"Path\.Direction\.CW", @"Path.Direction.Cw" },
            { @"Shader\.TileMode\.CLAMP", @"Shader.TileMode.Clamp" },
            { @"Canvas\.ALL_SAVE_FLAG", @"SaveFlags.All" },
            { @"BlurMaskFilter\.Blur\.NORMAL", @"BlurMaskFilter.Blur.Normal" },
            { @"Paint\.Style\.STROKE", @"Paint.Style.Stroke" },
            { @"PorterDuff\.Mode\.SRC_IN", @"PorterDuff.Mode.SrcIn" },
            { @"Arrays\.equals", @"Enumerable.SequenceEqual" },
        };

        private readonly TokenStreamRewriter _rewriter;

        public StyleKitVisitor(ITokenStream tokens)
        {
            _rewriter = new TokenStreamRewriter(tokens);
        }

        public void Visit(IParseTree node)
        {
            var traverseChildren = true;
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
                    traverseChildren = VisitExpression(ctx);
                    break;
                case SwitchLabelContext ctx:
                    VisitSwitchLabel(ctx);
                    break;
                case VariableDeclaratorIdContext ctx:
                    VisitariableDeclaratorId(ctx);
                    break;
                default:
                    break;
            }

            if (traverseChildren)
            {
                for (int i = 0; i < node.ChildCount; i++)
                {
                    Visit(node.GetChild(i));
                }
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
                    string.Join("\n", Usings.Select(x => $"using {x};")));
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
            var overrideModifier = ctx.modifier().FirstOrDefault(x => x.classOrInterfaceModifier()?.annotation()?.annotationName()?.qualifiedName()?.Identifier().FirstOrDefault()?.GetText() == "Override");

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

            if (overrideModifier != null)
            {
                _rewriter.Delete(overrideModifier.Start, overrideModifier.Stop);
                _rewriter.InsertBefore(ctx.memberDeclaration().Start, "override ");
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

        private bool VisitExpression(ExpressionContext ctx)
        {
            var expressionText = ctx.GetText();
            foreach (var map in ExpressionMappings)
            {
                if (Regex.IsMatch(expressionText, $"^{map.Key}$"))
                {
                    _rewriter.Replace(ctx.Start, ctx.Stop, map.Value);
                    return false;
                }
            }

            var identifier = ctx.Identifier();
            if (identifier != null)
            {
                _rewriter.Replace(identifier.Symbol, identifier.GetText().Pascalize());
            }
            else if (ctx.children.Count == 3)
            {
                if (ctx.children[1].GetText() == "instanceof"
                    && ctx.children[2] is TypeTypeContext)
                {
                    var instanceof = (TerminalNodeImpl)ctx.children[1];
                    _rewriter.Replace(instanceof.Symbol, "is");
                }
            }

            return true;
        }

        private void VisitSwitchLabel(SwitchLabelContext ctx)
        {
            var expr = ctx.constantExpression();
            if (expr != null)
            {
                var exprText = expr.GetText();
                if (ResizingBehaviorEnum.Contains(exprText))
                {
                    _rewriter.Replace(expr.Start, expr.Stop, $"ResizingBehavior.{exprText}");
                }
            }
        }

        private void VisitariableDeclaratorId(VariableDeclaratorIdContext ctx)
        {
            var idenifier = ctx.Identifier();
            if (ctx.GetText().EndsWith("[]") && idenifier != null)
            {
                var idenifierText = idenifier.GetText();
                var fieldDeclaration = ctx.Ancestors().OfType<FieldDeclarationContext>().FirstOrDefault();
                var localVariableDeclaration = ctx.Ancestors().OfType<LocalVariableDeclarationContext>().FirstOrDefault();

                TypeTypeContext typeType = null;
                if (fieldDeclaration != null)
                {
                    typeType = fieldDeclaration.typeType();
                }
                else if (localVariableDeclaration != null)
                {
                    typeType = localVariableDeclaration.typeType();
                }

                if (typeType != null)
                {
                    _rewriter.InsertAfter(typeType.Stop, "[]");
                    _rewriter.Replace(ctx.Start, ctx.Stop, idenifierText);
                }
            }
        }

        public string GetResult()
        {
            return _rewriter.GetText();
        }
    }
}
