{
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";
  inputs.csharpier-src = {
    url = "./csharpier-nix";
    inputs.nixpkgs.follows = "nixpkgs";
  };

  outputs =
    { nixpkgs, csharpier-src, ... }:
    let
      supportedSystems = [
        "x86_64-linux"
        "aarch64-linux"
        "x86_64-darwin"
        "aarch64-darwin"
      ];
      forAllSystems = nixpkgs.lib.genAttrs supportedSystems;
      csharpier-main = nixpkgs.legacyPackages.aarch64-darwin.callPackage ./csharpier.nix { };
    in
    {

      packages.aarch64-darwin.csharpier = csharpier-main;
      packages.aarch64-darwin.fetch-deps = csharpier-main.passthru.fetch-deps;

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

          csharpier-main = csharpier-src.packages.${system}.default;

          # csharpier-latest = pkgs.buildDotnetGlobalTool {
          #   pname = "csharpier";
          #   version = "1.3.0";
          #   nugetHash = "sha256-hwieEoQTcATyKZIZ7CQSWANPBv+pEShg6cDXU5EIexU=";
          #   dotnet-sdk = pkgs.dotnet-sdk_11;
          #   dotnet-runtime = pkgs.dotnet-runtime_11;
          # };
        in
        {
          default = pkgs.mkShell {
            name = "StardewValley";

            packages = with pkgs; [
              dotnet-sdk_11
              roslyn-ls
              netcoredbg
              ilspycmd-latest
              # csharpier-latest
              csharpier-main
              just
            ];
            shellHook = ''

              export DOTNET_ROOT=${pkgs.dotnet-sdk}
            '';
          };
        }
      );
    };
}
