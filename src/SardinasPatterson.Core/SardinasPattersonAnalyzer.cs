using System.Diagnostics;

namespace SardinasPatterson.Core;

public static class SardinasPattersonAnalyzer
{
    public static AnalysisResult Analyze(Codebook codebook)
    {
        var clock = Stopwatch.StartNew();
        var words = codebook.Words;
        var sets = new List<ResidualSet>();
        var transitions = new List<Transition>();
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var suffixes = new HashSet<string>(StringComparer.Ordinal);
        long comparisons = 0;
        var current = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var a in words)
        foreach (var b in words)
        {
            if (a == b) continue;
            comparisons++;
            if (!b.StartsWith(a, StringComparison.Ordinal)) continue;
            var tail = b[a.Length..];
            current.Add(tail);
            transitions.Add(new(0, b, a, "remove-codeword", tail));
        }
        var prefixFree = current.Count == 0;
        while (true)
        {
            var round = sets.Count + 1;
            sets.Add(new(round, current.ToArray()));
            suffixes.UnionWith(current.Where(s => s.Length > 0));
            // Binary suffixes cannot contain the delimiter; the key encodes a whole set.
            var key = string.Join(',', current);
            if (current.Contains("")) return Finish(false, TerminationReason.EmptyResidual);
            if (current.Count == 0) return Finish(true, TerminationReason.EmptySet);
            if (seen.TryGetValue(key, out var previous))
                return Finish(true, TerminationReason.RepeatedSet, previous);
            seen.Add(key, round);
            var next = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var residual in current)
            foreach (var word in words)
            {
                comparisons++;
                if (residual.StartsWith(word, StringComparison.Ordinal))
                    Add(residual[word.Length..], "remove-codeword");
                comparisons++;
                if (word.StartsWith(residual, StringComparison.Ordinal))
                    Add(word[residual.Length..], "remove-residual");

                void Add(string tail, string operation)
                {
                    next.Add(tail);
                    transitions.Add(new(round, residual, word, operation, tail));
                }
            }
            current = next;
        }

        AnalysisResult Finish(bool unique, TerminationReason reason, int? repeated = null)
        {
            clock.Stop();
            var distinctSets = sets.Select(s => string.Join(',', s.Suffixes)).Distinct().Count();
            return new(words, prefixFree, unique, reason, repeated, sets, transitions,
                new(suffixes.Count, distinctSets, sets.Count, comparisons, clock.Elapsed.TotalMilliseconds));
        }
    }
}
