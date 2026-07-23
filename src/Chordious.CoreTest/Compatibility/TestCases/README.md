# Chordious 2.8.0 compatibility corpus

This directory contains sanitized fixtures for compatibility tests.

- `Chordious.Config.2.8.0.xml` was produced through the public configuration API in the official `Chordious.Core.dll` shipped in `Chordious.Unpacked.zip` for release `v2.8.0`.
- `Classic.ChordLine.txt` exercises the legacy ChordLine parser with comments, option changes, an invalid line, open and muted strings, automatic and explicit barres, and a baseline label.
- `manifest.json` records the upstream release provenance and SHA-256 hashes needed to audit and verify the corpus.

The fixtures are synthetic and contain no user configuration or personal data. The release binary was used only to serialize public Chordious data types; it is not redistributed here.
