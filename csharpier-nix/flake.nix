{
  description = "CSharpier built from GitHub";
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";

  outputs =
    { self, nixpkgs }:
    let
      supportedSystems = [
        "x86_64-linux"
        "aarch64-linux"
        "x86_64-darwin"
        "aarch64-darwin"
      ];

      # Helper function to generate attributes for all systems
      forAllSystems = nixpkgs.lib.genAttrs supportedSystems;
    in
    {
      packages = forAllSystems (
        system:
        let
          pkgs = nixpkgs.legacyPackages.${system};
        in
        {
          default = pkgs.buildDotnetModule {
            pname = "csharpier";
            version = "2ece6d6";

            src = pkgs.fetchFromGitHub {
              owner = "belav";
              repo = "csharpier";
              rev = "2ece6d6";
              hash = "";
            };

            projectFile = "Src/CSharpier.Cli/CSharpier.Cli.csproj";
            nugetDeps = ./deps.nix;

            dotnet-sdk = pkgs.dotnetCorePackages.sdk_8_0;
            dotnet-runtime = pkgs.dotnetCorePackages.runtime_8_0;
          };
        }
      );
    };
}
