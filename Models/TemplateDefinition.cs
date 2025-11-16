using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NopHoSoTuDong.Models
{
    // Represents the entire template definition
    public class DossierTemplateDefinition
    {
        [JsonPropertyName("templateName")]
        public string? TemplateName { get; set; }

        [JsonPropertyName("fields")]
        public List<TemplateField>? Fields { get; set; }
    }

    // Represents a single field within the template
    public class TemplateField
    {
        [JsonPropertyName("fieldName")]
        public string? FieldName { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("componentType")]
        public string? ComponentType { get; set; }

        [JsonPropertyName("value")]
        public object? Value { get; set; }

        [JsonPropertyName("dataType")]
        public string? DataType { get; set; }

        [JsonPropertyName("options")]
        public List<OptionItem>? Options { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("validation")]
        public ValidationRules? Validation { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }


    }

    public class OptionItem
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    public class ValidationRules
    {
        [JsonPropertyName("notEmpty")]
        public bool? NotEmpty { get; set; }

        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("minLength")]
        public int? MinLength { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        [JsonPropertyName("trim")]
        public bool? Trim { get; set; }

        [JsonPropertyName("rows")]
        public int? Rows { get; set; }

        [JsonPropertyName("pattern")]
        public string? Pattern { get; set; }
    }
}
