using System.Text.Json.Serialization;

namespace NopHoSoTuDong.Models
{
    public class ApiTemplate
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = "";

        [JsonPropertyName("displayNamePattern")]
        public string DisplayNamePattern { get; set; } = "";

        [JsonPropertyName("abstractSSPattern")]
        public string AbstractSSPattern { get; set; } = "";

        [JsonPropertyName("serviceCode")]
        public string ServiceCode { get; set; } = "";

        [JsonPropertyName("ownerType")]
        public string OwnerType { get; set; } = "1";

        [JsonPropertyName("partType")]
        public string PartType { get; set; } = "1";

        [JsonPropertyName("partNo")]
        public string PartNo { get; set; } = "";

        [JsonPropertyName("paperNotation")]
        public string PaperNotation { get; set; } = "";

        [JsonPropertyName("isActive")]
        public string IsActive { get; set; } = "1";

        [JsonPropertyName("departmentIssue")]
        public string DepartmentIssue { get; set; } = "";

        [JsonPropertyName("codeNumber")]
        public string CodeNumber { get; set; } = "";

        [JsonPropertyName("ownerNo")]
        public string OwnerNo { get; set; } = "";

        [JsonPropertyName("ownerDate")]
        public string OwnerDate { get; set; } = "";
    }
}
