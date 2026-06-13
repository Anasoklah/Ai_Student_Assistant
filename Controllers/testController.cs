using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SyrianStudyBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class testController : ControllerBase
{
    [HttpGet]
    public IActionResult testEndpoint()
    {
        return Ok("api work");
    }
}
