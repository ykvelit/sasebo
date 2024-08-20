using System.Text.Json.Serialization;

namespace MyBenchmark;

public record Funcionario
{
    [JsonPropertyName("id")]
    public double Id { get; set; }

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = null!;

    [JsonPropertyName("sobrenome")]
    public string Sobrenome { get; set; } = null!;

    [JsonPropertyName("cargo")]
    public string Cargo { get; set; } = null!;

    [JsonPropertyName("dataDeNascimento")]
    public DateTime DataDeNascimento { get; set; }

    [JsonPropertyName("dependentes")]
    public IEnumerable<Dependente> Dependentes { get; set; } = null!;
}

public record Dependente
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = null!;
}