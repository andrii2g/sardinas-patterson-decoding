# Design and correctness

The core library has no filesystem, rendering, or console dependencies. Codebook
construction validates and sorts input; analysis returns an ordered trace and
operation provenance. The CLI handles input and output. The SVG library renders
the trace without recalculating the verdict.

## Finite residual process

The first set contains nonempty tails left by removing a codeword prefix from
another codeword. Subsequent rounds remove each codeword from each residual,
and each residual from each codeword, wherever a prefix match exists.

Every nonempty residual is a proper suffix of an original codeword. There are
at most the sum of `(word.Length - 1)` distinct nonempty residuals. Whole sets
range over a finite universe and each next set is deterministic, so repetition
without an empty residual proves no future round can introduce one. Repeating
individual suffixes alone does not establish a whole-set cycle.

Keys use sorted, comma-delimited binary strings. The delimiter cannot occur in
codewords. Empty sets and sets containing the empty string are handled before
key lookup. There is no iteration cap that could turn an incomplete decision
into a unique verdict. This educational whole-set implementation does not
claim a polynomial bound on rounds.

## Witness search

Breadth-first nodes contain bits and a codeword sequence. Each new bitstring is
retained with its first sequence. A second sequence proves ambiguity and is
validated before return. Limits bound bit length, expanded nodes, and retained
entries. The first resource preventing further search is reported separately
from the exact verdict. A residual path is not presented as a decoding.

## Scope and verification

The MVP accepts one codebook per invocation. Batch benchmarking, decoding
arbitrary messages, a web UI, shortest-bit witnesses, and Unicode alphabets
are future work. No word-length cap is imposed; large inputs require
corresponding memory and report space.

The executable test suite requires no external packages. Run it with
`dotnet run --project tests/SardinasPatterson.Tests`; `dotnet test` is not this
repository's verification command.

## References

- [Princeton: Data Compression](https://algs4.cs.princeton.edu/55compression/index.php)
- [University of Scranton: Sardinas–Patterson graph](https://www.scranton.edu/faculty/mccloskey/courses/cmps144/data_compression.pdf)
