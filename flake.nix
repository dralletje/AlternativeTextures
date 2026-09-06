{
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";
  # inputs.csharpier-src = {
  #   url = "./csharpier-nix";
  #   inputs.nixpkgs.follows = "nixpkgs";
  # };

  outputs =
    # { nixpkgs, csharpier-src, ... }:
    { nixpkgs, ... }:
    let
      supportedSystems = [
        "x86_64-linux"
        "aarch64-linux"
        "x86_64-darwin"
        "aarch64-darwin"
      ];
      forAllSystems = nixpkgs.lib.genAttrs supportedSystems;
    in
    {
      devShells = forAllSystems (
        system:
        let
          pkgs = nixpkgs.legacyPackages.${system};

          ilspycmd-latest =
            (pkgs.buildDotnetGlobalTool {
              pname = "ilspycmd";
              version = "11.0.0.9375";
              nugetHash = "sha256-j1VbP8qQodelkFDXhTnGne7arUIXVr1P5HjRNb2sLeo=";
              dotnet-sdk = pkgs.dotnet-sdk_10;
              dotnet-runtime = pkgs.dotnet-runtime_10;
              useDotnetXFromEnv = false;
            }).overrideAttrs
              (old: {
                useDotnetFromEnv = false;
              });

          dotnet = pkgs.dotnetCorePackages.combinePackages [
            pkgs.dotnetCorePackages.sdk_10_0
            pkgs.dotnetCorePackages.sdk_11_0
            # pkgs.dotnetCorePackages.sdk_8_0
          ];
        in
        {
          default = pkgs.mkShell {
            name = "StardewValley";

            packages = with pkgs; [
              dotnet

              # roslyn-ls
              # roslyn-language-server-latest
              netcoredbg
              ilspycmd-latest
              # csharpier-latest
              # csharpier-main
              just
            ];
            shellHook = ''
              export DOTNET_ROOT="${dotnet}/share/dotnet"
              export DOTNET_ROOT_ARM64="$DOTNET_ROOT"
            '';
          };
        }
      );
    };
}
