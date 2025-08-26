using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InsuranceSvgGenerator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsuranceSvgController : ControllerBase
    {
        private readonly Dictionary<int, int> _yieldHeight = new()
        {
            [-1] = 15, [0] = 25, [1] = 35, [2] = 45, [3] = 55, [4] = 65,
            [5] = 75, [6] = 85, [7] = 95, [8] = 105, [9] = 115, [10] = 125, [11] = 250, [99] = 0
        };

        private readonly Dictionary<string, Dictionary<string, string>> _textResources = new()
        {
            ["xAxisLabelText"] = new Dictionary<string, string>
            {
                ["th"] = "สิ้นปีกรมธรรม์ที่",
                ["en"] = "End of Year"
            },
            ["premiumEndText"] = new Dictionary<string, string>
            {
                ["th"] = "ชำระเบี้ยครบ",
                ["en"] = "Premium Payment Finished"
            },
            ["atAgeText"] = new Dictionary<string, string>
            {
                ["th"] = "ครบอายุ",
                ["en"] = "At age"
            },
            ["coverageText"] = new Dictionary<string, string>
            {
                ["th"] = "ความคุ้มครองชีวิต*",
                ["en"] = "Death coverage*"
            },
            ["numOfMonths"] = new Dictionary<string, string>
            {
                ["th"] = "จำนวนเท่า",
                ["en"] = "Number of Months"
            },
            ["cb1"] = new Dictionary<string, string>
            {
                ["th"] = " ",
                ["en"] = "Coverage and benefit"
            },
            ["cb2"] = new Dictionary<string, string>
            {
                ["th"] = "จำนวนผลประโยชน์ที่จะได้รับทั้งหมด",
                ["en"] = "Start from the month of death or being total and"
            },
            ["cb3"] = new Dictionary<string, string>
            {
                ["th"] = "(จำนวนเท่าของจำนวนเงินเอาประกัน)",
                ["en"] = "permanent disability until the end of policy term"
            },
            ["yearDeath"] = new Dictionary<string, string>
            {
                ["th"] = "ต้นปีกรมธรรม์ที่เสียชีวิต",
                ["en"] = "Year of death"
            }
        };

        [HttpPost("generate")]
        public IActionResult GenerateSvg([FromBody] QuotationRequest request)
        {
            try
            {
                var svgContent = GenerateSvgInternal(request.Quotation, request.Language);
                return Content(svgContent, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating SVG: {ex.Message}");
            }
        }

        private string GenerateSvgInternal(QuotationModel quotation, string language)
        {
            var paramsModel = quotation.ProSelection.SiModel;

            if (string.IsNullOrEmpty(paramsModel.PlanCode))
                return string.Empty;

            // Extract plan code
            var planCodeParts = paramsModel.PlanCode.Split(new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" },
                StringSplitOptions.RemoveEmptyEntries);
            var code = planCodeParts.Length > 0 ? planCodeParts[0] : paramsModel.PlanCode;

            return code == "MC" 
                ? GenerateMcSvg(quotation, language, paramsModel) 
                : GenerateStandardSvg(quotation, language, paramsModel, code);
        }

        private string GenerateStandardSvg(QuotationModel quotation, string language, SiModelModel paramsModel, string code)
        {
            // Text resources
            var xAxisLabelText = GetText("xAxisLabelText", language);
            var premiumEndText = GetText("premiumEndText", language);
            var atAgeText = GetText("atAgeText", language);
            var coverageText = GetText("coverageText", language);

            // Padding values
            var padding = new { width = 30, height = 10 };
            var paddingLastYear = new { width = 40, height = 10 };

            // Process SA text based on plan type
            ProcessSaText(quotation, paramsModel, code, language);

            // Find payment step
            var paymentStep = FindPaymentStep(paramsModel.Steps);
            var findLastPayment = paymentStep == paramsModel.Steps.Count - 1;

            // Calculate step sizes
            var stepSizes = CalculateStepSizes(paramsModel);

            // Augment steps with additional data
            AugmentSteps(paramsModel.Steps, stepSizes, quotation, code);

            // Calculate divide positions for header types 3 and 5
            var dividePositions = CalculateDividePositions(paramsModel.Steps, paramsModel.SaHeaderType);

            // Create SVG builder
            var svgWidth = paramsModel.LayoutBox.Width + paramsModel.GraphXPadding +
                (findLastPayment ? paddingLastYear.width + padding.width : padding.width);
            var svgHeight = paramsModel.LayoutBox.Height + padding.height + 15;

            var svgBuilder = new SvgBuilder(svgWidth, svgHeight);

            // Add markers
            svgBuilder.AddMarker("Triangle", paramsModel.Marker.MarkerRefX, paramsModel.Marker.MarkerRefY,
                paramsModel.Marker.MarkerWidth, paramsModel.Marker.MarkerHeight, "M 0 0 L 10 5 L 0 10 z");

            svgBuilder.AddMarker("TriangleStart", paramsModel.Marker.MarkerRefX, paramsModel.Marker.MarkerRefY,
                paramsModel.Marker.MarkerWidth, paramsModel.Marker.MarkerHeight, "M -2 5 L 8 0 L 8 10 z");

            // Create header
            // Pass SA from quotation.ProSelection.SA into CreateHeader to avoid accessing paramsModel.SA
            var headerSvg = svgBuilder.CreateSubSvg(paramsModel.GraphXPadding, 0,
                paramsModel.LayoutBox.Width, paramsModel.MsgBox.Height);

            var upperBBox = CreateHeader(headerSvg, svgBuilder, paramsModel, dividePositions, language, quotation.ProSelection.SA);

            // Create main axis
            var maxLevel = paramsModel.Steps.Max(s => s.Level ?? -1);
            var axisY = upperBBox.Y + upperBBox.Height + paramsModel.GraphYPadding +
                paramsModel.TextLabelHeight + _yieldHeight[maxLevel];

            var xAxisSvg = svgBuilder.CreateSubSvg(paramsModel.GraphXPadding, axisY, 300, 240);

            // Add circles for year markers
            foreach (var step in paramsModel.Steps)
            {
                xAxisSvg.Add(svgBuilder.CreateCircle(step.X, 0, paramsModel.YearMarker.R));
            }

            // Add x-axis label
            svgBuilder.CreateText(0, axisY + paramsModel.XAxisLabelOffset, xAxisLabelText,
                "Tahoma", paramsModel.FontSize.YAxisFontSize);

            // Add arrows and paths
            AddArrowsAndPaths(xAxisSvg, svgBuilder, paramsModel.Steps, paramsModel);

            // Add text labels
            AddTextLabels(xAxisSvg, svgBuilder, paramsModel.Steps, paramsModel, language, atAgeText);

            // Add premium end arrow and label
            AddPremiumEndIndicator(xAxisSvg, svgBuilder, paramsModel.Steps, paymentStep,
                paramsModel, premiumEndText, language);

            return svgBuilder.ToString();
        }

        private string GenerateMcSvg(QuotationModel quotation, string language, SiModelModel paramsModel)
        {
            // Text resources for MC plan
            var numOfMonths = GetText("numOfMonths", language);
            var cb1 = GetText("cb1", language);
            var cb2 = GetText("cb2", language);
            var cb3 = GetText("cb3", language);
            var yearDeath = GetText("yearDeath", language);

            // Padding values for MC
            var paddingMC = new { width = 150, height = 50 };

            // Calculate step sizes
            var stepSizes = CalculateStepSizes(paramsModel);

            // Augment steps with additional data
            AugmentSteps(paramsModel.Steps, stepSizes, quotation, "MC");

            // Create SVG builder
            var svgWidth = paramsModel.LayoutBox.Width + paramsModel.GraphXPadding + paddingMC.width;
            var svgHeight = paramsModel.LayoutBox.Height + paddingMC.height;

            var svgBuilder = new SvgBuilder(svgWidth, svgHeight);

            // Add markers
            svgBuilder.AddMarker("Triangle", paramsModel.Marker.MarkerRefX, paramsModel.Marker.MarkerRefY,
                paramsModel.Marker.MarkerWidth, paramsModel.Marker.MarkerHeight, "M 0 0 L 10 5 L 0 10 z");

            svgBuilder.AddMarker("TriangleStart", paramsModel.Marker.MarkerRefX, paramsModel.Marker.MarkerRefY,
                paramsModel.Marker.MarkerWidth, paramsModel.Marker.MarkerHeight, "M -2 5 L 8 0 L 8 10 z");

            // Create X-axis SVG
            var xAxisSvg = svgBuilder.CreateSubSvg(paramsModel.GraphXPadding, 0, 300, 240);

            // Calculate slope for MC visualization
            var slope = (0 - _yieldHeight[11]) / (paramsModel.Steps[paramsModel.Steps.Count - 1].X - paramsModel.Steps[0].X);

            // Add amount labels
            foreach (var step in paramsModel.Steps)
            {
                if (step.Level == null)
                {
                    var yPos = (int)(step.X * (-slope)) - _yieldHeight[11];
                    var year = int.Parse(step.Year.Replace("A", ""));

                    if (year < 4 || year > 6)
                    {
                        xAxisSvg.Add(svgBuilder.CreateText(step.X, yPos, step.AmountLabel,
                            "Tahoma", paramsModel.FontSize.YearLabelFontSize, "middle"));
                    }
                }
            }

            // Add main path
            var lastStep = paramsModel.Steps[paramsModel.Steps.Count - 1];
            xAxisSvg.Add(svgBuilder.CreatePath($"M {lastStep.X},0",
                markerEnd: "url(#Triangle)", strokeWidth: 2));

            // Add sloped path
            var firstStep = paramsModel.Steps[0];
            var secondLastStep = paramsModel.Steps[paramsModel.Steps.Count - 2];
            xAxisSvg.Add(svgBuilder.CreatePath(
                $"M {firstStep.X},{-_yieldHeight[11] + (lastStep.X - secondLastStep.X) / 2} " +
                $"L {lastStep.X - (lastStep.X - secondLastStep.X) / 2},0",
                strokeWidth: 2));

            // Add additional MC-specific elements
            // This is a simplified implementation - you would need to add the full MC logic here

            return svgBuilder.ToString();
        }

        // Helper methods and classes...

        private string GetText(string key, string language)
        {
            if (_textResources.ContainsKey(key) && _textResources[key].ContainsKey(language))
                return _textResources[key][language];

            return string.Empty;
        }

        private string ReplaceSaText(int saTextIndex, string language, dynamic allData)
        {
            var saTexts = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    ["th"] = $"ความคุ้มครอง = {allData.SA} บาท",
                    ["en"] = $"Death coverage = {allData.SA} baht"
                },
                new Dictionary<string, string>
                {
                    ["th"] = "ความคุ้มครองกรณีเสียชีวิต : จำนวนที่มากกว่าระหว่าง ทุนประกันภัย หรือ มูลค่าเวนคืนเงินสด หรือ เบี้ยประกันภัยสะสม",
                    ["en"] = "Death Coverage : The most value among sum insured or cash value or accumulated premium"
                },
                // Add all 37 text variations here...
                new Dictionary<string, string>
                {
                    ["th"] = "ความคุ้มครองกรณีเสียชีวิต : จำนวนที่มากกว่าระหว่าง 100% สำหรับ 3 ปีแรก, 200% สำหรับปีที่ 4 เป็นต้นไป จนครบอายุ 80 ปี ของทุนประกันเริ่มต้น หรือ มูลค่าเวนคืนเงินสด หรือเบี้ยประกันภัยสะสม",
                    ["en"] = "Death Coverage : The most value among 100% for year 1-3, 200% for year 4 at age 80 year of sum insured or cash value or accumulated premium"
                }
            };

            if (saTextIndex >= 0 && saTextIndex < saTexts.Count)
                return saTexts[saTextIndex][language];

            return string.Empty;
        }

        private void ProcessSaText(QuotationModel quotation, SiModelModel paramsModel, string code, string language)
        {
            var proSelection = quotation.ProSelection;
            var saValue = FormatNumber(proSelection.SA);

            if (paramsModel.SaHeaderType == 2)
            {
                ProcessSaHeaderType2(quotation, paramsModel, saValue, language);
            }
            else if (paramsModel.SaHeaderType == 4)
            {
                ProcessSaHeaderType4(quotation, paramsModel, saValue, language);
            }
            else
            {
                ProcessStandardSaText(quotation, paramsModel, code, saValue, language);
            }
        }

        private int FindPaymentStep(List<StepModel> steps)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i].LastPayment)
                    return i;
            }
            return steps.Count - 1;
        }

        private Dictionary<int, int> CalculateStepSizes(SiModelModel paramsModel)
        {
            var counts = new Dictionary<int, int>
            {
                [0] = paramsModel.Steps.Count(s => s.Next == 0),
                [1] = paramsModel.Steps.Count(s => s.Next == 1),
                [2] = paramsModel.Steps.Count(s => s.Next == 2)
            };

            var largeSpace = (paramsModel.LayoutBox.Width - (counts[0] * paramsModel.SmallSpace) -
                (counts[2] * (paramsModel.WarpGap + paramsModel.WarpSpace))) / counts[1];

            return new Dictionary<int, int>
            {
                [0] = paramsModel.SmallSpace,
                [1] = largeSpace,
                [2] = paramsModel.WarpGap + paramsModel.WarpSpace
            };
        }

        private void AugmentSteps(List<StepModel> steps, Dictionary<int, int> stepSizes, QuotationModel quotation, string code)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                step.Length = stepSizes[step.Next];

                // Calculate X positions
                if (i == 0)
                {
                    step.X = 0;
                    step.NextX = step.Length;
                }
                else
                {
                    step.X = steps[i - 1].X + steps[i - 1].Length;
                    if (i < steps.Count - 1)
                        step.NextX = step.X + step.Length;
                }

                // Calculate line coordinates for arrows
                if (step.Level.HasValue && step.Level.Value != -1)
                {
                    step.LineCoords = new List<Point>
                    {
                        new Point(step.X, 0),
                        new Point(step.X, -_yieldHeight[step.Level.Value])
                    };
                }

                // Find benefit amount
                if (code != "MC")
                {
                    var realYear = step.Year;
                    if (step.Year.Contains("A"))
                    {
                        var yearPart = step.Year.Substring(1);
                        if (int.TryParse(yearPart, out int year))
                        {
                            realYear = (year - quotation.InsAge).ToString();
                        }
                    }

                    if (int.TryParse(realYear, out int endYear))
                    {
                        var benefit = FindBenefit(quotation.CVRate, endYear);
                        if (benefit != null)
                        {
                            step.AmountLabel = FormatNumber(benefit.CashBenefit);
                        }
                    }
                }
            }
        }

        private CoverageData FindBenefit(CVRateModel cvRate, int endYear)
        {
            var benefit = cvRate.Data1?.FirstOrDefault(d => d.EndYear == endYear) ??
                         cvRate.Data2?.FirstOrDefault(d => d.EndYear == endYear) ??
                         cvRate.Data3?.FirstOrDefault(d => d.EndYear == endYear) ??
                         cvRate.Data4?.FirstOrDefault(d => d.EndYear == endYear);

            return benefit;
        }

        private List<int> CalculateDividePositions(List<StepModel> steps, int? saHeaderType)
        {
            var dividePositions = new List<int>();

            if (saHeaderType == 3 || saHeaderType == 5)
            {
                var cnt = 0;
                foreach (var step in steps)
                {
                    if (step.DivideSa)
                    {
                        if (cnt == 0)
                        {
                            dividePositions.Add(step.X);
                        }
                        else if (cnt == 1)
                        {
                            dividePositions.Add(step.X);
                        }
                        cnt++;
                    }
                }
            }

            return dividePositions;
        }

        private BoundingBox CreateHeader(XElement headerSvg, SvgBuilder svgBuilder, SiModelModel paramsModel,
            List<int> dividePositions, string language, decimal saAmount)
        {
            var headerType = paramsModel.SaHeaderType ?? 0;

            switch (headerType)
            {
                case 0:
                    return CreateHeaderType0(headerSvg, svgBuilder, paramsModel, language, saAmount);
                case 2:
                    return CreateHeaderType2(headerSvg, svgBuilder, paramsModel, language, saAmount);
                case 3:
                    return CreateHeaderType3(headerSvg, svgBuilder, paramsModel, dividePositions, language, saAmount);
                case 4:
                    return CreateHeaderType4(headerSvg, svgBuilder, paramsModel, language, saAmount);
                case 5:
                    return CreateHeaderType5(headerSvg, svgBuilder, paramsModel, dividePositions, language, saAmount);
                default:
                    return new BoundingBox { X = 0, Y = 0, Width = 500, Height = 0 };
            }
        }

        private BoundingBox CreateHeaderType0(XElement headerSvg, SvgBuilder svgBuilder, SiModelModel paramsModel, string language, decimal saAmount)
        {
            var headerTextBox = new
            {
                X = paramsModel.MsgBox.X + (paramsModel.LayoutBox.Width - paramsModel.MsgBox.Width) / 2,
                Y = 0
            };

            // Create message box
            headerSvg.Add(svgBuilder.CreateRect(headerTextBox.X, headerTextBox.Y,
                paramsModel.MsgBox.Width, paramsModel.MsgBox.Height));

            // Use the passed-in saAmount (formatted) instead of paramsModel.SA
            var saText = ReplaceSaText(paramsModel.SaText, language, new { SA = FormatNumber(saAmount) });

            // Create header text
            var headerText = svgBuilder.CreateText(headerTextBox.X, headerTextBox.Y,
                saText,
                "Tahoma", paramsModel.FontSize.MsgBoxFontSize, "middle");

            headerSvg.Add(headerText);

            // Wrap text (simplified implementation)
            var textLines = WrapText(
                saText,
                paramsModel.MsgBox.Width, paramsModel.FontSize.MsgBoxFontSize);

            var textHeight = textLines.Count * paramsModel.FontSize.MsgBoxFontSize * 1.2;

            // Create arrows
            var arrowY = (int)(headerTextBox.Y + textHeight / 2);

            headerSvg.Add(svgBuilder.CreatePath(
                $"M {headerTextBox.X - paramsModel.ArrowPadding},{arrowY} L 0,{arrowY}",
                markerEnd: "url(#Triangle)", strokeWidth: paramsModel.StrokeWidth));

            headerSvg.Add(svgBuilder.CreatePath(
                $"M {headerTextBox.X + paramsModel.MsgBox.Width + paramsModel.ArrowPadding},{arrowY} L {paramsModel.LayoutBox.Width},{arrowY}",
                markerEnd: "url(#Triangle)", strokeWidth: paramsModel.StrokeWidth));

            // Add remark if needed
            if (paramsModel.Remark)
            {
                var coverageText = GetText("coverageText", language);
                headerSvg.Add(svgBuilder.CreateText(-90, paramsModel.MsgBox.Height - (int)(paramsModel.MsgBox.Height * 0.9),
                    coverageText, "Tahoma", 9));
            }

            return new BoundingBox
            {
                X = headerTextBox.X,
                Y = headerTextBox.Y,
                Width = paramsModel.MsgBox.Width,
                Height = (int)textHeight
            };
        }

        private BoundingBox CreateHeaderType2(XElement headerSvg, SvgBuilder svgBuilder, SiModelModel paramsModel, string language, decimal saAmount)
        {
            // Simplified implementation for header type 2
            return new BoundingBox { X = 0, Y = 0, Width = 500, Height = 100 };
        }

        private BoundingBox CreateHeaderType3(XElement headerSvg, SvgBuilder svgBuilder, SiModelModel paramsModel,
            List<int> dividePositions, string language, decimal saAmount)
        {
            // Simplified implementation for header type 3
            return new BoundingBox { X = 0, Y = 0, Width = 500, Height = 100 };
        }

        private BoundingBox CreateHeaderType4(XElement headerSvg, SvgBuilder svgBuilder, SiModelModel paramsModel, string language, decimal saAmount)
        {
            // Simplified implementation for header type 4
            return new BoundingBox { X = 0, Y = 0, Width = 500, Height = 100 };
        }

        private BoundingBox CreateHeaderType5(XElement headerSvg, SvgBuilder svgBuilder, SiModelModel paramsModel,
            List<int> dividePositions, string language, decimal saAmount)
        {
            // Simplified implementation for header type 5
            return new BoundingBox { X = 0, Y = 0, Width = 500, Height = 100 };
        }

        private void AddArrowsAndPaths(XElement xAxisSvg, SvgBuilder svgBuilder, List<StepModel> steps, SiModelModel paramsModel)
        {
            foreach (var step in steps)
            {
                // Add arrows
                if (step.Level.HasValue && step.Level.Value >= 0)
                {
                    var pathData = $"M {step.LineCoords[0].X},{step.LineCoords[0].Y} L {step.LineCoords[1].X},{step.LineCoords[1].Y}";
                    xAxisSvg.Add(svgBuilder.CreatePath(pathData,
                        markerEnd: "url(#Triangle)", strokeWidth: paramsModel.StrokeWidth));
                }

                // Add axes paths
                if (step.NextX > 0)
                {
                    string pathData;

                    if (step.Next == 0 || step.Next == 1)
                    {
                        // Straight line
                        pathData = $"M {step.X},0 L {step.NextX},0";
                    }
                    else if (step.Next == 2)
                    {
                        // Warped line
                        var warpWidth = paramsModel.WarpSpace - paramsModel.WarpGap;
                        var x1 = step.X + paramsModel.WarpGap;
                        var x2 = step.X + paramsModel.WarpGap + warpWidth * 0.25;
                        var x3 = step.X + paramsModel.WarpGap + warpWidth * 0.75;
                        var x4 = step.X + paramsModel.WarpGap + warpWidth;
                        var x5 = step.X + paramsModel.WarpGap + warpWidth + paramsModel.WarpGap;

                        pathData = $"M {step.X},0 L {x1},0 " +
                                   $"L {x2},{-paramsModel.WarpHeight} " +
                                   $"L {x3},{paramsModel.WarpHeight} " +
                                   $"L {x4},0 L {x5},0";
                    }
                    else
                    {
                        continue;
                    }

                    xAxisSvg.Add(svgBuilder.CreatePath(pathData, strokeWidth: paramsModel.StrokeWidth));
                }
            }
        }

        private void AddTextLabels(XElement xAxisSvg, SvgBuilder svgBuilder, List<StepModel> steps,
            SiModelModel paramsModel, string language, string atAgeText)
        {
            foreach (var step in steps)
            {
                // Add amount labels
                if (step.Level.HasValue && step.Level.Value >= 0)
                {
                    xAxisSvg.Add(svgBuilder.CreateText(step.X,
                        -_yieldHeight[step.Level.Value] - paramsModel.LabelArrowPadding,
                        step.AmountLabel, "Tahoma", paramsModel.FontSize.YearLabelFontSize, "middle"));
                }

                // Add year labels
                var yearLabel = step.Year.Replace("A", "");
                var yPos = paramsModel.LabelYearPadding;

                if (step.Year.Contains("A"))
                    yPos = paramsModel.LabelYearPadding * 2;

                xAxisSvg.Add(svgBuilder.CreateText(step.X, yPos, yearLabel,
                    "Tahoma", paramsModel.FontSize.YearLabelFontSize, "middle"));

                // Add "at age" labels for age-based steps
                if (step.Year.Contains("A"))
                {
                    xAxisSvg.Add(svgBuilder.CreateText(step.X, paramsModel.LabelYearPadding,
                        atAgeText, "Tahoma", paramsModel.FontSize.AtAgeLabelFontSize, "middle"));
                }
            }
        }

        private void AddPremiumEndIndicator(XElement xAxisSvg, SvgBuilder svgBuilder, List<StepModel> steps,
            int paymentStep, SiModelModel paramsModel, string premiumEndText, string language)
        {
            var step = steps[paymentStep];

            // Calculate start Y position
            var startY = paramsModel.LabelYearPadding + paramsModel.TextLabelHeight;
            if (step.Year.Contains("A"))
                startY += paramsModel.TextLabelHeight;

            // Add premium end arrow
            var arrowPath = $"M {step.X},{startY} L {step.X},{startY + paramsModel.ArrowPremiumLength}";
            xAxisSvg.Add(svgBuilder.CreatePath(arrowPath,
                markerEnd: "url(#Triangle)", strokeWidth: paramsModel.StrokeWidth));

            // Add premium end label
            var labelY = startY + paramsModel.LabelPremiumPadding + paramsModel.TextLabelHeight * 2 + paramsModel.ArrowPremiumLength;
            if (step.Year.Contains("A"))
                labelY += paramsModel.TextLabelHeight;

            xAxisSvg.Add(svgBuilder.CreateText(step.X, labelY, premiumEndText,
                "Tahoma", paramsModel.FontSize.PremiumEndFontSize, "middle"));
        }

        private void ProcessSaHeaderType2(QuotationModel quotation, SiModelModel paramsModel, string saValue, string language)
        {
            // Implementation for header type 2 processing
        }

        private void ProcessSaHeaderType4(QuotationModel quotation, SiModelModel paramsModel, string saValue, string language)
        {
            // Implementation for header type 4 processing
        }

        private void ProcessStandardSaText(QuotationModel quotation, SiModelModel paramsModel, string code, string saValue, string language)
        {
            // Implementation for standard SA text processing
        }

        private string FormatNumber(decimal value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        private List<string> WrapText(string text, int maxWidth, int fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            // Simple text wrapping implementation
            var words = text.Split(' ');
            var lines = new List<string>();
            var currentLine = new StringBuilder();
            var approximateCharWidth = fontSize * 0.6;
            var maxCharsPerLine = (int)(maxWidth / approximateCharWidth);

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 > maxCharsPerLine)
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }

                    // Handle very long words
                    if (word.Length > maxCharsPerLine)
                    {
                        var chunks = SplitLongWord(word, maxCharsPerLine);
                        foreach (var chunk in chunks)
                        {
                            lines.Add(chunk);
                        }
                        continue;
                    }
                }

                if (currentLine.Length > 0)
                    currentLine.Append(" ");

                currentLine.Append(word);
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine.ToString());

            return lines;
        }

        private List<string> SplitLongWord(string word, int maxLength)
        {
            var chunks = new List<string>();
            for (int i = 0; i < word.Length; i += maxLength)
            {
                chunks.Add(word.Substring(i, Math.Min(maxLength, word.Length - i)));
            }
            return chunks;
        }

        // Nested classes for models and helpers
        public class QuotationRequest
        {
            public QuotationModel Quotation { get; set; }
            public string Language { get; set; } = "th";
        }

        public class QuotationModel
        {
            public ProSelectionModel ProSelection { get; set; } = new ProSelectionModel();
            public CVRateModel CVRate { get; set; } = new CVRateModel();
            public int InsAge { get; set; }
        }

        public class ProSelectionModel
        {
            public SiModelModel SiModel { get; set; } = new SiModelModel();
            public decimal SA { get; set; }
            public int PlanPremiumTerm { get; set; }
        }

        public class SiModelModel
        {
            public string PlanCode { get; set; } = string.Empty;
            public int? SaHeaderType { get; set; }
            public int SaText { get; set; }
            public int SaText2 { get; set; }
            public int SaText3 { get; set; }
            public int SaText4 { get; set; }
            public List<StepModel> Steps { get; set; } = new List<StepModel>();
            public LayoutBoxModel LayoutBox { get; set; } = new LayoutBoxModel();
            public MsgBoxModel MsgBox { get; set; } = new MsgBoxModel();
            public MsgBoxModel MsgBoxL { get; set; } = new MsgBoxModel();
            public MsgBoxModel MsgBoxR { get; set; } = new MsgBoxModel();
            public MsgBoxModel MsgBoxM { get; set; } = new MsgBoxModel();
            public int SmallSpace { get; set; }
            public int WarpGap { get; set; }
            public int WarpSpace { get; set; }
            public int WarpHeight { get; set; }
            public int GraphXPadding { get; set; }
            public int GraphYPadding { get; set; }
            public MarkerModel Marker { get; set; } = new MarkerModel();
            public int StrokeWidth { get; set; } = 1;
            public FontSizeModel FontSize { get; set; } = new FontSizeModel();
            public int LabelArrowPadding { get; set; }
            public int LabelYearPadding { get; set; }
            public int LabelPremiumPadding { get; set; }
            public int ArrowPadding { get; set; }
            public int ArrowPremiumLength { get; set; }
            public int TextLabelHeight { get; set; }
            public int XAxisLabelOffset { get; set; }
            public YearMarkerModel YearMarker { get; set; } = new YearMarkerModel();
            public bool Remark { get; set; }
        }

        public class StepModel
        {
            public bool LastPayment { get; set; }
            public int Next { get; set; }
            public string Year { get; set; } = string.Empty;
            public int? Level { get; set; }
            public bool DivideSa { get; set; }
            public int Length { get; set; }
            public int X { get; set; }
            public int NextX { get; set; }
            public string AmountLabel { get; set; } = string.Empty;
            public List<Point> LineCoords { get; set; } = new List<Point>();
        }

        public class LayoutBoxModel
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public class MsgBoxModel
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }

        public class MarkerModel
        {
            public int MarkerRefX { get; set; } = 5;
            public int MarkerRefY { get; set; } = 5;
            public int MarkerWidth { get; set; } = 6;
            public int MarkerHeight { get; set; } = 6;
        }

        public class FontSizeModel
        {
            public int MsgBoxFontSize { get; set; } = 12;
            public int YearLabelFontSize { get; set; } = 12;
            public int YAxisFontSize { get; set; } = 12;
            public int AtAgeLabelFontSize { get; set; } = 10;
            public int PremiumEndFontSize { get; set; } = 12;
        }

        public class YearMarkerModel
        {
            public int R { get; set; } = 3;
        }

        public class CVRateModel
        {
            public List<CoverageData> Data1 { get; set; } = new List<CoverageData>();
            public List<CoverageData> Data2 { get; set; } = new List<CoverageData>();
            public List<CoverageData> Data3 { get; set; } = new List<CoverageData>();
            public List<CoverageData> Data4 { get; set; } = new List<CoverageData>();
        }

        public class CoverageData
        {
            public int EndYear { get; set; }
            public decimal Coverage { get; set; }
            public decimal CashBenefit { get; set; }
        }

        public class Point
        {
            public int X { get; set; }
            public int Y { get; set; }

            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        public class BoundingBox
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public class SvgBuilder
        {
            private readonly XNamespace _ns = "http://www.w3.org/2000/svg";
            private XElement _root;
            private XElement _defs;

            public SvgBuilder(int width, int height, bool overflowVisible = true)
            {
                _root = new XElement(_ns + "svg",
                    new XAttribute("width", width),
                    new XAttribute("height", height));

                if (overflowVisible)
                {
                    _root.Add(new XAttribute("style", "overflow: visible"));
                }

                _defs = new XElement(_ns + "defs");
                _root.Add(_defs);
            }

            public void AddMarker(string id, int refX, int refY, int markerWidth, int markerHeight, string pathData)
            {
                var marker = new XElement(_ns + "marker",
                    new XAttribute("id", id),
                    new XAttribute("viewBox", "0 0 10 10"),
                    new XAttribute("refX", refX),
                    new XAttribute("refY", refY),
                    new XAttribute("markerWidth", markerWidth),
                    new XAttribute("markerHeight", markerHeight),
                    new XAttribute("orient", "auto"),
                    new XAttribute("border", "1"),
                    new XElement(_ns + "path",
                        new XAttribute("d", pathData)));

                _defs.Add(marker);
            }

            public XElement CreateSubSvg(int x, int y, int width, int height, bool overflowVisible = true)
            {
                var svg = new XElement(_ns + "svg",
                    new XAttribute("x", x),
                    new XAttribute("y", y),
                    new XAttribute("width", width),
                    new XAttribute("height", height));

                if (overflowVisible)
                {
                    svg.Add(new XAttribute("style", "overflow: visible"));
                }

                _root.Add(svg);
                return svg;
            }

            public XElement CreatePath(string d, string stroke = "black", int strokeWidth = 1,
                string markerEnd = null, string markerStart = null, string strokeDasharray = null)
            {
                var path = new XElement(_ns + "path",
                    new XAttribute("d", d),
                    new XAttribute("stroke", stroke),
                    new XAttribute("stroke-width", strokeWidth));

                if (!string.IsNullOrEmpty(markerEnd))
                    path.Add(new XAttribute("marker-end", markerEnd));

                if (!string.IsNullOrEmpty(markerStart))
                    path.Add(new XAttribute("marker-start", markerStart));

                if (!string.IsNullOrEmpty(strokeDasharray))
                    path.Add(new XAttribute("stroke-dasharray", strokeDasharray));

                return path;
            }

            public XElement CreateText(int x, int y, string text, string fontFamily = "Tahoma",
                int fontSize = 12, string textAnchor = "start", string @class = null)
            {
                var textElement = new XElement(_ns + "text",
                    new XAttribute("x", x),
                    new XAttribute("y", y),
                    new XAttribute("font-family", fontFamily),
                    new XAttribute("font-size", fontSize),
                    new XAttribute("text-anchor", textAnchor),
                    text);

                if (!string.IsNullOrEmpty(@class))
                    textElement.Add(new XAttribute("class", @class));

                return textElement;
            }

            public XElement CreateRect(int x, int y, int width, int height, string fill = "none")
            {
                return new XElement(_ns + "rect",
                    new XAttribute("x", x),
                    new XAttribute("y", y),
                    new XAttribute("width", width),
                    new XAttribute("height", height),
                    new XAttribute("fill", fill));
            }

            public XElement CreateCircle(int cx, int cy, int r)
            {
                return new XElement(_ns + "circle",
                    new XAttribute("cx", cx),
                    new XAttribute("cy", cy),
                    new XAttribute("r", r));
            }

            public override string ToString()
            {
                return _root.ToString();
            }
        }
    }
}
