# MVP verification — 2026-11-02

Local environment: Windows, .NET SDK 10.0.403.

- Release build: passed, no warnings or errors.
- Executable verification suite: 1,640 checks passed across all 455 two- and
  three-word codebooks drawn from the 14 binary words of length 1–3.
- 124 ambiguous codebooks detected. Each had an independent decoding-count
  oracle collision within 10 bits and a validated BFS witness within 24 bits.
- Independent exhaustive concatenation oracle: sequences through six codewords.
- CLI JSON input/output, witness bit-length metric, explicit witness limit,
  duplicate input, invalid numeric option, and error exit status: passed.
- Release publish: passed.
- Ambiguous and repeated-set SVG examples rendered in headless Edge and visually
  inspected; SVG structure is also checked in the executable suite.

The bounded oracles do not establish the theorem for arbitrary codebooks.
Linux/Windows GitHub Actions checks are configured but were not run locally.
