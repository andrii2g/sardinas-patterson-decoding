using System.Diagnostics;

namespace SardinasPatterson.Core;

public enum WitnessStatus { Found, LimitReached, NotApplicable }
public sealed record WitnessOptions(int MaxBits = 64, int MaxNodes = 100_000, int MaxEntries = 50_000);
public sealed record WitnessResult(WitnessStatus Status, string? Bits,
    IReadOnlyList<string>? FirstDecoding, IReadOnlyList<string>? SecondDecoding,
    long ExpandedNodes, int RetainedEntries, double ElapsedMilliseconds, string? Limit);
public sealed record AnalysisReport(AnalysisResult Analysis, WitnessResult Witness);

public static class WitnessSearcher
{
    public static WitnessResult Search(Codebook codebook, WitnessOptions? options = null)
    {
        options ??= new();
        if (options.MaxBits < 1 || options.MaxNodes < 1 || options.MaxEntries < 1)
            throw new ArgumentException("Witness limits must be positive integers.");
        var clock = Stopwatch.StartNew();
        var queue = new Queue<(string Bits, string[] Words)>();
        queue.Enqueue(("", []));
        var encodings = new Dictionary<string, string[]>(StringComparer.Ordinal);
        long expanded = 0;
        while (queue.TryDequeue(out var node))
        {
            if (expanded >= options.MaxNodes) return Finish(WitnessStatus.LimitReached, "nodes");
            expanded++;
            foreach (var word in codebook.Words)
            {
                if ((long)node.Bits.Length + word.Length > options.MaxBits) continue;
                var bits = node.Bits + word;
                string[] decoding = [.. node.Words, word];
                if (encodings.TryGetValue(bits, out var first))
                {
                    if (first.SequenceEqual(decoding)) continue;
                    if (string.Concat(first) != bits || string.Concat(decoding) != bits)
                        throw new InvalidOperationException("Invalid reconstructed witness.");
                    return Finish(WitnessStatus.Found, null, bits, first, decoding);
                }
                if (encodings.Count >= options.MaxEntries) return Finish(WitnessStatus.LimitReached, "entries");
                encodings.Add(bits, decoding);
                queue.Enqueue((bits, decoding));
            }
        }
        return Finish(WitnessStatus.LimitReached, "bits");

        WitnessResult Finish(WitnessStatus status, string? limit, string? bits = null,
            string[]? first = null, string[]? second = null)
        {
            clock.Stop();
            return new(status, bits, first, second, expanded, encodings.Count,
                clock.Elapsed.TotalMilliseconds, limit);
        }
    }

    public static WitnessResult NotApplicable() => new(WitnessStatus.NotApplicable,
        null, null, null, 0, 0, 0, null);
}
