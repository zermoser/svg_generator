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

```json mock for request
{
  "Width": 1000,
  "Height": 320,
  "MarginHorizontal": 50,
  "Color": "#2c8592",
  "DotFill": "#c9e04a",
  "Lang": "th",
  "ZigzagAtIndices": [4,5,7],
  "ZigzagMaxAmplitude": 20.0,
  "Data": [
    {"Id":"1","Label":"1","Year":"1","Next":1},
    {"Id":"2","Label":"2","Year":"2","Next":1},
    {"Id":"5","Label":"5","Year":"5","Next":1},
    {"Id":"10","Label":"10","Year":"10","Next":1},
    {"Id":"15","Label":"15","Year":"15","Next":1},
    {"Id":"20","Label":"20","Year":"20","Next":1},
    {"Id":"60","Major":true,"Year":"A60","Next":1},
    {"Id":"70","Major":true,"Year":"A70","Next":1},
    {"Id":"80","Major":true,"Year":"A80","Next":1},
    {"Id":"90","Major":true,"Value":"150,000","Year":"A90","Next":1,"LastPayment":true}
  ]
}

```

## Notes
- The controller is mostly a direct port of the code you provided and includes simplified header-type implementations (2,3,4,5).
- If you want the controller split into smaller files (models/helpers), or full implementations for the header types / MC plan logic, tell me and I can update the project.
