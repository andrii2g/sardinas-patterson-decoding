using SardinasPatterson.Core;

var checks = 0;
Check(new[] { "0", "1" }, true, true, TerminationReason.EmptySet);
Check(new[] { "0", "01" }, true, false, TerminationReason.EmptySet);
Check(new[] { "0", "01", "10" }, false, false, TerminationReason.EmptyResidual);
Check(new[] { "0", "01", "11" }, true, false, TerminationReason.RepeatedSet);
foreach (var invalid in new[] { new[] { "0" }, new[] { "", "1" }, new[] { "0", "0" }, new[] { "0", "2" } })
{
    try { _ = new Codebook(invalid); throw new Exception("Accepted invalid input."); }
    catch (ArgumentException) { checks++; }
}
var ambiguous = new Codebook(["0", "01", "10"]);
var witness = WitnessSearcher.Search(ambiguous);
Assert(witness.Status == WitnessStatus.Found && witness.Bits == "010", "Expected 010 witness.");
Assert(string.Concat(witness.FirstDecoding!) == witness.Bits &&
    string.Concat(witness.SecondDecoding!) == witness.Bits &&
    !witness.FirstDecoding!.SequenceEqual(witness.SecondDecoding!), "Witness must have two decodings.");
foreach (var (options, limit) in new[] {
    (new WitnessOptions(MaxBits: 2), "bits"),
    (new WitnessOptions(MaxNodes: 1), "nodes"),
    (new WitnessOptions(MaxEntries: 1), "entries") })
{
    var limited = WitnessSearcher.Search(ambiguous, options);
    Assert(limited.Status == WitnessStatus.LimitReached && limited.Limit == limit, "Expected explicit limit.");
    Assert(!SardinasPattersonAnalyzer.Analyze(ambiguous).IsUniquelyDecodable, "Limits must not change verdict.");
}
Console.WriteLine($"Passed {checks} checks.");

void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}

void Check(string[] words, bool unique, bool prefix, TerminationReason reason)
{
    var result = SardinasPattersonAnalyzer.Analyze(new(words));
    if (result.IsUniquelyDecodable != unique || result.IsPrefixFree != prefix || result.TerminationReason != reason)
        throw new Exception($"Unexpected result for {string.Join(',', words)}");
    checks++;
}
