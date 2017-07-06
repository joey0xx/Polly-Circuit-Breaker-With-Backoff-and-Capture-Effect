using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace SampleApi.Controllers
{
    public class PollyController : ApiController
    {
        [HttpGet]
        [Route("api/testok")]
        public async Task<IHttpActionResult> TestOk()
        {
      
            return Ok(new {name = "angel"});
        }
    }
}
