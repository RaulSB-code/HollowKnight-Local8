# Publishing Hollow Knight 8-Player Co-op v0.10.0

These files are prepared for publication; preparation does not publish a release or update a PR.

## Repository

Use the complete **HollowKnightLocal8-source-v0.10.0.zip** to replace the old source/project/docs tree. Remove obsolete source files before copying the new `src/Core/` and `src/Features/` structure, so duplicate class definitions are not retained. Keep existing Git history, repository settings and any license. Do not add game libraries or compiler output.

The source package is the exact tree used for the release DLL. Follow [building](BUILDING.md) for reproducible compilation.

## GitHub Release

- Tag: **v0.10.0**
- Title: **Hollow Knight 8-Player Co-op v0.10.0**
- Installation asset: **HollowKnightLocal8-v0.10.0.zip**
- Release body: [RELEASE_NOTES_v0.10.0.md](RELEASE_NOTES_v0.10.0.md)

Upload the supplied ZIP unchanged. It contains only `HollowKnightLocal8.dll`. The SHA-256 in `release/SHA256SUMS.txt` and `release/ModLinks-v0.10.0.xml` is for that exact ZIP. Repacking it changes its checksum. Do not upload the source ZIP as the Lumafly installation asset; source belongs in the repository.

## ModLinks / Lumafly

Use the prepared manifest in `release/ModLinks-v0.10.0.xml` to update the existing PR after the release asset and source are available.

Name/DisplayName: **Hollow Knight 8-Player Co-op**. Version: **0.10.0.0**. Preserve repository, README and issue URLs, optional Custom Knight integration, Gameplay and LLM-Assisted tags, and author RaulSB-code. No mandatory mod dependency has been added.

Before submitting, confirm the uploaded asset's digest still matches the prepared checksum and that manual installation works on the target game/API.
