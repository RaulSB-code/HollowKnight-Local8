# Hollow Knight 8-Player Co-op source notes

The public v0.10.0 package is a consolidated, complete C# tree based on the latest Work alpha186 DLL and its feature sources. `src/Core/` recovers the final core behavior, including later integration patches; `src/Features/` contains the current feature implementations. Embedded artwork and translation tables are under `assets/`.

The earlier development pipeline applied integrations to a base binary. This public source does not require that binary, a merger or old builds. The final public DLL is compiled directly from this source tree. A second build from the extracted source package verifies byte-for-byte equality with the distributed DLL using the same SDK and Modding API game references.

A small number of recovered native coroutines retain explicit iterator state machines. Their private identifiers were made legal C# identifiers while preserving state, completion and disposal behavior. Types consumed by feature code have the internal visibility needed for a single-assembly build. Existing public mod classes, namespace, assembly filename, settings fields, save IDs and resource IDs remain compatible.

The source package deliberately excludes all DLLs, game/Team Cherry libraries, previous builds, local reference paths, logs, temporary files and debug output. It includes no new license grant beyond the repository's existing terms.
