using System.Text.Json.Serialization;

namespace MyBenchmark;

public record Schema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("properties")]
    public IEnumerable<SchemaProperty> Properties { get; set; } = null!;
}

public record SchemaProperty
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("type")]
    public PropertyType Type { get; set; }

    [JsonPropertyName("properties")]
    public IEnumerable<SchemaProperty> Properties { get; set; } = null!;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PropertyType
{
    String,
    Number,
    Date,
    Bool,
    Array
}
