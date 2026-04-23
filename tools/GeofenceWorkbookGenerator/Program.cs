using ClosedXML.Excel;

var outputPath = args.Length > 0
    ? args[0]
    : Path.Combine(Directory.GetCurrentDirectory(), "geofence_bulk_test.xlsx");

using var workbook = new XLWorkbook();
var sheet = workbook.Worksheets.Add("Template");

var headers = new[]
{
    "AccountName",
    "UniqueCode",
    "DisplayName",
    "GeometryType",
    "Latitude",
    "Longitude",
    "GeoPoint",
    "RadiusM"
};

for (var i = 0; i < headers.Length; i++)
{
    sheet.Cell(1, i + 1).Value = headers[i];
}

sheet.Row(1).Style.Font.Bold = true;

sheet.Cell(2, 1).Value = "Demo Account";
sheet.Cell(2, 2).Value = "GF_TEST_CIRCLE_001";
sheet.Cell(2, 3).Value = "Test Circle Geofence";
sheet.Cell(2, 4).Value = "CIRCLE";
sheet.Cell(2, 5).Value = 28.6139;
sheet.Cell(2, 6).Value = 77.2090;
sheet.Cell(2, 8).Value = 500;

sheet.Cell(3, 1).Value = "Demo Account";
sheet.Cell(3, 2).Value = "GF_TEST_POLYGON_001";
sheet.Cell(3, 3).Value = "Test Polygon Geofence";
sheet.Cell(3, 4).Value = "POLYGON";
sheet.Cell(3, 7).Value = "28.6139,77.2090|28.6145,77.2105|28.6128,77.2110";

sheet.Columns().AdjustToContents();
workbook.SaveAs(outputPath);

Console.WriteLine(outputPath);
