using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using Svg.Skia;

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

            // maximum amplitude (px) for zigzag calculation
            public double ZigzagMaxAmplitude { get; set; } = 20.0;

            // pixel ratio for HiDPI output (1 = 1:1, 2 = retina)
            public float PixelRatio { get; set; } = 1f;

            public string? FontPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Fonts", "THSarabun.ttf");
        }

        // Internal typed point class
        private class PointPos
        {
            public DataPoint Point { get; set; } = new DataPoint();
            public double X { get; set; }
            public double Y { get; set; }
            public int Index { get; set; }
        }

        [HttpPost("generate-png")]
        public IActionResult GenerateTimelineChart([FromBody] TimelineChartRequest request)
        {
            try
            {
                var svg = GenerateSvg(request);
                var pngBytes = ConvertSvgToPng(svg, request.Width, request.Height, request.PixelRatio);

                return File(pngBytes, "image/png", "timeline-chart.png");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating chart: {ex.Message}");
            }
        }

        [HttpPost("generate-svg")]
        public IActionResult GenerateTimelineSvg([FromBody] TimelineChartRequest request)
        {
            try
            {
                var svg = GenerateSvg(request);
                return Content(svg, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating SVG: {ex.Message}");
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
            var fontPath = request.FontPath;

            // Text content based on language
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

            // Calculate points positions using typed PointPos list
            var points = data.Select((p, i) => new PointPos
            {
                Point = p,
                X = marginHorizontal + gap * i,
                Y = centerY,
                Index = i
            }).ToList();

            var svg = new StringBuilder();

            // Try to embed font if provided (base64)
            string fontStyle = "";
            if (!string.IsNullOrEmpty(fontPath))
            {
                try
                {
                    var b64 = EmbedFontBase64(fontPath);
                    if (!string.IsNullOrEmpty(b64))
                    {
                        // Use a fixed family name so CSS can reference it
                        fontStyle = $"<style>@font-face{{font-family:'EmbeddedFont';src:url('data:font/truetype;charset=utf-8;base64,{b64}') format('truetype');}} text{{font-family:'EmbeddedFont', sans-serif;}}</style>";
                    }
                }
                catch
                {
                    // If embedding fails, silently ignore and fall back to system fonts
                    fontStyle = "";
                }
            }

            // add viewBox & preserveAspectRatio so SKSvg can infer proper bounds
            svg.AppendLine($"<svg width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" preserveAspectRatio=\"xMidYMid meet\" xmlns=\"http://www.w3.org/2000/svg\">");

            // insert font style (if any)
            if (!string.IsNullOrEmpty(fontStyle))
            {
                svg.AppendLine(fontStyle);
            }

            // Background
            svg.AppendLine($"<rect width=\"{width}\" height=\"{height}\" fill=\"#f8f9fa\"/>");

            // Main Title (will use embedded font via CSS)
            svg.AppendLine($"<text x=\"{width / 2}\" y=\"25\" font-size=\"16\" font-weight=\"600\" text-anchor=\"middle\" fill=\"#333\">");
            svg.AppendLine($"{textContent["coverage"]}");
            svg.AppendLine("</text>");

            // Subtitle
            svg.AppendLine($"<text x=\"{width / 2}\" y=\"42\" font-size=\"14\" text-anchor=\"middle\" fill=\"#666\">");
            svg.AppendLine($"{textContent["or"]}");
            svg.AppendLine("</text>");

            // Generate zigzag path with configurable indices (now using Major==true condition)
            var pathD = GenerateZigzagPath(points, centerY, gap, zigzagMaxAmp);
            svg.AppendLine($"<path d=\"{pathD}\" stroke=\"{color}\" stroke-width=\"{strokeWidth}\" fill=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");

            // Data points
            foreach (var point in points)
            {
                var p = point.Point;
                var x = point.X;
                var y = point.Y;

                // Data point circle
                svg.AppendLine($"<circle cx=\"{x}\" cy=\"{y}\" r=\"{dotRadius}\" fill=\"{dotFill}\" stroke=\"{color}\" stroke-width=\"2\"/>");

                // Year labels below points
                var labelY = p.Year?.Contains('A') == true ? y + 40 : y + 25;
                var yearText = p.Year?.Replace("A", "") ?? "";
                svg.AppendLine($"<text x=\"{x}\" y=\"{labelY}\" font-size=\"{smallFont}\" text-anchor=\"middle\" fill=\"#333\">{EscapeXml(yearText)}</text>");

                // Age labels for major points
                if (p.Year?.Contains('A') == true)
                {
                    svg.AppendLine($"<text x=\"{x}\" y=\"{y + 25}\" font-size=\"{smallFont - 2}\" text-anchor=\"middle\" fill=\"#333\">{EscapeXml(textContent["atAge"])}</text>");
                }

                // Amount labels for points with level
                if (p.Level.HasValue && p.Level >= 0 && !string.IsNullOrEmpty(p.AmountLabel))
                {
                    var amountY = y - 40 - (p.Level.Value * 20);
                    svg.AppendLine($"<text x=\"{x}\" y=\"{amountY}\" font-size=\"{smallFont}\" font-weight=\"700\" text-anchor=\"middle\" fill=\"#333\">{EscapeXml(p.AmountLabel)}</text>");
                }
            }

            // Y-axis label
            svg.AppendLine($"<text x=\"15\" y=\"{centerY}\" font-size=\"{fontSize}\" font-weight=\"600\" text-anchor=\"middle\" fill=\"#333\" transform=\"rotate(-90 15 {centerY})\">");
            svg.AppendLine($"{EscapeXml(textContent["xAxisLabel"])}");
            svg.AppendLine("</text>");

            // Arrow and final value for last payment point
            var lastPaymentPoint = points.FirstOrDefault(p => p.Point.LastPayment);
            if (lastPaymentPoint != null && !string.IsNullOrEmpty(lastPaymentPoint.Point.Value))
            {
                var x = lastPaymentPoint.X;
                var y = lastPaymentPoint.Y;

                // Vertical arrow line
                svg.AppendLine($"<line x1=\"{x}\" y1=\"60\" x2=\"{x}\" y2=\"{y - 20}\" stroke=\"{color}\" stroke-width=\"3\"/>");

                // Arrow head pointing down
                svg.AppendLine($"<polygon points=\"{x - 6},{y - 23} {x + 6},{y - 23} {x},{y - 15}\" fill=\"#1b1b1bff\"/>");

                // Value above arrow
                svg.AppendLine($"<text x=\"{x}\" y=\"52\" font-size=\"16\" font-weight=\"700\" text-anchor=\"middle\" fill=\"#333\">{EscapeXml(lastPaymentPoint.Point.Value)}</text>");

                // Label below the last point
                svg.AppendLine($"<text x=\"{x}\" y=\"{y + 60}\" font-size=\"{smallFont}\" text-anchor=\"middle\" fill=\"#333\">{EscapeXml(textContent["premiumEnd"])}</text>");
            }

            // Left arrow indicator
            svg.AppendLine($"<line x1=\"60\" y1=\"80\" x2=\"150\" y2=\"80\" stroke=\"{color}\" stroke-width=\"3\"/>");
            svg.AppendLine($"<polygon points=\"50,80 65,75 65,85\" fill=\"#1b1b1bff\"/>");

            // Right arrow indicator
            svg.AppendLine($"<line x1=\"{width - 150}\" y1=\"80\" x2=\"{width - 60}\" y2=\"80\" stroke=\"{color}\" stroke-width=\"3\"/>");
            svg.AppendLine($"<polygon points=\"{width - 50},80 {width - 65},75 {width - 65},85\" fill=\"#1b1b1bff\"/>");

            svg.AppendLine("</svg>");

            return svg.ToString();
        }

        // Produce zigzag for segments where previous point has Major == true
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
                var segmentLength = x1 - x0;
                var baseY = centerY;

                // Produce zigzag if the previous point is marked Major
                if (prev.Point != null && prev.Point.Major)
                {
                    var maxAmplitude = Math.Min(zigzagMaxAmplitude, gap * 0.4);
                    var straightLength = Math.Min(maxAmplitude, segmentLength / 6.0);
                    var zigzagWidth = Math.Max(0, segmentLength - 2 * straightLength);
                    var ampX = zigzagWidth / 6.0;
                    var ampY = ampX;

                    var p1 = x0 + straightLength;
                    var peakX = p1 + ampX;
                    var p2 = peakX + ampX;
                    var troughX = p2 + ampX;
                    var p3 = troughX + ampX;
                    var finalX = x1 - straightLength;

                    var peakY = baseY - ampY;
                    var troughY = baseY + ampY;

                    pathD.Append($" L {p1} {baseY}");
                    pathD.Append($" L {peakX} {peakY}");
                    pathD.Append($" L {p2} {baseY}");
                    pathD.Append($" L {troughX} {troughY}");
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
                new DataPoint { Id = "1", Label = "1", Year = "1", Next = 1 },
                new DataPoint { Id = "2", Label = "2", Year = "2", Next = 1 },
                new DataPoint { Id = "5", Label = "5", Year = "5", Next = 1 },
                new DataPoint { Id = "10", Label = "10", Year = "10", Next = 1 },
                new DataPoint { Id = "15", Label = "15", Year = "15", Next = 1 },
                new DataPoint { Id = "20", Label = "20", Year = "20", Next = 1 },
                new DataPoint { Id = "60", Major = true, Year = "A60", Next = 1 },
                new DataPoint { Id = "70", Major = true, Year = "A70", Next = 1 },
                new DataPoint { Id = "80", Major = true, Year = "A80", Next = 1 },
                new DataPoint { Id = "90", Major = true, Value = "150,000", Year = "A90", Next = 1, LastPayment = true }
            };
        }

        // Convert SVG string to PNG bytes, scaling picture to fit target size & pixel ratio
        private byte[] ConvertSvgToPng(string svgContent, int targetWidth, int targetHeight, float pixelRatio = 1f)
        {
            var outWidth = (int)Math.Max(1, Math.Round(targetWidth * pixelRatio));
            var outHeight = (int)Math.Max(1, Math.Round(targetHeight * pixelRatio));

            using var svg = new SKSvg();
            var picture = svg.FromSvg(svgContent);

            using var surface = SKSurface.Create(new SKImageInfo(outWidth, outHeight));
            var canvas = surface.Canvas;

            // Set background (white). ถ้าต้องการ transparency ให้ใช้ SKColors.Transparent
            canvas.Clear(SKColors.White);

            if (picture != null)
            {
                var bounds = picture.CullRect;
                float picW = bounds.Width > 0 ? (float)bounds.Width : (float)targetWidth;
                float picH = bounds.Height > 0 ? (float)bounds.Height : (float)targetHeight;

                var scaleX = outWidth / picW;
                var scaleY = outHeight / picH;
                var scale = Math.Min(scaleX, scaleY);

                var scaledPicW = picW * (float)scale;
                var scaledPicH = picH * (float)scale;
                var tx = (outWidth - scaledPicW) / 2f - (float)bounds.Left * (float)scale;
                var ty = (outHeight - scaledPicH) / 2f - (float)bounds.Top * (float)scale;

                canvas.Save();
                canvas.Translate(tx, ty);
                canvas.Scale((float)scale);
                canvas.DrawPicture(picture);
                canvas.Restore();
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        // Helper: read font file (ttf/otf) and return base64 string
        private string? EmbedFontBase64(string fontPath)
        {
            // If relative path given, resolve to content root
            if (!Path.IsPathRooted(fontPath))
            {
                var baseDir = AppContext.BaseDirectory; // application base
                fontPath = Path.Combine(baseDir, fontPath.Replace('/', Path.DirectorySeparatorChar));
            }

            if (!System.IO.File.Exists(fontPath)) return null;

            var bytes = System.IO.File.ReadAllBytes(fontPath);
            return Convert.ToBase64String(bytes);
        }

        // Helper: escape basic XML entities for texts
        private string EscapeXml(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }
    }
}
