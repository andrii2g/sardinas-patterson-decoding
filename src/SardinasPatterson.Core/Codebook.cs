namespace SardinasPatterson.Core;

public sealed class Codebook
{
    public IReadOnlyList<string> Words { get; }

    public Codebook(IEnumerable<string> words)
    {
        ArgumentNullException.ThrowIfNull(words);
        var values = words.ToArray();
        if (values.Length is < 2 or > 8)
            throw new ArgumentException("Supply 2–8 codewords.");
        if (values.Any(w => string.IsNullOrEmpty(w) || w.Any(c => c is not ('0' or '1'))))
            throw new ArgumentException("Codewords must be nonempty binary strings.");
        if (values.Distinct(StringComparer.Ordinal).Count() != values.Length)
            throw new ArgumentException("Codewords must be distinct.");
        Words = Array.AsReadOnly(values.Order(StringComparer.Ordinal).ToArray());
    }
}
