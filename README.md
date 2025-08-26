# InsuranceSvgGenerator (.NET 8)

This is a minimal .NET 8 Web API project containing `InsuranceSvgController` that generates SVG content from a JSON quotation payload.

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
  "quotation": {
    "proSelection": {
      "siModel": {
        "planCode": "SP10",
        "saHeaderType": 0,
        "saText": 0,
        "steps": [
          { "lastPayment": false, "next": 1, "year": "1", "level": 0 },
          { "lastPayment": false, "next": 1, "year": "2", "level": 1 },
          { "lastPayment": true, "next": 1, "year": "3", "level": 2 }
        ],
        "layoutBox": { "width": 600, "height": 400 },
        "msgBox": { "width": 300, "height": 60, "x": 0, "y": 0 },
        "smallSpace": 30,
        "warpGap": 5,
        "warpSpace": 50,
        "warpHeight": 20,
        "graphXPadding": 50,
        "graphYPadding": 20,
        "marker": { "markerRefX": 5, "markerRefY": 5, "markerWidth": 6, "markerHeight": 6 },
        "strokeWidth": 2,
        "fontSize": { "msgBoxFontSize": 14, "yearLabelFontSize": 12, "yAxisFontSize": 12, "atAgeLabelFontSize": 10, "premiumEndFontSize": 12 },
        "labelArrowPadding": 5,
        "labelYearPadding": 10,
        "labelPremiumPadding": 5,
        "arrowPadding": 10,
        "arrowPremiumLength": 30,
        "textLabelHeight": 15,
        "xAxisLabelOffset": 20,
        "yearMarker": { "r": 3 },
        "remark": true
      },
      "sa": 1000000,
      "planPremiumTerm": 10
    },
    "cvRate": {
      "data1": [
        { "endyear": 1, "coverage": 1000000, "cash_benefit": 50000 },
        { "endyear": 2, "coverage": 1100000, "cash_benefit": 60000 },
        { "endyear": 3, "coverage": 1200000, "cash_benefit": 70000 }
      ]
    },
    "insAge": 35
  },
  "language": "th"
}
```

## Notes
- The controller is mostly a direct port of the code you provided and includes simplified header-type implementations (2,3,4,5).
- If you want the controller split into smaller files (models/helpers), or full implementations for the header types / MC plan logic, tell me and I can update the project.
