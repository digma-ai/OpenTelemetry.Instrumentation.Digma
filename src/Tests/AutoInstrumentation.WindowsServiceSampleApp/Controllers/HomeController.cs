using Microsoft.AspNetCore.Mvc;

namespace AutoInstrumentation.WindowsServiceSampleApp.Controllers;

[ApiController]
[Route("/")]
public class HomeController
{
    [HttpGet]
    public string GetOk([FromQuery] int times = 1) => string.Join(", ", Enumerable.Range(0, times).Select(_ => "Ok!"));
}