using Antlr4.Runtime;
using StyleKitSharper.Core.Transpiler;
using System;
using System.Collections.Generic;
using System.Text;

namespace StyleKitSharper.Core
{
    public class StyleKitTranspiler
    {
        public string Namespace { get; set; }

        public string Transpile(string javaCode)
        {
            var stream = new AntlrInputStream(javaCode);
            var lexer = new JavaLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new JavaParser(tokens);
            parser.BuildParseTree = true;

            var styleKitVisitor = new StyleKitVisitor(tokens)
            {
                Namespace = Namespace
            };

            styleKitVisitor.Visit(parser.compilationUnit());
            return styleKitVisitor.GetResult();
        }
    }
}
