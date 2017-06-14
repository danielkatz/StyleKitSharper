﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace StyleKitSharper.Web.Controllers
{
    [Route("api")]
    public class ApiController : Controller
    {
        [HttpPost("transform")]
        public IActionResult Transform([FromBody] string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            var java = Encoding.UTF8.GetString(bytes);
            var csharp = JavaToCSharp(java);

            return Content(csharp);
        }

        private string JavaToCSharp(string java)
        {
            var stream = new AntlrInputStream(java);
            var lexer = new JavaLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var rewriter = new TokenStreamRewriter(tokens);
            var parser = new JavaParser(tokens);
            parser.BuildParseTree = true;
            var tree = parser.compilationUnit();

            return rewriter.GetText();
        }
    }
}
