# default: lint build test

INSTALL_DIRECTORY := "stardew_install_symlink"

build project="AlternativeTextures":
    #!/usr/bin/env bash
    echo Building…
    cd "{{ project }}"
    dotnet build

run: build
    "{{ INSTALL_DIRECTORY }}"/StardewModdingAPI --mods-path {{ absolute_path("./Mods") }}

# Decompile Stardew Valley.dll into ./Decompiled
# and generate a .pdb from it which we put next to  Stardew Valley.dll.
# This will make it so debugger stacktraces will point to the correct file+line
# relative to ./Decompiled. Use `./netcoredbg` to then map those to the actual
# ./Decompiled folder for better inspection.
decompile: _install-folder-exists
    #!/usr/bin/env bash
    echo Decompiling Stardew Valley.dll…
    ilspycmd -p -o ./Decompiled "{{ INSTALL_DIRECTORY }}/Stardew\ Valley.dll"
    ilspycmd -genpdb "{{ INSTALL_DIRECTORY }}/Stardew\ Valley.dll"

# Symlink the Stardew Valley game installation folder to
# ./stardew_install_symlink in this project directory,
# Most other commands do expect that to be present.
symlink-stardew-installation path="${HOME}/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS":
    #!/usr/bin/env bash
    set -euo pipefail

    if [[ -d "{{ path }}" ]]; then
        ln -sfn "{{ path }}" "{{ INSTALL_DIRECTORY }}"
        exit 0
    else
      echo "Error: Stardew Valley installation directory not found." >&2
      exit 1
    fi

_install-folder-exists:
    #!/usr/bin/env bash
    if [ ! -d "{{ INSTALL_DIRECTORY }}" ]; then
      echo "Error: Directory '{{ INSTALL_DIRECTORY }}' does not exist." >&2
      just --show symlink-stardew-installation
      exit 1
    fi
