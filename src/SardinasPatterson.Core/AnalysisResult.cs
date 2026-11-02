namespace SardinasPatterson.Core;

public enum TerminationReason { EmptyResidual, EmptySet, RepeatedSet }

public sealed record ResidualSet(int Round, IReadOnlyList<string> Suffixes);
public sealed record Transition(int FromRound, string Source, string Codeword,
    string Operation, string Result);
public sealed record DecisionMetrics(int DistinctSuffixes, int DistinctSets,
    int GeneratedRounds, long PrefixComparisons, double ElapsedMilliseconds);
public sealed record AnalysisResult(IReadOnlyList<string> Codewords, bool IsPrefixFree,
    bool IsUniquelyDecodable, TerminationReason TerminationReason, int? RepeatedRound,
    IReadOnlyList<ResidualSet> ResidualSets, IReadOnlyList<Transition> Transitions,
    DecisionMetrics Metrics);
