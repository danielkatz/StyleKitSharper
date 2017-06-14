using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text;

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

            return Content(java);
        }
    }
}
