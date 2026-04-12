using Microsoft.AspNetCore.Http;

namespace Application.DTOs
{
    public class VehicleTypeIconUploadRequest
    {
        public IFormFile? MovingIcon { get; set; }
        public IFormFile? StoppedIcon { get; set; }
        public IFormFile? IdleIcon { get; set; }
        public IFormFile? ParkedIcon { get; set; }
        public IFormFile? OfflineIcon { get; set; }
        public IFormFile? BreakdownIcon { get; set; }
    }

    public class VehicleTypeIconUploadResponseDto
    {
        public int VehicleTypeId { get; set; }
        public int AccountId { get; set; }
        public string? MovingIcon { get; set; }
        public string? StoppedIcon { get; set; }
        public string? IdleIcon { get; set; }
        public string? ParkedIcon { get; set; }
        public string? OfflineIcon { get; set; }
        public string? BreakdownIcon { get; set; }
    }
}
