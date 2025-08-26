using System.Collections.Generic;

namespace TimelineChartAPI.Models
{
    public class DataPoint
    {
        public string Id { get; set; } = string.Empty;
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

    internal class PointPosition
    {
        public DataPoint Point { get; set; } = new DataPoint();
        public double X { get; set; }
        public double Y { get; set; }
        public int Index { get; set; }
    }

    public class ChartConfiguration
    {
        public double DotRadius { get; set; } = 8;
        public double StrokeWidth { get; set; } = 3;
        public int FontSize { get; set; } = 14;
        public int SmallFontSize { get; set; } = 12;
        public string FontFamily { get; set; } = "THSarabun, Tahoma, sans-serif";
        public string BackgroundColor { get; set; } = "#f8f9fa";
    }
}
