# AlternativeTexturesPlus

- Create a symlink `./stardew_install_symlink` to the folder containing the stardew valley installation (this is where your `Mods` folder is)

- To get nice paths while debugging in Zed:
  - set `dap.netcoredbg.binary` to `netcoredbg-proxy/app.ts`
  - Decompile Stardew Valley in `./Decompiled`
  - Generate a `.pdb` for it and put that in the Stardew installation folder (`Stardew Valley.pdb`, next to `Startdew Valley.dll`)
