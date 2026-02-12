namespace Domain.Entities
{
    public class OemManufacturer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
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
}
