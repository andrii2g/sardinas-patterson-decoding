# Sardinas–Patterson decoding

A .NET 10 CLI and library that decides whether 2–8 distinct, nonempty binary
codewords are uniquely decodable. Prefix checking, residual generation,
repeated-state detection, and bounded witness BFS are implemented directly.

```powershell
dotnet build -c Release
dotnet run --project src/SardinasPatterson.Cli -- analyze 0 01 10
dotnet run --project src/SardinasPatterson.Cli -- analyze --input examples/ambiguous.json --format json --svg report.svg
dotnet run --project src/SardinasPatterson.Cli -- analyze --input examples/cycle.json --svg cycle.svg
```

For `{0, 01}`, the code is uniquely decodable despite not being prefix-free.
For `{0, 01, 10}`, `010` has two decodings: `0 | 10` and `01 | 0`.

## Input and output

Supply codewords as arguments or a JSON object with a `codewords` array of
strings. Leading zeros are significant. Duplicate, empty, nonbinary, null,
and out-of-range codebooks are rejected. Input order does not affect the trace.
Use `--format json` for the analysis and witness result. Exit status is 0 for a
completed analysis (including ambiguity), and 2 for invalid input or file errors.
`--help` lists options.

Publish with `dotnet publish src/SardinasPatterson.Cli -c Release -o artifacts/cli`.
Invoke the published executable or DLL with the same arguments.

## Exact verdict and bounded witness

An empty residual proves ambiguity. An empty set or a repeated **whole set**
proves unique decodability. All comparisons use ordinal semantics.

Witness generation runs only after an ambiguous verdict. It enumerates codeword
sequences breadth-first and detects two sequences encoding the same bits.
Defaults: `--max-bits 64 --max-nodes 100000 --max-entries 50000`.
Limits must be positive integers. `LimitReached` never changes the exact verdict.
The first witness is not guaranteed to have minimum bit length. Memory also
depends on the bit limit and retained decoding lengths.

SVG reports contain successive residual sets, numbered suffix operations, a
highlighted path to the empty residual, and a back-reference for repeated sets.
The witness is shown separately: a residual path is not itself a decoding.
Report dimensions grow with states, operations, and codeword lengths.

## Metrics

Decision metrics count distinct nonempty suffixes, distinct whole sets,
generated rounds (including the terminal round), and prefix predicate calls.
Initial residual generation also performs prefix checking. Both directions
count when equality matches. Witness metrics count expanded nodes and retained
encoded strings. `witness.bitLength` reports the witness length, when found.
Decision and witness runtimes exclude parsing, SVG rendering, and serialization.
Aggregate ambiguous-codebook counts are printed by the verification suite.

## Verification

```powershell
dotnet run --project tests/SardinasPatterson.Tests -c Release
```

The dependency-free executable suite checks examples, all stopping conditions,
validation, witness limits, SVG structure, and input-order invariance. It tests
every two- and three-word codebook drawn from binary words of length 1–3 against
an independent dynamic-programming oracle enumerating messages through 10 bits,
stopping after a collision. A second oracle exhaustively concatenates codeword
sequences through six codewords. Returned witnesses are independently validated.
A bounded absence of collisions is not a proof of unique decodability.

CI is configured for Windows and Linux. Local checks do not imply CI has run.
See [design notes](docs/design.md) for implementation boundaries.
