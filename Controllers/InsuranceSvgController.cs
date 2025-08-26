using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace TimelineChartAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimelineChartController : ControllerBase
    {
        public class DataPoint
        {
            public string Id { get; set; } = "";
            public string? Label { get; set; }
            public bool Major { get; set; }
            public string? Value { get; set; }
            public string? Year { get; set; }
            public int? Level { get; set; }
            public string? AmountLabel { get; set; }
            public bool LastPayment { get; set; }
            public int Next { get; set; }
            public bool DivideSa { get; set; }
        }

        public class TimelineChartRequest
        {
            public List<DataPoint>? Data { get; set; }
            public int Height { get; set; } = 280;
            public int MarginHorizontal { get; set; } = 40;
            public string Color { get; set; } = "#2c8592";
            public string DotFill { get; set; } = "#c9e04a";
            public string Lang { get; set; } = "th";
            public int Width { get; set; } = 800;
            public double ZigzagMaxAmplitude { get; set; } = 20.0;
        }

        private class PointPos
        {
            public DataPoint Point { get; set; } = new DataPoint();
            public double X { get; set; }
            public double Y { get; set; }
            public int Index { get; set; }
        }

        [HttpPost("generate-svg")]
        public IActionResult GenerateTimelineSvg([FromBody] TimelineChartRequest request)
        {
            try
            {
                var svg = GenerateSvg(request);
                return Content(svg, "image/svg+xml");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Error generating SVG: {ex.Message}");
            }
        }

        [HttpPost("generate-png")]
        public async Task<IActionResult> GenerateTimelinePng([FromBody] TimelineChartRequest request)
        {
            try
            {
                var svg = GenerateSvg(request);
                var pngBytes = await RenderSvgToPngAsync(svg, request.Width, request.Height);
                return File(pngBytes, "image/png", "timeline-chart.png");
            }
            catch (System.Exception ex)
            {
                return BadRequest($"Error generating PNG: {ex.Message}");
            }
        }

        private string GenerateSvg(TimelineChartRequest request)
        {
            var data = request.Data ?? GetDefaultData();
            var width = request.Width;
            var height = request.Height;
            var marginHorizontal = request.MarginHorizontal;
            var color = request.Color;
            var dotFill = request.DotFill;
            var lang = request.Lang;
            var zigzagMaxAmp = request.ZigzagMaxAmplitude;

            var textContent = new Dictionary<string, string>
            {
                ["xAxisLabel"] = lang == "th" ? "สิ้นปีกรมธรรม์ที่" : "End of Year",
                ["premiumEnd"] = lang == "th" ? "ชำระเบี้ยครบ" : "Premium Payment Finished",
                ["atAge"] = lang == "th" ? "ครบอายุ" : "At age",
                ["coverage"] = lang == "th" ? "ความคุ้มครองชีวิต : จำนวนที่มากกว่าระหว่าง 100% ของทุนประกันภัย" : "Death coverage*",
                ["or"] = lang == "th" ? "หรือ มูลค่าเวนคืนเงินสด หรือ เบี้ยประกันภัยสะสม" : "or Cash Value or Accumulated Premium"
            };

            var centerY = Math.Round((double)height / 2 + 20);
            var availableW = Math.Max(200, width - marginHorizontal * 2);
            var gap = (double)availableW / Math.Max(1, data.Count - 1);

            var dotRadius = 8;
            var strokeWidth = 3;
            var fontSize = 14;
            var smallFont = 12;

            var points = data.Select((p, i) => new PointPos
            {
                Point = p,
                X = marginHorizontal + gap * i,
                Y = centerY,
                Index = i
            }).ToList();

            var svg = new StringBuilder();
            svg.AppendLine($"<svg viewBox=\"0 0 {width} {height}\" xmlns=\"http://www.w3.org/2000/svg\">");
            svg.AppendLine($"<rect width=\"{width}\" height=\"{height}\" fill=\"#f8f9fa\"/>");

            // Title
            svg.AppendLine($"<text x=\"{width / 2}\" y=\"25\" font-size=\"16\" font-weight=\"600\" text-anchor=\"middle\" fill=\"#333\" font-family=\"THSarabun, Tahoma, sans-serif\">{textContent["coverage"]}</text>");
            svg.AppendLine($"<text x=\"{width / 2}\" y=\"42\" font-size=\"14\" text-anchor=\"middle\" fill=\"#666\" font-family=\"THSarabun, Tahoma, sans-serif\">{textContent["or"]}</text>");

            // Path
            var pathD = GenerateZigzagPath(points, centerY, gap, zigzagMaxAmp);
            svg.AppendLine($"<path d=\"{pathD}\" stroke=\"{color}\" stroke-width=\"{strokeWidth}\" fill=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");

            // Data points
            foreach (var point in points)
            {
                var p = point.Point;
                var x = point.X;
                var y = point.Y;

                svg.AppendLine($"<circle cx=\"{x}\" cy=\"{y}\" r=\"{dotRadius}\" fill=\"{dotFill}\" stroke=\"{color}\" stroke-width=\"2\"/>");

                var labelY = p.Year?.Contains('A') == true ? y + 40 : y + 25;
                var yearText = p.Year?.Replace("A", "") ?? "";
                svg.AppendLine($"<text x=\"{x}\" y=\"{labelY}\" font-size=\"{smallFont}\" text-anchor=\"middle\" fill=\"#333\" font-family=\"THSarabun, Tahoma, sans-serif\">{EscapeXml(yearText)}</text>");

                if (p.Year?.Contains('A') == true)
                {
                    svg.AppendLine($"<text x=\"{x}\" y=\"{y + 25}\" font-size=\"{smallFont - 2}\" text-anchor=\"middle\" fill=\"#333\" font-family=\"THSarabun, Tahoma, sans-serif\">{textContent["atAge"]}</text>");
                }

                if (p.Level.HasValue && p.Level >= 0 && !string.IsNullOrEmpty(p.AmountLabel))
                {
                    var amountY = y - 40 - (p.Level.Value * 20);
                    svg.AppendLine($"<text x=\"{x}\" y=\"{amountY}\" font-size=\"{smallFont}\" font-weight=\"700\" text-anchor=\"middle\" fill=\"#333\" font-family=\"THSarabun, Tahoma, sans-serif\">{EscapeXml(p.AmountLabel)}</text>");
                }
            }

            // Y-axis
            svg.AppendLine($"<text x=\"15\" y=\"{centerY}\" font-size=\"{fontSize}\" font-weight=\"600\" text-anchor=\"middle\" fill=\"#333\" font-family=\"THSarabun, Tahoma, sans-serif\" transform=\"rotate(-90 15 {centerY})\">{textContent["xAxisLabel"]}</text>");

            // Last payment
            var lastPaymentPoint = points.FirstOrDefault(p => p.Point.LastPayment);
            if (lastPaymentPoint != null && !string.IsNullOrEmpty(lastPaymentPoint.Point.Value))
            {
                var x = lastPaymentPoint.X;
                var y = lastPaymentPoint.Y;

                svg.AppendLine($"<line x1=\"{x}\" y1=\"60\" x2=\"{x}\" y2=\"{y - 20}\" stroke=\"{color}\" stroke-width=\"3\"/>");
                svg.AppendLine($"<polygon points=\"{x - 6},{y - 23} {x + 6},{y - 23} {x},{y - 15}\" fill=\"#1b1b1bff\"/>");
                svg.AppendLine($"<text x=\"{x}\" y=\"52\" font-size=\"16\" font-weight=\"700\" text-anchor=\"middle\" fill=\"#333\" font-family=\"THSarabun, Tahoma, sans-serif\">{EscapeXml(lastPaymentPoint.Point.Value)}</text>");
                svg.AppendLine($"<text x=\"{x}\" y=\"{y + 60}\" font-size=\"{smallFont}\" text-anchor=\"middle\" fill=\"#333\" font-family=\"THSarabun, Tahoma, sans-serif\">{textContent["premiumEnd"]}</text>");
            }

            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        private string GenerateZigzagPath(List<PointPos> points, double centerY, double gap, double zigzagMaxAmplitude)
        {
            if (points == null || points.Count == 0) return "";
            var pathD = new StringBuilder();
            pathD.Append($"M {points[0].X} {centerY}");
            for (int i = 1; i < points.Count; i++)
            {
                var prev = points[i - 1];
                var cur = points[i];
                var x0 = prev.X;
                var x1 = cur.X;
                var baseY = centerY;

                if (prev.Point.Major)
                {
                    var maxAmp = Math.Min(zigzagMaxAmplitude, gap * 0.4);
                    var straightLength = Math.Min(maxAmp, (x1 - x0) / 6.0);
                    var zigzagWidth = Math.Max(0, (x1 - x0) - 2 * straightLength);
                    var ampX = zigzagWidth / 6.0;
                    var ampY = ampX;

                    var p1 = x0 + straightLength;
                    var peakX = p1 + ampX;
                    var p2 = peakX + ampX;
                    var troughX = p2 + ampX;
                    var p3 = troughX + ampX;
                    var finalX = x1 - straightLength;

                    pathD.Append($" L {p1} {baseY}");
                    pathD.Append($" L {peakX} {baseY - ampY}");
                    pathD.Append($" L {p2} {baseY}");
                    pathD.Append($" L {troughX} {baseY + ampY}");
                    pathD.Append($" L {p3} {baseY}");
                    pathD.Append($" L {finalX} {baseY}");
                    pathD.Append($" L {x1} {baseY}");
                }
                else
                {
                    pathD.Append($" L {x1} {baseY}");
                }
            }
            return pathD.ToString();
        }

        private List<DataPoint> GetDefaultData()
        {
            return new List<DataPoint>
            {
                new DataPoint { Id = "1", Year = "1" },
                new DataPoint { Id = "5", Year = "5" },
                new DataPoint { Id = "10", Year = "10" },
                new DataPoint { Id = "60", Major = true, Year = "A60" },
                new DataPoint { Id = "90", Major = true, Value = "150,000", Year = "A90", LastPayment = true }
            };
        }

        private string EscapeXml(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        private async Task<byte[]> RenderSvgToPngAsync(string svgContent, int width, int height)
        {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            using var page = await browser.NewPageAsync();

            string html = $@"
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ margin:0; padding:0; }}
                        svg {{ width:{width}px; height:{height}px; }}
                    </style>
                </head>
                <body>
                    {svgContent}
                </body>
                </html>";

            await page.SetContentAsync(html);
            var element = await page.QuerySelectorAsync("svg");

            var screenshotBytes = await element.ScreenshotDataAsync(new ScreenshotOptions
            {
                Type = ScreenshotType.Png,
                OmitBackground = false
            });

            return screenshotBytes;
        }
    }
}
