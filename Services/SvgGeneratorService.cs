using TimelineChartAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimelineChartAPI.Services
{
    public interface ISvgGeneratorService
    {
        string GenerateSvg(
            List<DataPoint> dataPoints,
            TimelineChartRequest request,
            Dictionary<string, string> texts,
            ChartConfiguration config);
    }

    public class SvgGeneratorService : ISvgGeneratorService
    {
        public string GenerateSvg(
            List<DataPoint> dataPoints,
            TimelineChartRequest request,
            Dictionary<string, string> texts,
            ChartConfiguration config)
        {
            var centerY = CalculateCenterY(request.Height);
            var points = CalculatePointPositions(dataPoints, request, centerY);
            var gap = CalculateGap(request, dataPoints.Count);

            var svg = new StringBuilder();
            BuildSvgStructure(svg, request, config);
            AddTitle(svg, request, texts, config);
            AddPath(svg, points, centerY, gap, request, config);
            AddDataPoints(svg, points, texts, config);
            AddYAxisLabel(svg, centerY, request, texts, config);
            AddLastPaymentIndicator(svg, points, request, texts, config);
            svg.AppendLine("</svg>");

            return svg.ToString();
        }

        private double CalculateCenterY(int height) => Math.Round((double)height / 2 + 20);

        private double CalculateGap(TimelineChartRequest request, int dataCount)
        {
            var availableWidth = Math.Max(200, request.Width - request.MarginHorizontal * 2);
            return (double)availableWidth / Math.Max(1, dataCount - 1);
        }

        private List<PointPosition> CalculatePointPositions(
            List<DataPoint> dataPoints, 
            TimelineChartRequest request, 
            double centerY)
        {
            var gap = CalculateGap(request, dataPoints.Count);
            return dataPoints.Select((point, index) => new PointPosition
            {
                Point = point,
                X = request.MarginHorizontal + gap * index,
                Y = centerY,
                Index = index
            }).ToList();
        }

        private void BuildSvgStructure(StringBuilder svg, TimelineChartRequest request, ChartConfiguration config)
        {
            svg.AppendLine($"<svg viewBox=\"0 0 {request.Width} {request.Height}\" xmlns=\"http://www.w3.org/2000/svg\">");
            svg.AppendLine($"<rect width=\"{request.Width}\" height=\"{request.Height}\" fill=\"{config.BackgroundColor}\"/>");
        }

        private void AddTitle(StringBuilder svg, TimelineChartRequest request, Dictionary<string, string> texts, ChartConfiguration config)
        {
            var centerX = request.Width / 2;
            svg.AppendLine($"<text x=\"{centerX}\" y=\"25\" font-size=\"16\" font-weight=\"600\" text-anchor=\"middle\" fill=\"#333\" font-family=\"{config.FontFamily}\">{EscapeXml(texts["coverage"])}</text>");
            svg.AppendLine($"<text x=\"{centerX}\" y=\"42\" font-size=\"14\" text-anchor=\"middle\" fill=\"#666\" font-family=\"{config.FontFamily}\">{EscapeXml(texts["or"])}</text>");
        }

        private void AddPath(StringBuilder svg, List<PointPosition> points, double centerY, double gap, TimelineChartRequest request, ChartConfiguration config)
        {
            var pathData = GenerateZigzagPath(points, centerY, gap, request.ZigzagMaxAmplitude);
            svg.AppendLine($"<path d=\"{pathData}\" stroke=\"{request.Color}\" stroke-width=\"{config.StrokeWidth}\" fill=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
        }

        private void AddDataPoints(StringBuilder svg, List<PointPosition> points, Dictionary<string, string> texts, ChartConfiguration config)
        {
            foreach (var pointPos in points)
            {
                var point = pointPos.Point;
                AddDataPoint(svg, pointPos, texts, config);
            }
        }

        private void AddDataPoint(StringBuilder svg, PointPosition pointPos, Dictionary<string, string> texts, ChartConfiguration config)
        {
            var point = pointPos.Point;
            var x = pointPos.X;
            var y = pointPos.Y;

            // Draw circle
            svg.AppendLine($"<circle cx=\"{x}\" cy=\"{y}\" r=\"{config.DotRadius}\" fill=\"#c9e04a\" stroke=\"#2c8592\" stroke-width=\"2\"/>");

            // Add year label
            var labelY = point.Year?.Contains('A') == true ? y + 40 : y + 25;
            var yearText = point.Year?.Replace("A", "") ?? "";
            svg.AppendLine($"<text x=\"{x}\" y=\"{labelY}\" font-size=\"{config.SmallFontSize}\" text-anchor=\"middle\" fill=\"#333\" font-family=\"{config.FontFamily}\">{EscapeXml(yearText)}</text>");

            // Add age indicator
            if (point.Year?.Contains('A') == true)
            {
                svg.AppendLine($"<text x=\"{x}\" y=\"{y + 25}\" font-size=\"{config.SmallFontSize - 2}\" text-anchor=\"middle\" fill=\"#333\" font-family=\"{config.FontFamily}\">{EscapeXml(texts["atAge"])}</text>");
            }

            // Add amount label
            if (point.Level.HasValue && point.Level >= 0 && !string.IsNullOrEmpty(point.AmountLabel))
            {
                var amountY = y - 40 - (point.Level.Value * 20);
                svg.AppendLine($"<text x=\"{x}\" y=\"{amountY}\" font-size=\"{config.SmallFontSize}\" font-weight=\"700\" text-anchor=\"middle\" fill=\"#333\" font-family=\"{config.FontFamily}\">{EscapeXml(point.AmountLabel)}</text>");
            }
        }

        private void AddYAxisLabel(StringBuilder svg, double centerY, TimelineChartRequest request, Dictionary<string, string> texts, ChartConfiguration config)
        {
            svg.AppendLine($"<text x=\"15\" y=\"{centerY}\" font-size=\"{config.FontSize}\" font-weight=\"600\" text-anchor=\"middle\" fill=\"#333\" font-family=\"{config.FontFamily}\" transform=\"rotate(-90 15 {centerY})\">{EscapeXml(texts["xAxisLabel"])}</text>");
        }

        private void AddLastPaymentIndicator(StringBuilder svg, List<PointPosition> points, TimelineChartRequest request, Dictionary<string, string> texts, ChartConfiguration config)
        {
            var lastPaymentPoint = points.FirstOrDefault(p => p.Point.LastPayment);
            if (lastPaymentPoint?.Point.Value == null) return;

            var x = lastPaymentPoint.X;
            var y = lastPaymentPoint.Y;

            svg.AppendLine($"<line x1=\"{x}\" y1=\"60\" x2=\"{x}\" y2=\"{y - 20}\" stroke=\"{request.Color}\" stroke-width=\"3\"/>");
            svg.AppendLine($"<polygon points=\"{x - 6},{y - 23} {x + 6},{y - 23} {x},{y - 15}\" fill=\"#1b1b1bff\"/>");
            svg.AppendLine($"<text x=\"{x}\" y=\"52\" font-size=\"16\" font-weight=\"700\" text-anchor=\"middle\" fill=\"#333\" font-family=\"{config.FontFamily}\">{EscapeXml(lastPaymentPoint.Point.Value)}</text>");
            svg.AppendLine($"<text x=\"{x}\" y=\"{y + 60}\" font-size=\"{config.SmallFontSize}\" text-anchor=\"middle\" fill=\"#333\" font-family=\"{config.FontFamily}\">{EscapeXml(texts["premiumEnd"])}</text>");
        }

        private string GenerateZigzagPath(List<PointPosition> points, double centerY, double gap, double zigzagMaxAmplitude)
        {
            if (points?.Count == 0) return string.Empty;

            var pathBuilder = new StringBuilder();
            pathBuilder.Append($"M {points[0].X} {centerY}");

            for (int i = 1; i < points.Count; i++)
            {
                var previousPoint = points[i - 1];
                var currentPoint = points[i];

                if (previousPoint.Point.Major)
                {
                    AppendZigzagSegment(pathBuilder, previousPoint.X, currentPoint.X, centerY, gap, zigzagMaxAmplitude);
                }
                else
                {
                    pathBuilder.Append($" L {currentPoint.X} {centerY}");
                }
            }

            return pathBuilder.ToString();
        }

        private void AppendZigzagSegment(StringBuilder pathBuilder, double x0, double x1, double baseY, double gap, double zigzagMaxAmplitude)
        {
            var maxAmp = Math.Min(zigzagMaxAmplitude, gap * 0.4);
            var straightLength = Math.Min(maxAmp, (x1 - x0) / 6.0);
            var zigzagWidth = Math.Max(0, (x1 - x0) - 2 * straightLength);
            var ampX = zigzagWidth / 6.0;
            var ampY = ampX;

            var controlPoints = new[]
            {
                x0 + straightLength,
                x0 + straightLength + ampX,
                x0 + straightLength + 2 * ampX,
                x0 + straightLength + 3 * ampX,
                x0 + straightLength + 4 * ampX,
                x1 - straightLength
            };

            pathBuilder.Append($" L {controlPoints[0]} {baseY}");
            pathBuilder.Append($" L {controlPoints[1]} {baseY - ampY}");
            pathBuilder.Append($" L {controlPoints[2]} {baseY}");
            pathBuilder.Append($" L {controlPoints[3]} {baseY + ampY}");
            pathBuilder.Append($" L {controlPoints[4]} {baseY}");
            pathBuilder.Append($" L {controlPoints[5]} {baseY}");
            pathBuilder.Append($" L {x1} {baseY}");
        }

        private static string EscapeXml(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
