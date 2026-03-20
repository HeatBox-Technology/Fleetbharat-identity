using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Newtonsoft.Json;
using Application.DTOs;
using System.Collections.Generic;

namespace Infrastructure.LiveTracking
{
    public class LiveTrackingSimulatorService : BackgroundService
    {
        private readonly IConnectionMultiplexer _mux;
        private readonly ILogger<LiveTrackingSimulatorService> _logger;
        private readonly string[] _vehicleNos = new[] {
            "HR29CA6032", "DL8CAF5031", "MH12DE1433", "RJ14AB1234", "UP32HN5678",
            "KA01AB4321", "TN09CD8765", "GJ01XY2345", "PB10EF6789", "WB20GH3456"
        };
        private readonly Random _random = new Random();
        private readonly Dictionary<string, double> _lastSpeeds = new();
        private readonly Dictionary<string, (double lat, double lng)> _lastCoords = new();

        public LiveTrackingSimulatorService(IConnectionMultiplexer mux, ILogger<LiveTrackingSimulatorService> logger)
        {
            _mux = mux;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var db = _mux.GetDatabase();
            // Telangana bounding box (approximate)
            double minLat = 16.30, maxLat = 19.00;
            double minLng = 77.00, maxLng = 80.30;
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var vehicleNo in _vehicleNos)
                {
                    // Generate a new random location within Delhi NCR
                    var lat = minLat + _random.NextDouble() * (maxLat - minLat);
                    var lng = minLng + _random.NextDouble() * (maxLng - minLng);

                    // Random speed between 0 and 180, always different
                    double speed = Math.Round(_random.NextDouble() * 180, 1);
                    if (_lastSpeeds.TryGetValue(vehicleNo, out var lastSpeed))
                    {
                        while (speed == lastSpeed)
                            speed = Math.Round(_random.NextDouble() * 180, 1);
                    }
                    _lastSpeeds[vehicleNo] = speed;

                    var dto = new DeviceLiveTrackingDto
                    {
                        DeviceNo = vehicleNo,
                        Imei = "123456789012345",
                        Latitude = lat,
                        Longitude = lng,
                        Speed = speed,
                        Altitude = 0,
                        Direction = _random.Next(0, 360),
                        Ignition = true,
                        GpsDate = DateTime.UtcNow,
                        Id = vehicleNo,
                        VehicleNo = vehicleNo
                    };

                    var json = JsonConvert.SerializeObject(dto);
                    var redisKey = $"dashboard::{vehicleNo}";
                    await db.StringSetAsync(redisKey, json);
                    _logger.LogInformation($"Pushed simulated live tracking for {vehicleNo}: {json}");
                }
                await Task.Delay(30000, stoppingToken); // every 30 seconds
            }
        }
    }
}
