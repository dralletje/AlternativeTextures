# default: lint build test

LOCAL_GAME_PATH := "stardew_install_symlink"

build project="AlternativeTextures": _install-folder-exists
    #!/usr/bin/env bash
    echo Building…
    cd "{{ project }}"
    dotnet build

run: build
    "{{ LOCAL_GAME_PATH }}"/StardewModdingAPI --mods-path {{ absolute_path("./Mods") }}

# Decompile Stardew Valley.dll into ./Decompiled
# and generate a .pdb from it which we put next to  Stardew Valley.dll.
# This will make it so debugger stacktraces will point to the correct file+line
# relative to ./Decompiled. Use `./netcoredbg` to then map those to the actual
# ./Decompiled folder for better inspection.
decompile: _install-folder-exists
    #!/usr/bin/env bash
    echo Decompiling Stardew Valley.dll…
    ilspycmd -p -o ./Decompiled "{{ LOCAL_GAME_PATH }}/Stardew\ Valley.dll"
    ilspycmd -genpdb "{{ LOCAL_GAME_PATH }}/Stardew\ Valley.dll"

[doc('
Symlink the Stardew Valley game installation folder
to ./stardew_install_symlink in this project directory,
Most other commands do expect that to be present.
')]
symlink-stardew-installation path=(env('HOME') / "Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS"):
    #!/usr/bin/env bash
    set -euo pipefail

    if [[ ! -d "{{ path }}" ]]; then
      echo "Error: Stardew Valley installation directory not found." >&2
      exit 1
    fi
    if [[ -e "{{ LOCAL_GAME_PATH }}" ]]; then
      if [[ -L "{{ LOCAL_GAME_PATH }}" && "$(realpath "{{ LOCAL_GAME_PATH }}")" == "$(realpath "{{ path }}")" ]]; then
        echo "Path already symlinked correctly." >&2
        exit 0
      else
        echo "Error: '{{ LOCAL_GAME_PATH }}' already exists." >&2
        exit 1
      fi
    fi

    ln -sfn "{{ path }}" "{{ LOCAL_GAME_PATH }}"
    echo "Linked {{ path }} to {{ LOCAL_GAME_PATH }}." >&2
    exit 0

_install-folder-exists:
    #!/usr/bin/env bash
    if [ ! -d "{{ LOCAL_GAME_PATH }}" ]; then
      echo "Error: Directory '{{ LOCAL_GAME_PATH }}' does not exist." >&2
      just --usage symlink-stardew-installation
      exit 1
    fi
