using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace NopHoSoTuDong.Models
{
    public class ServiceInfo
    {
        [JsonPropertyName("domainCode")]
        public string? DomainCode { get; set; }

        [JsonPropertyName("serviceCode")]
        public string? ServiceCode { get; set; }

        [JsonPropertyName("serviceName")]
        public string? ServiceName { get; set; }

        public string Display => $"{ServiceCode} - {ServiceName}";
    }

    public class ServiceInfoData
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("data")]
        public List<ServiceInfo>? Data { get; set; }
    }
}
