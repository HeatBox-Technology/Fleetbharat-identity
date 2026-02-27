using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("OemManufacturer")]
public class OemManufacturer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? OfficialWebsite { get; set; }

    [MaxLength(100)]
    public string? OriginCountry { get; set; }

    [MaxLength(150)]
    public string? SupportEmail { get; set; }

    [MaxLength(50)]
    public string? SupportHotline { get; set; }

    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;
}


[Table("mst_device_type")]
public class mst_device_type
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Unique code (ex: GPS, CAMERA)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Feature toggle (admin control)
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int oemmanufacturerid { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public bool IsActive { get; set; } = true;
}


public class DeviceCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}


public class NetworkProvider
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class VehicleBrandOem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class LeasedVendor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
