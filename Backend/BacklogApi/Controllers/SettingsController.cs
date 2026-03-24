using BacklogApi.Application.DTOs;
using BacklogApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BacklogApi.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ISmtpSettingsService _smtpService;

    public SettingsController(ISmtpSettingsService smtpService)
    {
        _smtpService = smtpService;
    }

    [HttpGet("smtp")]
    public async Task<ActionResult<SmtpSettingsDto>> GetSmtpSettings()
    {
        var settings = await _smtpService.GetSettingsAsync();
        if (settings == null)
        {
            return Ok(new SmtpSettingsDto(0, "", 587, "", null, true, "", "", DateTime.UtcNow));
        }
        return Ok(settings);
    }

    [HttpPost("smtp")]
    public async Task<IActionResult> UpdateSmtpSettings(UpdateSmtpSettingsDto dto)
    {
        await _smtpService.UpdateSettingsAsync(dto);
        return Ok(new { message = "SMTP nastavitve uspešno posodobljene" });
    }

    [HttpPost("smtp/test")]
    public async Task<IActionResult> TestSmtpSettings([FromBody] string email)
    {
        var result = await _smtpService.SendEmailAsync(email, "Testno sporočilo - TaskMaster", "To je testno e-poštno sporočilo iz aplikacije TaskMaster. Če ste to prejeli, vaše SMTP nastavitve delujejo pravilno.");
        if (result)
        {
            return Ok(new { message = "Testno e-poštno sporočilo je bilo uspešno poslano" });
        }
        return BadRequest(new { message = "Pošiljanje testnega e-poštnega sporočila ni uspelo. Preverite nastavitve SMTP." });
    }
}
