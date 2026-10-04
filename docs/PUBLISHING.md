# Publishing Hollow Knight 8-Player Co-op

Checklist for the first public release of Hollow Knight 8-Player Co-op.

## Target release

- Public version: **v0.9.0**
- Internal release candidate: **build 139**
- Target Hollow Knight version: **1.5.78.11833**
- Release package: **HollowKnightLocal8-v0.9.0.zip**

## Before making the repository public

- [x] Add the v0.9.0 recovered source tree.
- [x] Make sure no original Hollow Knight files or Team Cherry assemblies are committed.
- [x] Remove temporary development files, private paths and unnecessary debug artifacts.
- [ ] Confirm the project builds from a clean checkout.
- [ ] Test the final DLL on the target Hollow Knight version.
- [ ] Test manual installation from the final ZIP.
- [x] Prepare README and feature documentation.
- [x] Prepare compatibility documentation.
- [x] Prepare bug report template.
- [ ] Decide whether to add a source-code license before publication.

## GitHub Release v0.9.0

Published on 2026-10-02.

- Release: https://github.com/RaulSB-code/HollowKnight-Local8/releases/tag/v0.9.0
- Asset: `HollowKnightLocal8-v0.9.0.zip`
- SHA-256: `4eaf5176d980c1bf9e7d090e66d0657ead5c3d304b115fc21e3779bfef0cdd49`

Release package verified against GitHub's asset digest.

For future releases:

1. Build the exact DLL intended for public distribution.
2. Rename the public package to:
   `HollowKnightLocal8-v0.9.0.zip`
3. Keep the ZIP minimal:

```text
HollowKnightLocal8-v0.9.0.zip
└── HollowKnightLocal8.dll
```

4. Create the GitHub tag:
   `v0.9.0`
5. Create the GitHub Release:
   `Hollow Knight 8-Player Co-op v0.9.0`
6. Attach the exact ZIP.
7. Calculate the SHA-256 of that exact uploaded ZIP.
8. Verify the release download before submitting to ModLinks.

## ModLinks / Lumafly

Current ModLinks requirements include having the mod source code available in a Git repository.

The ModLinks entry should use:

- Name: `Hollow Knight 8-Player Co-op`
- Display name: `Hollow Knight 8-Player Co-op`
- Version: `0.9.0.0`
- GitHub Release download URL for the final ZIP.
- SHA-256 of that exact ZIP.
- Repository link to this project.
- README link.
- Issues link.
- Gameplay tag.
- Custom Knight as an integration.
- Enemy HP Bar can be documented as recommended; it does not need to be a required dependency.

After the ModLinks pull request is accepted, verify that Hollow Knight 8-Player Co-op appears in Lumafly and performs a clean install.
