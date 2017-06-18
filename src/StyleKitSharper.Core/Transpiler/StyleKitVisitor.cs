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
    public class StyleKitVisitor
    {
        protected static readonly string[] Usings = new string[] {
            "System",
            "System.Linq",
            "Android.Graphics",
            "System.Collections.Generic",
        };

        protected static readonly Regex JavaConstantConventionRegex = new Regex(@"^[A-Z_]+[A-Z_\d]*$");

        protected static readonly string[] ResizingBehaviorEnum = new string[] { "AspectFit", "AspectFill", "Stretch", "Center" };

        protected static readonly Dictionary<string, string> ExpressionMappings = new Dictionary<string, string> {
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

            { @"(.*)\.setFlags", @"$1.Flags = " },
            { @"(.*)\.setColor", @"$1.Color = (ColorWrapper)" },
            { @"(.*)\.setStrokeWidth", @"$1.StrokeWidth = " },
            { @"(.*)\.setStrokeMiter", @"$1.StrokeMiter = " },

            { @"(.*)\.drawColor\((.*)\.color\)", @"$1.DrawColor((ColorWrapper)$2.Color)" },
        };

        protected static readonly Dictionary<string, string> ColorClassMethods = new Dictionary<string, string>
        {
            { "red", "GetRedComponent" },
            { "green", "GetGreenComponent" },
            { "blue", "GetBlueComponent" },
            { "alpha", "GetAlphaComponent" },
            { "argb", "Argb" },
            { "RGBToHSV", "RGBToHSV" },
            { "HSVToColor", "HSVToColor" },
        };

        protected readonly TokenStreamRewriter _rewriter;

        public StyleKitVisitor(ITokenStream tokens)
        {
            _rewriter = new TokenStreamRewriter(tokens);
        }

        protected StyleKitVisitor(StyleKitVisitor parent)
        {
            _rewriter = parent._rewriter;
        }

        public void Visit(IParseTree node)
        {
            var traverseChildren = true;

            var visitor = ResolveNodeVisitor(node);
            if (visitor != null)
            {
                traverseChildren = visitor();
            }

            if (traverseChildren)
            {
                for (int i = 0; i < node.ChildCount; i++)
                {
                    Visit(node.GetChild(i));
                }
            }
        }

        protected virtual Func<bool> ResolveNodeVisitor(IParseTree node)
        {
            switch (node)
            {
                case CompilationUnitContext ctx:
                    return () => VisitCompilationUnit(ctx);
                case ClassDeclarationContext ctx:
                    return () => VisitClassDeclaration(ctx);
                case ClassBodyDeclarationContext ctx:
                    return () => VisitClassBodyDeclaration(ctx);
                case MethodDeclarationContext ctx:
                    return () => VisitMethodDeclaration(ctx);
                case FieldDeclarationContext ctx:
                    return () => VisitFieldDeclaration(ctx);
                case TypeTypeContext ctx:
                    return () => VisitTypeType(ctx);
                case ExpressionContext ctx:
                    return () => VisitExpression(ctx);
                case SwitchLabelContext ctx:
                    return () => VisitSwitchLabel(ctx);
                case VariableDeclaratorIdContext ctx:
                    return () => VisitariableDeclaratorId(ctx);
            }
            return null;
        }

        protected virtual bool VisitCompilationUnit(CompilationUnitContext ctx)
        {
            var package = ctx.packageDeclaration();
            if (package != null)
            {
                var qualifiedName = package.qualifiedName().GetText();

                _rewriter.Replace(package.Start, package.Stop, $"namespace {qualifiedName} {{");
            }

            var imports = ctx.importDeclaration();
            if (imports.Length > 0)
            {
                _rewriter.Replace(
                    imports.First().Start,
                    imports.Last().Stop,
                    string.Join("\n", Usings.Select(x => $"using {x};")));
            }

            if (package != null)
            {
                _rewriter.InsertAfter(ctx.Stop, "\n}");
            }

            _rewriter.InsertAfter(ctx.Stop, "\n\n" + Resources.ColorWrapper);

            return true;
        }

        protected virtual bool VisitClassDeclaration(ClassDeclarationContext ctx)
        {
            var className = ctx.children[1];

            if (className.GetText() == "PaintCodeColor")
            {
                var styleKitPaintCodeColorClassVisitor = new StyleKitPaintCodeColorClassVisitor(this);
                styleKitPaintCodeColorClassVisitor.Visit(ctx);
                return false;
            }

            return true;
        }

        protected virtual bool VisitClassBodyDeclaration(ClassBodyDeclarationContext ctx)
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

            return true;
        }

        protected virtual bool VisitMethodDeclaration(MethodDeclarationContext ctx)
        {
            var methodNameNode = ctx.children.OfType<TerminalNodeImpl>()
                .Where(x => x.Symbol.Type == Identifier).Single();

            _rewriter.Replace(methodNameNode.Symbol, methodNameNode.GetText().Pascalize());

            return true;
        }

        protected virtual bool VisitFieldDeclaration(FieldDeclarationContext ctx)
        {
            var fieldNameNodes = ctx.Descendants().OfType<VariableDeclaratorIdContext>().ToList();

            foreach (var node in fieldNameNodes)
            {
                var identifier = node.Identifier();
                _rewriter.Replace(identifier.Symbol, identifier.GetText().Pascalize());
            }

            return true;
        }

        protected virtual bool VisitTypeType(TypeTypeContext ctx)
        {
            var primitiveType = ctx.primitiveType();
            if (primitiveType != null)
            {
                if (primitiveType.GetText() == "boolean")
                {
                    _rewriter.Replace(primitiveType.Start, primitiveType.Stop, "bool");
                }
            }

            return true;
        }

        protected virtual bool VisitExpression(ExpressionContext ctx)
        {
            var expressionText = ctx.GetText();
            foreach (var map in ExpressionMappings)
            {
                if (Regex.IsMatch(expressionText, $"^{map.Key}$"))
                {
                    var replacement = Regex.Replace(expressionText, map.Key, map.Value);
                    _rewriter.Replace(ctx.Start, ctx.Stop, replacement);
                    return false;
                }
                else if (Regex.IsMatch(expressionText, @"^Color.[A-Za-z_]+[A-Za-z_\d]*$"))
                {
                    var memberText = expressionText.Substring(6);
                    if (ColorClassMethods.ContainsKey(memberText))
                    {
                        _rewriter.Replace(ctx.Start, ctx.Stop, $"Color.{ColorClassMethods[memberText]}");
                        return false;
                    }
                    else if (JavaConstantConventionRegex.IsMatch(memberText))
                    {
                        _rewriter.Replace(ctx.Start, ctx.Stop, $"Color.{memberText.ToLowerInvariant().Pascalize()}");
                        return false;
                    }
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

        protected virtual bool VisitSwitchLabel(SwitchLabelContext ctx)
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

            return true;
        }

        protected virtual bool VisitariableDeclaratorId(VariableDeclaratorIdContext ctx)
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

            return true;
        }

        public string GetResult()
        {
            return _rewriter.GetText();
        }
    }
}
