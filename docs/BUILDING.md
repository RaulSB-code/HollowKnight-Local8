# Building Hollow Knight 8-Player Co-op

This is the complete source for public **v0.10.0**, targeting **Hollow Knight 1.5.78.11833** and its matching Modding API. No previous mod DLL is needed. Game/Team Cherry assemblies and build outputs are excluded.

## Canonical release build

Requirements: Python 3, **.NET SDK 8.0.408**, and the matching Modding API-patched game's `hollow_knight_Data/Managed` directory. That directory must contain `Assembly-CSharp.dll`, `MMHOOK_Assembly-CSharp.dll`, `PlayMaker.dll`, `MMHOOK_PlayMaker.dll`, `mscorlib.dll` and the matching Unity/framework references.

From this source root:

```bash
python build.py --managed "YOUR_HOLLOW_KNIGHT_MANAGED_DIRECTORY"
```

Use `--dotnet` to select an SDK executable, or `HK_MANAGED_DIR` to supply the reference directory. `--output` selects the output file; the default is `artifacts/HollowKnightLocal8.dll`. References remain outside the source tree. The build script uses Roslyn directly with optimization, deterministic output and path mapping, without PDBs.

The public DLL was compiled with this command path and the exact source/assets in this package. Recompilation after extracting the source ZIP was checked for byte-for-byte equality using the same SDK and reference set. Different compiler versions or game/API assemblies can produce a different binary.

## IDE/MSBuild project

`HollowKnightLocal8.csproj` is provided for editing and conventional Release builds. Copy `LocalBuildProperties_example.props` to `LocalBuildProperties.props`, set `HKManagedDir` to your own reference directory, and build in Release. A .NET Framework 4.7.2 targeting pack may be needed for conventional MSBuild builds. The canonical Python/Roslyn path uses the game's references directly and does not download a targeting pack.

The project preserves `HollowKnightLocal8`, `KO.HollowKnight8` and existing configuration identifiers. Public branding/version constants are centralized in `src/Core/ReleaseInfo.cs`; `Properties/AssemblyInfo.cs` consumes them. Embedded-resource logical names remain compatible with the runtime.

Do not commit `LocalBuildProperties.props`, `bin/`, `obj/`, `artifacts/`, compiler response files, game assemblies or debug output. See [source notes](SOURCE_NOTES.md).
