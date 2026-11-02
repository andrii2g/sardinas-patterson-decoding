using SardinasPatterson.Core;
using SardinasPatterson.Svg;
using System.Xml.Linq;

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
Assert(new Codebook(Enumerable.Range(0, 8).Select(i => Convert.ToString(i, 2).PadLeft(3, '0'))).Words.Count == 8,
    "Eight codewords must be accepted.");
foreach (var invalid in new[] { new string[] { null!, "1" }, Enumerable.Repeat("0", 9).ToArray() })
{
    try { _ = new Codebook(invalid); throw new Exception("Accepted invalid codebook."); }
    catch (ArgumentException) { checks++; }
}
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
var universe = Enumerable.Range(1, 3).SelectMany(length => Enumerable.Range(0, 1 << length)
    .Select(n => Convert.ToString(n, 2).PadLeft(length, '0'))).ToArray();
var codebooks = 0;
var detected = 0;
var boundedUndetected = 0;
for (var a = 0; a < universe.Length; a++)
for (var b = a + 1; b < universe.Length; b++)
{
    Verify([universe[a], universe[b]]);
    for (var c = b + 1; c < universe.Length; c++) Verify([universe[a], universe[b], universe[c]]);
}
foreach (var words in new[] { new[] { "0", "1" }, new[] { "0", "01" },
    new[] { "0", "01", "10" }, new[] { "0", "01", "11" } })
{
    var analysis = SardinasPattersonAnalyzer.Analyze(new(words));
    var xml = XDocument.Parse(SvgReport.Render(new(analysis, analysis.IsUniquelyDecodable
        ? WitnessSearcher.NotApplicable() : WitnessSearcher.Search(new(words)))));
    XNamespace ns = "http://www.w3.org/2000/svg";
    Assert(xml.Root!.Name == ns + "svg" && xml.Descendants(ns + "title").Any(), "Accessible SVG expected.");
    Assert(xml.Descendants(ns + "text").Any(t => t.Value.Contains(analysis.TerminationReason.ToString())), "SVG verdict expected.");
}
Console.WriteLine($"Passed {checks} checks across {codebooks} codebooks; {detected} ambiguous; {boundedUndetected} ambiguous without oracle witness within 10 bits.");

void Verify(string[] words)
{
    codebooks++;
    var analysis = SardinasPattersonAnalyzer.Analyze(new(words));
    // Independent oracle: count segmentations of every binary message through 10 bits.
    var collision = false;
    for (var length = 1; length <= 10 && !collision; length++)
    for (var value = 0; value < 1 << length && !collision; value++)
    {
        var message = Convert.ToString(value, 2).PadLeft(length, '0');
        var counts = new int[length + 1];
        counts[0] = 1;
        for (var end = 1; end <= length; end++)
        foreach (var word in words)
            if (end >= word.Length && message.AsSpan(end - word.Length, word.Length).SequenceEqual(word))
                counts[end] = Math.Min(2, counts[end] + counts[end - word.Length]);
        collision = counts[length] == 2;
    }
    Assert(!collision || !analysis.IsUniquelyDecodable, "Oracle collision contradicts exact verdict.");
    // A second oracle exhaustively concatenates codeword sequences through depth six.
    var concatenations = new HashSet<string>(StringComparer.Ordinal);
    var frontier = new List<string> { "" };
    var concatenationCollision = false;
    for (var depth = 1; depth <= 6; depth++)
    {
        var next = new List<string>();
        foreach (var prefix in frontier)
        foreach (var word in words)
        {
            var bits = prefix + word;
            if (!concatenations.Add(bits)) concatenationCollision = true;
            next.Add(bits);
        }
        frontier = next;
    }
    Assert(!concatenationCollision || !analysis.IsUniquelyDecodable, "Concatenation collision contradicts verdict.");
    var reversed = SardinasPattersonAnalyzer.Analyze(new(words.Reverse()));
    Assert(analysis.ResidualSets.Select(s => string.Join(',', s.Suffixes)).SequenceEqual(
        reversed.ResidualSets.Select(s => string.Join(',', s.Suffixes))), "Input order must not affect trace.");
    if (!analysis.IsUniquelyDecodable)
    {
        detected++;
        if (!collision) boundedUndetected++;
        var found = WitnessSearcher.Search(new(words), new(MaxBits: 24));
        Assert(found.Status == WitnessStatus.Found, "Expected small-codebook witness within bounds.");
        Assert(string.Concat(found.FirstDecoding!) == found.Bits && string.Concat(found.SecondDecoding!) == found.Bits
            && !found.FirstDecoding!.SequenceEqual(found.SecondDecoding!), "Invalid exhaustive-case witness.");
    }
}

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
