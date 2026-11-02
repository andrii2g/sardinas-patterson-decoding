using System.Text.Json;
using System.Text.Json.Serialization;
using SardinasPatterson.Core;

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = { new JsonStringEnumConverter() }
};
try
{
    if (args.Length == 0 || args.Contains("--help"))
    {
        Console.WriteLine("Usage: spd analyze <words...> | --input <file> [--format text|json]");
        return 0;
    }
    var words = new List<string>();
    string? input = null;
    var format = "text";
    var start = args[0] == "analyze" ? 1 : 0;
    for (var i = start; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--input": input = Value(ref i); break;
            case "--format": format = Value(ref i); break;
            default:
                if (args[i].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"Unknown option: {args[i]}");
                words.Add(args[i]); break;
        }
    }
    if (format is not ("text" or "json")) throw new ArgumentException("Format must be text or json.");
    if (input is not null)
    {
        if (words.Count > 0) throw new ArgumentException("Use either CLI codewords or --input.");
        using var document = JsonDocument.Parse(File.ReadAllText(input));
        words = document.RootElement.GetProperty("codewords").EnumerateArray()
            .Select(e => e.GetString() ?? throw new ArgumentException("Null codeword.")).ToList();
    }
    var result = SardinasPattersonAnalyzer.Analyze(new(words));
    if (format == "json") Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
    else
    {
        Console.WriteLine($"{(result.IsUniquelyDecodable ? "Uniquely decodable" : "Ambiguous")}; prefix-free: {result.IsPrefixFree}");
        foreach (var set in result.ResidualSets)
            Console.WriteLine($"S{set.Round} = {{{string.Join(", ", set.Suffixes.Select(s => s.Length == 0 ? "ε" : s))}}}");
        Console.WriteLine($"Termination: {result.TerminationReason}; comparisons: {result.Metrics.PrefixComparisons}");
    }
    return 0;

    string Value(ref int i) => ++i < args.Length ? args[i] : throw new ArgumentException("Missing option value.");
}
catch (Exception error) when (error is ArgumentException or IOException or JsonException
    or KeyNotFoundException or InvalidOperationException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Error: {error.Message}");
    return 2;
}
