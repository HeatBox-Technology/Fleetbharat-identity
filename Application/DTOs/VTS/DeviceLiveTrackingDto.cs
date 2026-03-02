using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for device live tracking and current location data from Redis.
    /// Supports ISO date strings, epoch millis and array date formats.
    /// </summary>
    public class DeviceLiveTrackingDto
    {
        public string DeviceNo { get; set; } = string.Empty;
        public string Imei { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public double Altitude { get; set; }
        public int Direction { get; set; }
        public int OrgId { get; set; }
        public int? Rpm { get; set; }
        public string? NorthSouthLatitude { get; set; }
        public string? EastWestLongitude { get; set; }
        public bool Ignition { get; set; }
        public bool Ac { get; set; }
        public bool PowerCut { get; set; }
        public bool LowVoltage { get; set; }
        public bool DoorLock { get; set; }
        public bool DoorOpen { get; set; }
        public bool DeviceLock { get; set; }
        public bool FuelCut { get; set; }
        public bool GpsFixed { get; set; }
        public bool Collision { get; set; }

        [JsonConverter(typeof(FlexibleDateTimeConverter))]
        public DateTime GpsDate { get; set; }

        public bool Sos { get; set; }
        public bool OverSpeed { get; set; }
        public bool Fatigue { get; set; }
        public bool Danger { get; set; }
        public bool GnssFault { get; set; }
        public bool GnssAntennaDisconnect { get; set; }
        public bool GnssAntennaShort { get; set; }
        public bool PowerUnderVoltage { get; set; }
        public bool PowerDown { get; set; }
        public bool PowerDisplayFault { get; set; }
        public bool TtsFault { get; set; }
        public bool Rollover { get; set; }

        [JsonConverter(typeof(FlexibleNullableDateTimeConverter))]
        public DateTime? ReceivedAt { get; set; }

        public string Id { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;

        [JsonProperty("gpsDateMillis")]
        private long? GpsDateMillis
        {
            set
            {
                if (value.HasValue)
                {
                    var parsed = DateTimeOffset.FromUnixTimeMilliseconds(value.Value).UtcDateTime;
                    if (GpsDate == default)
                    {
                        GpsDate = parsed;
                    }
                }
            }
        }

        [JsonProperty("receivedAtMillis")]
        private long? ReceivedAtMillis
        {
            set
            {
                if (value.HasValue && !ReceivedAt.HasValue)
                {
                    ReceivedAt = DateTimeOffset.FromUnixTimeMilliseconds(value.Value).UtcDateTime;
                }
            }
        }
    }

    internal sealed class FlexibleDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            return ParseToken(token) ?? default;
        }

        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }

        internal static DateTime? ParseToken(JToken token)
        {
            if (token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token.Type == JTokenType.Date)
            {
                return token.Value<DateTime>();
            }

            if (token.Type == JTokenType.String)
            {
                var text = token.Value<string>();
                if (DateTime.TryParse(text, out var dt))
                {
                    return dt;
                }
                return null;
            }

            if (token.Type == JTokenType.Integer)
            {
                var millis = token.Value<long>();
                return DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime;
            }

            if (token.Type == JTokenType.Array)
            {
                var arr = (JArray)token;
                if (arr.Count < 3)
                {
                    return null;
                }

                int year = arr[0]?.Value<int>() ?? 1;
                int month = arr[1]?.Value<int>() ?? 1;
                int day = arr[2]?.Value<int>() ?? 1;
                int hour = arr.Count > 3 ? arr[3]?.Value<int>() ?? 0 : 0;
                int minute = arr.Count > 4 ? arr[4]?.Value<int>() ?? 0 : 0;
                int second = arr.Count > 5 ? arr[5]?.Value<int>() ?? 0 : 0;
                int nanosecond = arr.Count > 6 ? arr[6]?.Value<int>() ?? 0 : 0;

                long ticks = nanosecond / 100;
                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).AddTicks(ticks);
            }

            return null;
        }
    }

    internal sealed class FlexibleNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? ReadJson(JsonReader reader, Type objectType, DateTime? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            return FlexibleDateTimeConverter.ParseToken(token);
        }

        public override void WriteJson(JsonWriter writer, DateTime? value, JsonSerializer serializer)
        {
            if (value.HasValue)
            {
                writer.WriteValue(value.Value);
                return;
            }

            writer.WriteNull();
        }
    }
}
