# Building Local8

This repository does not include Hollow Knight or Team Cherry assemblies.

1. Install Hollow Knight 1.5.78.11833 and the matching Modding API.
2. Copy `LocalBuildProperties_example.props` to `LocalBuildProperties.props`.
3. Set `References` to the game's `hollow_knight_Data/Managed` folder containing the Modding API-patched `Assembly-CSharp.dll` and the required Unity/PlayMaker hook assemblies.
4. Build `HollowKnightLocal8.csproj` in Release configuration.

`LocalBuildProperties.props`, build output, game assemblies and Modding API binaries must not be committed.

## v0.9.0 source provenance

The v0.9.0 source tree was recovered from the exact internal build 139 DLL used for the first public release because a complete hand-written source snapshot for that final build was not preserved. Earlier development source existed, but some later changes were applied directly at IL/binary level.

The recovered source is published for transparency and continued cleanup. The public v0.9.0 release binary remains the authoritative build for that release.
