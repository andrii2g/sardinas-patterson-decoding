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
        Console.WriteLine("Usage: spd analyze <words...> | --input <file> [--format text|json]\nWitness limits: --max-bits 64 --max-nodes 100000 --max-entries 50000");
        return 0;
    }
    var words = new List<string>();
    string? input = null;
    var format = "text";
    var maxBits = 64;
    var maxNodes = 100_000;
    var maxEntries = 50_000;
    var start = args[0] == "analyze" ? 1 : 0;
    for (var i = start; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--input": input = Value(ref i); break;
            case "--format": format = Value(ref i); break;
            case "--max-bits": maxBits = Positive(Value(ref i)); break;
            case "--max-nodes": maxNodes = Positive(Value(ref i)); break;
            case "--max-entries": maxEntries = Positive(Value(ref i)); break;
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
    var codebook = new Codebook(words);
    var result = SardinasPattersonAnalyzer.Analyze(codebook);
    var witness = result.IsUniquelyDecodable ? WitnessSearcher.NotApplicable()
        : WitnessSearcher.Search(codebook, new(maxBits, maxNodes, maxEntries));
    if (format == "json") Console.WriteLine(JsonSerializer.Serialize(new AnalysisReport(result, witness), jsonOptions));
    else
    {
        Console.WriteLine($"{(result.IsUniquelyDecodable ? "Uniquely decodable" : "Ambiguous")}; prefix-free: {result.IsPrefixFree}");
        foreach (var set in result.ResidualSets)
            Console.WriteLine($"S{set.Round} = {{{string.Join(", ", set.Suffixes.Select(s => s.Length == 0 ? "ε" : s))}}}");
        Console.WriteLine($"Termination: {result.TerminationReason}; comparisons: {result.Metrics.PrefixComparisons}");
        Console.WriteLine($"Witness: {witness.Status}{(witness.Limit is null ? "" : $" ({witness.Limit} limit)")}");
        if (witness.Status == WitnessStatus.Found)
        {
            Console.WriteLine($"Encoded: {witness.Bits}");
            Console.WriteLine(string.Join(" | ", witness.FirstDecoding!));
            Console.WriteLine(string.Join(" | ", witness.SecondDecoding!));
        }
    }
    return 0;

    string Value(ref int i) => ++i < args.Length ? args[i] : throw new ArgumentException("Missing option value.");
    static int Positive(string value) => int.TryParse(value, out var number) && number > 0
        ? number : throw new ArgumentException("Witness limits must be positive integers.");
}
catch (Exception error) when (error is ArgumentException or IOException or JsonException
    or KeyNotFoundException or InvalidOperationException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Error: {error.Message}");
    return 2;
}
