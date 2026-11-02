using System.Xml.Linq;
using SardinasPatterson.Core;

namespace SardinasPatterson.Svg;

public static class SvgReport
{
    private static readonly XNamespace Ns = "http://www.w3.org/2000/svg";

    public static string Render(AnalysisReport report)
    {
        var result = report.Analysis;
        var columns = new List<IReadOnlyList<string>> { result.Codewords };
        columns.AddRange(result.ResidualSets.Select(s => s.Suffixes));
        var nodeWidth = Math.Max(120, result.Codewords.Max(w => w.Length) * 9 + 32);
        var spacing = nodeWidth + 150;
        var width = Math.Max(900, columns.Count * spacing + 80);
        var graphBottom = 220 + columns.Max(c => c.Count) * 80;
        var height = graphBottom + 100 + result.Transitions.Count * 28 + 100;
        var root = new XElement(Ns + "svg", new XAttribute("viewBox", $"0 0 {width} {height}"),
            new XAttribute("role", "img"), new XAttribute("aria-labelledby", "title description"),
            new XElement(Ns + "title", new XAttribute("id", "title"), "Sardinas–Patterson residual trace"),
            new XElement(Ns + "desc", new XAttribute("id", "description"),
                "Columns show codewords and successive residual sets. Red edges trace an ambiguity to the empty suffix. Numbered operations appear below."),
            new XElement(Ns + "style", "text{font-family:monospace;font-size:14px;fill:#172b4d} .heading{font-size:20px;font-weight:bold}"),
            new XElement(Ns + "rect", new XAttribute("width", "100%"), new XAttribute("height", "100%"), new XAttribute("fill", "#f6f8fc")));
        Text(30, 35, "Sardinas–Patterson: " + (result.IsUniquelyDecodable ? "uniquely decodable" : "ambiguous"), "heading");
        Text(30, 65, $"Prefix-free: {result.IsPrefixFree}; termination: {result.TerminationReason}");
        Text(30, 95, report.Witness.Status == WitnessStatus.Found
            ? $"Witness: {report.Witness.Bits} = {string.Join(" | ", report.Witness.FirstDecoding!)} = {string.Join(" | ", report.Witness.SecondDecoding!)}"
            : $"Witness: {report.Witness.Status}");

        var path = new HashSet<Transition>();
        if (!result.IsUniquelyDecodable)
        {
            var target = "";
            for (var round = result.ResidualSets.Count - 1; round >= 0; round--)
            {
                var edge = result.Transitions.First(t => t.FromRound == round && t.Result == target);
                path.Add(edge);
                target = edge.Source;
            }
        }
        // Paint highlighted edges last so coincident operations cannot obscure them.
        foreach (var i in Enumerable.Range(0, result.Transitions.Count)
            .OrderBy(i => path.Contains(result.Transitions[i])))
        {
            var edge = result.Transitions[i];
            var source = Position(edge.FromRound, edge.Source);
            var target = Position(edge.FromRound + 1, edge.Result);
            var color = path.Contains(edge) ? "#bd2031" : "#60758d";
            root.Add(new XElement(Ns + "path", new XAttribute("d",
                $"M {source.X + nodeWidth} {source.Y} L {target.X} {target.Y} l -9 -4 m 9 4 l -9 4"),
                new XAttribute("stroke", color), new XAttribute("fill", "none"),
                new XAttribute("stroke-width", path.Contains(edge) ? 3 : 1)));
            Text(source.X + nodeWidth + 12, source.Y - 9 + (i % 3) * 16, (i + 1).ToString());
            Text(30, graphBottom + 65 + i * 28,
                $"{i + 1}. {(edge.FromRound == 0 ? "C" : $"S{edge.FromRound}")}:{Show(edge.Source)} — {edge.Operation} {edge.Codeword} → S{edge.FromRound + 1}:{Show(edge.Result)}");
        }
        for (var column = 0; column < columns.Count; column++)
        {
            Text(40 + column * spacing, 140, column == 0 ? "Codebook C" : $"S{column}", "heading");
            if (columns[column].Count == 0) Text(40 + column * spacing, 185, "∅");
            foreach (var suffix in columns[column])
            {
                var p = Position(column, suffix);
                root.Add(new XElement(Ns + "rect", new XAttribute("x", p.X), new XAttribute("y", p.Y - 23),
                    new XAttribute("width", nodeWidth), new XAttribute("height", 46), new XAttribute("rx", 9),
                    new XAttribute("fill", suffix.Length == 0 ? "#ffe0e4" : "#e4edff"), new XAttribute("stroke", "#60758d")));
                Text(p.X + 12, p.Y + 5, Show(suffix));
            }
        }
        if (result.RepeatedRound is int repeated)
        {
            var from = 40 + (columns.Count - 1) * spacing + nodeWidth / 2;
            var to = 40 + repeated * spacing + nodeWidth / 2;
            root.Add(new XElement(Ns + "path", new XAttribute("d",
                $"M {from} 208 V {graphBottom - 40} Q {(from + to) / 2} {graphBottom + 10} {to} {graphBottom - 40} V 208 l -4 9 m 4 -9 l 4 9"),
                new XAttribute("stroke", "#305db0"), new XAttribute("stroke-dasharray", "5 4"), new XAttribute("fill", "none")));
            Text(to, graphBottom + 20, $"Repeated set: S{columns.Count - 1} = S{repeated}");
        }
        Text(30, graphBottom + 45, "Transition operations (red graph edges identify one path to ε)");
        return new XDocument(root).ToString();

        (int X, int Y) Position(int column, string suffix) =>
            (40 + column * spacing, 185 + columns[column].ToList().IndexOf(suffix) * 80);
        void Text(int x, int y, string value, string? css = null)
        {
            var element = new XElement(Ns + "text", new XAttribute("x", x), new XAttribute("y", y), value);
            if (css is not null) element.Add(new XAttribute("class", css));
            root.Add(element);
        }
    }

    private static string Show(string suffix) => suffix.Length == 0 ? "ε" : suffix;
}
