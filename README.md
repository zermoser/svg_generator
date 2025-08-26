# InsuranceSvgGenerator (.NET 8)

This is a minimal .NET 8 Web API project containing `InsuranceSvgController` that generates SVG content from a JSON quotation payload.

เป็นโปรเจกต์ Mockup เท่านั้น ไม่มีการเชื่อมต่อฐานข้อมูลหรือบริการภายนอกใด ๆ เลยไม่ได้ทำเป็น Controller Service Model ดี ๆ

## Requirements
- .NET 8 SDK installed

## How to run
1. Extract the ZIP.
2. From the project folder run:
   ```bash
   dotnet restore
   dotnet run
   ```
3. The API will be available at `https://localhost:5001` (or the port printed in the console).
4. Example request (POST JSON to `/api/InsuranceSvg/generate`):



```json mock for request (example)
{
  "Data": [
    {
      "Id": "1",
      "Label": "1",
      "Year": "1",
      "Next": 1
    },
    {
      "Id": "2",
      "Label": "2",
      "Year": "2",
      "Next": 1
    },
    {
      "Id": "5",
      "Label": "5",
      "Year": "5",
      "Next": 1
    },
    {
      "Id": "10",
      "Label": "10",
      "Year": "10",
      "Next": 1
    },
    {
      "Id": "15",
      "Label": "15",
      "Year": "15",
      "Next": 1
    },
    {
      "Id": "20",
      "Major": true,
      "Label": "20",
      "Year": "20",
      "Next": 1
    },
    {
      "Id": "60",
      "Major": true,
      "Year": "A60",
      "Next": 1
    },
    {
      "Id": "70",
      "Major": true,
      "Year": "A70",
      "Next": 1
    },
    {
      "Id": "80",
      "Major": true,
      "Year": "A80",
      "Next": 1
    },
    {
      "Id": "90",
      "Value": "150,000",
      "Year": "A90",
      "Next": 1,
      "LastPayment": true
    }
  ]
}
```



```json mock for request (Full)
{
  "Data": [
    { "Id": "1",  "Label": "1",  "Year": "1",   "Next": 1, "Major": false, "LastPayment": false, "DivideSa": false },
    { "Id": "2",  "Label": "2",  "Year": "2",   "Next": 1, "Major": false, "LastPayment": false, "DivideSa": false },
    { "Id": "5",  "Label": "5",  "Year": "5",   "Next": 1, "Major": false, "LastPayment": false, "DivideSa": false },
    { "Id": "10", "Label": "10", "Year": "10",  "Next": 1, "Major": false, "LastPayment": false, "DivideSa": false },
    { "Id": "15", "Label": "15", "Year": "15",  "Next": 1, "Major": false, "LastPayment": false, "DivideSa": false },
    { "Id": "20", "Label": "20", "Year": "20",  "Next": 1, "Major": true,  "LastPayment": false, "DivideSa": false },
    { "Id": "60", "Label": "60",                "Year": "A60", "Next": 1, "Major": true,  "LastPayment": false, "DivideSa": false },
    { "Id": "70", "Label": "70",                "Year": "A70", "Next": 1, "Major": true,  "LastPayment": false, "DivideSa": false },
    { "Id": "80", "Label": "80",                "Year": "A80", "Next": 1, "Major": true,  "LastPayment": false, "DivideSa": false },
    { "Id": "90", "Label": "90", "Value": "150,000", "Year": "A90", "Next": 1, "Major": true,  "LastPayment": true,  "DivideSa": false }
  ],
  "Height": 280,
  "MarginHorizontal": 40,
  "Color": "#2c8592",
  "DotFill": "#c9e04a",
  "Lang": "th",
  "Width": 800,
  "ZigzagMaxAmplitude": 20.0
}

```

# TimelineChartAPI

This archive contains the C# source files for a small ASP.NET Core service that generates timeline charts as SVG and PNG.

## Structure
- Models/ - Data models and chart configuration
- Services/ - SVG generation, localization and PNG rendering services
- Controllers/ - API controller exposing endpoints to generate SVG and PNG

## Notes
- The `PngRenderingService` uses PuppeteerSharp to render SVG to PNG. Ensure Chromium is available or allow BrowserFetcher to download it.
- You may need to create a .csproj and register services in Program.cs when integrating into your project.

Enjoy!
