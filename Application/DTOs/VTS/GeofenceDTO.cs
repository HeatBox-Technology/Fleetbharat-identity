using System;
using System.Collections.Generic;

public class CreateGeofenceDto
{
    public int AccountId { get; set; }
    public string UniqueCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string ClassificationCode { get; set; } = string.Empty;
    public string? ClassificationLabel { get; set; }
    public string GeometryType { get; set; } = string.Empty;   // CIRCLE / POLYGON
    public int? RadiusM { get; set; }
    public string CoordinatesJson { get; set; } = string.Empty;

    public List<CoordinateDto> Coordinates { get; set; } = new();

    public string? ColorTheme { get; set; }
    public decimal? Opacity { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int? CreatedBy { get; set; }
}
public class UpdateGeofenceDto
{
    public string UniqueCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string ClassificationCode { get; set; } = string.Empty;
    public string? ClassificationLabel { get; set; }

    public string CoordinatesJson { get; set; } = string.Empty;

    public string GeometryType { get; set; } = string.Empty;
    public int? RadiusM { get; set; }

    public List<CoordinateDto> Coordinates { get; set; } = new();

    public string? ColorTheme { get; set; }
    public decimal? Opacity { get; set; }

    public bool IsEnabled { get; set; }

    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
public class CoordinateDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
public class GeofenceDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string UniqueCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public string ClassificationCode { get; set; } = string.Empty;

    public string GeometryType { get; set; } = string.Empty;
    public int? RadiusM { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ColorTheme { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string CoordinatesJson { get; set; } = string.Empty;
    public List<CoordinateDto> Coordinates { get; set; } = new();
}
public class GeofenceSummaryDto
{
    public int TotalZones { get; set; }
    public int Enabled { get; set; }
    public int Disabled { get; set; }
}
public class GeofenceListUiResponseDto
{
    public GeofenceSummaryDto Summary { get; set; } = new();
    public PagedResultDto<GeofenceDto> Zones { get; set; } = new();
}