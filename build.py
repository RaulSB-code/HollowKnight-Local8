#!/usr/bin/env python3
"""Compile Hollow Knight 8-Player Co-op from this complete C# source tree."""
from pathlib import Path
import argparse, os, re, shutil, subprocess, sys

root = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--managed', default=os.environ.get('HK_MANAGED_DIR'), help='Modding API-patched hollow_knight_Data/Managed directory')
parser.add_argument('--output', default=str(root / 'artifacts' / 'HollowKnightLocal8.dll'))
parser.add_argument('--dotnet', default=os.environ.get('DOTNET', 'dotnet'))
args = parser.parse_args()
if not args.managed:
    parser.error('Supply --managed or HK_MANAGED_DIR; game libraries are not distributed with the source.')
managed = Path(args.managed).expanduser().resolve()
required = ['Assembly-CSharp.dll', 'MMHOOK_Assembly-CSharp.dll', 'PlayMaker.dll', 'MMHOOK_PlayMaker.dll', 'mscorlib.dll']
missing = [name for name in required if not (managed / name).is_file()]
if missing:
    parser.error('Missing Modding API game references: ' + ', '.join(missing))
dotnet = shutil.which(args.dotnet) or (str(Path(args.dotnet).resolve()) if Path(args.dotnet).is_file() else None)
if not dotnet:
    parser.error('A .NET SDK with Roslyn is required.')
sdks = subprocess.check_output([dotnet, '--list-sdks'], text=True).splitlines()
if not sdks:
    parser.error('No installed .NET SDK.')
version, location = sdks[-1].rsplit(' [', 1)
csc = Path(location.rstrip(']')) / version / 'Roslyn' / 'bincore' / 'csc.dll'
output = Path(args.output).resolve()
output.parent.mkdir(parents=True, exist_ok=True)
refs = [p for p in sorted(managed.glob('*.dll')) if p.name.lower() not in {'unityscenerepacker.dll', 'hollowknightlocal8.dll'}]
options = ['-nologo', '-nostdlib+', '-target:library', '-langversion:latest', '-optimize+', '-deterministic+', '-unsafe+', '-debug-', '-pathmap:' + str(root) + '=source', '-out:' + str(output)]
options += ['-r:' + str(p) for p in refs]
options += ['-resource:' + str(p) + ',' + p.name for p in sorted((root / 'assets').iterdir()) if p.is_file()]
options += [str(p) for p in sorted((root / 'src').rglob('*.cs'))]
options += [str(root / 'Properties' / 'AssemblyInfo.cs')]
# Response file belongs only to the build output; it is never source or distribution content.
rsp = output.parent / 'compile.rsp'
rsp.write_text('\n'.join('"' + s + '"' for s in options) + '\n', encoding='utf-8')
subprocess.run([dotnet, str(csc), '@' + str(rsp)], check=True)
print('Built ' + str(output))
