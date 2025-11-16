using System.Text.Json.Serialization;

namespace NopHoSoTuDong.Models
{
    public class DossierTemplate
    {
        [JsonPropertyName("fileName")] public string? FileName { get; set; }
        [JsonPropertyName("displayNamePattern")] public string? DisplayNamePattern { get; set; }
        [JsonPropertyName("abstractSSPattern")] public string? AbstractSSPattern { get; set; }
        [JsonPropertyName("serviceCode")] public string? ServiceCode { get; set; }
        [JsonPropertyName("ownerType")] public string? OwnerType { get; set; }
        [JsonPropertyName("partType")] public string? PartType { get; set; }
        [JsonPropertyName("partNo")] public string? PartNo { get; set; }
        [JsonPropertyName("codeNotation")] public string? CodeNotation { get; set; }
        [JsonPropertyName("isActive")] public string? IsActive { get; set; }
        [JsonPropertyName("departmentIssue")] public string? DepartmentIssue { get; set; }
        [JsonPropertyName("codeNumber")] public string? CodeNumber { get; set; }
        [JsonPropertyName("ownerNo")] public string? OwnerNo { get; set; }
        [JsonPropertyName("ownerDate")] public string? OwnerDate { get; set; }
    }
}

