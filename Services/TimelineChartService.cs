using TimelineChartAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TimelineChartAPI.Services
{
    public class TimelineChartService : ITimelineChartService
    {
        private readonly ISvgGeneratorService _svgGenerator;
        private readonly ILocalizationService _localization;
        private readonly IPngRenderingService _pngRenderer;

        public TimelineChartService(
            ISvgGeneratorService svgGenerator,
            ILocalizationService localization,
            IPngRenderingService pngRenderer)
        {
            _svgGenerator = svgGenerator;
            _localization = localization;
            _pngRenderer = pngRenderer;
        }

        public string GenerateSvg(TimelineChartRequest request)
        {
            var dataPoints = request.Data ?? GetDefaultData();
            var texts = _localization.GetTexts(request.Lang);
            var config = new ChartConfiguration();

            return _svgGenerator.GenerateSvg(dataPoints, request, texts, config);
        }

        public async Task<byte[]> GeneratePngAsync(TimelineChartRequest request)
        {
            var svg = GenerateSvg(request);
            return await _pngRenderer.RenderSvgToPngAsync(svg, request.Width, request.Height);
        }

        private static List<DataPoint> GetDefaultData()
        {
            return new List<DataPoint>
            {
                new() { Id = "1", Year = "1" },
                new() { Id = "5", Year = "5" },
                new() { Id = "10", Year = "10" },
                new() { Id = "60", Major = true, Year = "A60" },
                new() { Id = "90", Major = true, Value = "150,000", Year = "A90", LastPayment = true }
            };
        }
    }
}
