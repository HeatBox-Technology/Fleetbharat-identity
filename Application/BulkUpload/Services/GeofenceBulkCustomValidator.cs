using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite;
using NetTopologySuite.Geometries;

public class GeofenceBulkCustomValidator : IBulkCustomValidator
{
    public string ModuleKey => "geofence";

    public Task<List<string>> ValidateAsync(
        Dictionary<string, string> row,
        object dto,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (dto is not CreateGeofenceDto geofenceDto)
            return Task.FromResult(errors);

        geofenceDto.Coordinates = NormalizeCoordinates(geofenceDto.Coordinates, geofenceDto.CoordinatesJson);

        if (string.IsNullOrWhiteSpace(geofenceDto.UniqueCode))
            errors.Add("Unique code is required.");

        var geometryType = geofenceDto.GeometryType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(geometryType))
        {
            errors.Add("Geometry type is required.");
            return Task.FromResult(errors);
        }

        if (geometryType != "CIRCLE" && geometryType != "POLYGON")
        {
            errors.Add("Invalid geometry type.");
            return Task.FromResult(errors);
        }

        try
        {
            BuildGeometry(geometryType, geofenceDto.Coordinates);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        return Task.FromResult(errors);
    }

    private static List<CoordinateDto> NormalizeCoordinates(
        List<CoordinateDto>? coordinates,
        string? coordinatesJson)
    {
        if (coordinates != null && coordinates.Count > 0)
            return coordinates;

        if (string.IsNullOrWhiteSpace(coordinatesJson))
            return new List<CoordinateDto>();

        try
        {
            return JsonSerializer.Deserialize<List<CoordinateDto>>(coordinatesJson) ?? new List<CoordinateDto>();
        }
        catch
        {
            return new List<CoordinateDto>();
        }
    }

    private static Geometry BuildGeometry(string geometryType, List<CoordinateDto> coordinates)
    {
        if (coordinates == null || coordinates.Count == 0)
            throw new Exception("Coordinates are required");

        var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        if (geometryType == "CIRCLE")
        {
            var center = coordinates.First();
            return factory.CreatePoint(new Coordinate(center.Longitude, center.Latitude));
        }

        if (geometryType == "POLYGON")
        {
            if (coordinates.Count < 3)
                throw new Exception("Polygon requires at least 3 points");

            var points = coordinates
                .Select(x => new Coordinate(x.Longitude, x.Latitude))
                .Distinct()
                .ToList();

            if (!points.First().Equals2D(points.Last()))
                points.Add(points.First());

            var polygon = factory.CreatePolygon(points.ToArray());

            if (!polygon.IsValid)
                polygon = (Polygon)polygon.Buffer(0);

            if (!polygon.IsValid)
                throw new Exception("Invalid polygon geometry");

            return polygon;
        }

        throw new Exception("Invalid geometry type");
    }
}
