using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class CreateAccountConfigurationRequest
{
    [Required]
    public int AccountId { get; set; }

    // Map
    public string MapProvider { get; set; } = "GoogleMaps";
    public string? LicenseKey { get; set; }
    public string? AddressKey { get; set; }

    // Internationalization
    public string DateFormat { get; set; } = "DD/MM/YYYY";
    public string TimeFormat { get; set; } = "12H";
    public string DistanceUnit { get; set; } = "KM";
    public string SpeedUnit { get; set; } = "KMH";
    public string FuelUnit { get; set; } = "LITRE";
    public string TemperatureUnit { get; set; } = "CELSIUS";
    public string AddressDisplay { get; set; } = "SHOW";

    // Language
    public string DefaultLanguage { get; set; } = "en";
    public List<string>? AllowedLanguages { get; set; }
}

public class UpdateAccountConfigurationRequest
{
    public string MapProvider { get; set; } = "GoogleMaps";
    public string? LicenseKey { get; set; }
    public string? AddressKey { get; set; }

    public string DateFormat { get; set; } = "DD/MM/YYYY";
    public string TimeFormat { get; set; } = "12H";
    public string DistanceUnit { get; set; } = "KM";
    public string SpeedUnit { get; set; } = "KMH";
    public string FuelUnit { get; set; } = "LITRE";
    public string TemperatureUnit { get; set; } = "CELSIUS";
    public string AddressDisplay { get; set; } = "SHOW";

    public string DefaultLanguage { get; set; } = "en";
    public List<string>? AllowedLanguages { get; set; }

    public bool IsActive { get; set; } = true;
}

public class AccountConfigurationResponseDto
{
    public int AccountConfigurationId { get; set; }
    public int AccountId { get; set; }
    public string AccountName { get; set; } = "";

    public string MapProvider { get; set; } = "";
    public string? licenseKey { get; set; }
    public string? AddressKey { get; set; }
    public string TimeFormat { get; set; } = "";
    public string DistanceUnit { get; set; } = "";
    public string SpeedUnit { get; set; } = "";
    public string FuelUnit { get; set; } = "";
    public string TemperatureUnit { get; set; } = "";
    public string AddressDisplay { get; set; } = "";

    public string DateFormat { get; set; } = "";
    public string DefaultLanguage { get; set; } = "";

    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}
