using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Antlr4.Runtime;
using StyleKitSharper.Core.Transpiler;
using StyleKitSharper.Core;

namespace StyleKitSharper.Web.Controllers
{
    [Route("api")]
    public class ApiController : Controller
    {
        [HttpPost("transform")]
        public IActionResult Transform([FromBody] string base64)
        {
            var transpiler = new StyleKitTranspiler();
            var bytes = Convert.FromBase64String(base64);
            var java = Encoding.UTF8.GetString(bytes);
            var csharp = transpiler.Transpile(java);

            return Content(csharp);
        }
    }
}
