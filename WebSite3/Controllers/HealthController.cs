using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;

namespace WebSite3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly StatelessServiceContext _serviceContext;

        public HealthController(StatelessServiceContext serviceContext)
        {
            _serviceContext = serviceContext;
        }
        // GET: api/<HealthController>
        [HttpGet]
        public ActionResult Get()
        {
            ServiceEventSource.Current.ServiceMessage(_serviceContext, "This endpoint is always unhealthy....");
            return new BadRequestResult();
        }
    }
}
