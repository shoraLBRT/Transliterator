using Microsoft.AspNetCore.Mvc;
using Transliterator.API.Models;
using Transliterator.Domain.Interfaces;

namespace Transliterator.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransliterationController : ControllerBase
    {
        private readonly ITransliterationService _transliterationService;
        private readonly ILogger<TransliterationController> _logger;

        public TransliterationController(ITransliterationService transliterationService, ILogger<TransliterationController> logger)
        {
            _transliterationService = transliterationService;
            _logger = logger;
        }

        [HttpPost("transliterate")]
        public async Task<ActionResult<TransliterationResponse>> Transliterate([FromBody] TransliterationRequest request)
        {
            try
            {
                var result = await _transliterationService.TransliterateAsync(
                    request.ArabicText, request.Profile);

                return Ok(new TransliterationResponse(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transliteration error");
                return StatusCode(500, new { Error = "Transliteration failed" });
            }
        }

        [HttpPut("rules/{arabicLetter}")]
        public async Task<IActionResult> UpdateRule(string arabicLetter, [FromQuery] string mapping, [FromQuery] string? profile = null)
        {
            // TODO
            return Ok(new { Message = "Rule updated" });
        }

        [HttpGet("rules")]
        public async Task<ActionResult<Dictionary<string, string>>> GetRules([FromQuery] string? profile = null)
        {
            // TODO
            return Ok();
        }

        [HttpGet("profiles")]
        public async Task<ActionResult<IEnumerable<string>>> GetProfiles()
        {
            // TODO
            return Ok();
        }
    }
}
