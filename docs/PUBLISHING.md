# Publishing Local8

This document is a preparation checklist for the eventual public release of Hollow Knight Local8.

## Before publishing

- [ ] Remove private/debug-only code and temporary files.
- [ ] Make sure no original Hollow Knight game files or Team Cherry assemblies are committed.
- [ ] Add the complete Local8 source code.
- [ ] Confirm the project builds from a clean checkout.
- [ ] Test the intended supported Hollow Knight version.
- [ ] Test installation without development-only files.
- [ ] Update README.md.
- [ ] Update CHANGELOG.md.
- [ ] Choose a license before making the source repository public, if desired.

## GitHub release

For the first stable public release:

1. Choose a numeric version such as `1.0.0`.
2. Build the release version.
3. Create a clean ZIP containing only the files users need.
4. Suggested filename:
   `HollowKnightLocal8-v1.0.0.zip`
5. Create a GitHub Release tagged:
   `v1.0.0`
6. Attach the ZIP to that release.
7. Calculate the SHA-256 of the exact ZIP that users will download.

Do not calculate the ModLinks hash from a different DLL or ZIP.

## ModLinks / Lumafly

When the release is ready:

- [ ] Make the repository accessible as required by the Hollow Knight modding community.
- [ ] Check the current ModLinks contribution requirements.
- [ ] Add the Local8 entry using the current ModLinks manifest format.
- [ ] Use the GitHub Release download URL.
- [ ] Use the SHA-256 of that exact release ZIP.
- [ ] Point the manifest to this repository and README.
- [ ] Submit the ModLinks pull request.
- [ ] After it is accepted, verify that Local8 appears and installs correctly through Lumafly.

The ModLinks format and requirements can change, so they should be checked again immediately before submission.

## Recommended release contents

A public release ZIP should be kept minimal. For example:

```text
HollowKnightLocal8-v1.0.0.zip
└── HollowKnightLocal8.dll
```

If Local8 later needs additional runtime files, include only those required by the mod.
