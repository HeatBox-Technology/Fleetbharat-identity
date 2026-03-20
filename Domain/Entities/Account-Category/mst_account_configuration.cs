using System;

public class mst_account_configuration : IAccountEntity
{
    public int AccountConfigurationId { get; set; }

    public int AccountId { get; set; } // FK mst_account
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
    public string? AllowedLanguagesCsv { get; set; }  // "en,hi,es"

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedOn { get; set; }
    public bool IsDeleted { get; set; } = false;
}
