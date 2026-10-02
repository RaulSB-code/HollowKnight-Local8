# Publishing Local8

Checklist for the first public release of Hollow Knight Local8.

## Target release

- Public version: **v0.9.0**
- Internal release candidate: **build 139**
- Target Hollow Knight version: **1.5.78.11833**
- Release package: **HollowKnightLocal8-v0.9.0.zip**

## Before making the repository public

- [ ] Add the complete Local8 source code that matches the v0.9.0 release candidate.
- [ ] Make sure no original Hollow Knight files or Team Cherry assemblies are committed.
- [ ] Remove temporary development files, private paths and unnecessary debug artifacts.
- [ ] Confirm the project builds from a clean checkout.
- [ ] Test the final DLL on the target Hollow Knight version.
- [ ] Test manual installation from the final ZIP.
- [x] Prepare README and feature documentation.
- [x] Prepare compatibility documentation.
- [x] Prepare bug report template.
- [ ] Decide whether to add a source-code license before publication.

## GitHub Release v0.9.0

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
   `Hollow Knight Local8 v0.9.0`
6. Attach the exact ZIP.
7. Calculate the SHA-256 of that exact uploaded ZIP.
8. Verify the release download before submitting to ModLinks.

## ModLinks / Lumafly

Current ModLinks requirements include having the mod source code available in a Git repository.

The Local8 entry should use:

- Name: `HollowKnightLocal8`
- Display name: `Hollow Knight Local8`
- Version: `0.9.0.0`
- GitHub Release download URL for the final ZIP.
- SHA-256 of that exact ZIP.
- Repository link to this project.
- README link.
- Issues link.
- Gameplay tag.
- Custom Knight as an integration.
- Enemy HP Bar can be documented as recommended; it does not need to be a required dependency.

After the ModLinks pull request is accepted, verify that Local8 appears in Lumafly and performs a clean install.
