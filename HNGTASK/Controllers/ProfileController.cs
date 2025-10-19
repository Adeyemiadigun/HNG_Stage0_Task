using HNGTASK.ExternalService;
using HNGTASK.ResponseModels;
using Microsoft.AspNetCore.Mvc;

namespace HNGPROJ.HNGTASK.Controllers;

[ApiController]
[Route("api/[Controller]")]
public class ProfileController : Controller
{
    private readonly RandomFactService _randomFactService;

    public ProfileController(RandomFactService randomFactService)
    {
        _randomFactService = randomFactService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetResponse()
    {
        var response = await _randomFactService.GetRandomFact();
        return Ok(new ResponseDto
        {
            Fact = response
        });
    }
}