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

```json
{
  "Quotation": {
    "ProSelection": {
      "SiModel": {
        "PlanCode": "MC10",
        "SaHeaderType": 0,
        "SaText": 0,
        "SaText2": 0,
        "SaText3": 0,
        "SaText4": 0,
        "Steps": [
          {
            "LastPayment": false,
            "Next": 1,
            "Year": "1",
            "Level": 0,
            "DivideSa": false
          },
          {
            "LastPayment": true,
            "Next": 1,
            "Year": "2",
            "Level": 1,
            "DivideSa": false
          },
          {
            "LastPayment": false,
            "Next": 2,
            "Year": "3",
            "Level": 2,
            "DivideSa": false
          }
        ],
        "LayoutBox": {
          "Width": 500,
          "Height": 300
        },
        "MsgBox": {
          "Width": 300,
          "Height": 50,
          "X": 100,
          "Y": 0
        },
        "SmallSpace": 20,
        "WarpGap": 5,
        "WarpSpace": 30,
        "WarpHeight": 15,
        "GraphXPadding": 50,
        "GraphYPadding": 20,
        "Marker": {
          "MarkerRefX": 5,
          "MarkerRefY": 5,
          "MarkerWidth": 6,
          "MarkerHeight": 6
        },
        "StrokeWidth": 1,
        "FontSize": {
          "MsgBoxFontSize": 12,
          "YearLabelFontSize": 12,
          "YAxisFontSize": 12,
          "AtAgeLabelFontSize": 10,
          "PremiumEndFontSize": 12
        },
        "LabelArrowPadding": 5,
        "LabelYearPadding": 10,
        "LabelPremiumPadding": 5,
        "ArrowPadding": 10,
        "ArrowPremiumLength": 20,
        "TextLabelHeight": 15,
        "XAxisLabelOffset": 20,
        "YearMarker": {
          "R": 3
        },
        "Remark": true
      },
      "SA": 1000000,
      "PlanPremiumTerm": 10
    },
    "CVRate": {
      "Data1": [
        {
          "EndYear": 1,
          "Coverage": 1000000,
          "CashBenefit": 0
        },
        {
          "EndYear": 2,
          "Coverage": 2000000,
          "CashBenefit": 50000
        },
        {
          "EndYear": 3,
          "Coverage": 3000000,
          "CashBenefit": 100000
        }
      ],
      "Data2": [],
      "Data3": [],
      "Data4": []
    },
    "InsAge": 30
  },
  "Language": "th"
}
```

## Notes
- The controller is mostly a direct port of the code you provided and includes simplified header-type implementations (2,3,4,5).
- If you want the controller split into smaller files (models/helpers), or full implementations for the header types / MC plan logic, tell me and I can update the project.
