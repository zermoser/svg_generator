using Microsoft.AspNetCore.Mvc;
using TimelineChartAPI.Models;
using TimelineChartAPI.Services;
using System;
using System.Threading.Tasks;

namespace TimelineChartAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimelineChartController : ControllerBase
    {
        private readonly ITimelineChartService _chartService;

        public TimelineChartController(ITimelineChartService chartService)
        {
            _chartService = chartService;
        }

        [HttpPost("generate-svg")]
        public IActionResult GenerateTimelineSvg([FromBody] TimelineChartRequest request)
        {
            try
            {
                var svg = _chartService.GenerateSvg(request);
                return Content(svg, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating SVG: {ex.Message}");
            }
        }

        [HttpPost("generate-png")]
        public async Task<IActionResult> GenerateTimelinePng([FromBody] TimelineChartRequest request)
        {
            try
            {
                var pngBytes = await _chartService.GeneratePngAsync(request);
                return File(pngBytes, "image/png", "timeline-chart.png");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating PNG: {ex.Message}");
            }
        }
    }
}
