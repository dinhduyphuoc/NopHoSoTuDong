using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace NopHoSoTuDong.Models
{
    public class DossierPart
    {
        [JsonPropertyName("partTypeDetail")]
        public string? PartTypeDetail { get; set; }

        [JsonPropertyName("partName")]
        public string? PartName { get; set; }

        [JsonPropertyName("partType")]
        public int PartType { get; set; }

        [JsonPropertyName("partNo")]
        public string? PartNo { get; set; }
    }

    public class DossierPartData
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("data")]
        public List<DossierPart>? Data { get; set; }
    }
}