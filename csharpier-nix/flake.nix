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

            # postPatch = ''
            #   rm -f .config/dotnet-tools.json
            #   rm -f global.json

            #   # Prevent MSBuild from enforcing the osx-arm64 RuntimeIdentifier on the netstandard2.0 generator
            #   substituteInPlace Src/CSharpier.Generators/CSharpier.Generators.csproj \
            #       --replace-fail "</Project>" '
            #     <PropertyGroup>
            #       <RuntimeIdentifier></RuntimeIdentifier>
            #       <RuntimeIdentifiers></RuntimeIdentifiers>
            #     </PropertyGroup>
            #   </Project>'
            # '';

            src = pkgs.fetchFromGitHub {
              owner = "belav";
              repo = "csharpier";
              rev = "2ece6d6";
              hash = "sha256-Cvqd3C39k2IrVe8uwMwJok364UJ+r/FCQGHy7Ijy3Ts=";
              fetchSubmodules = true;
            };

            # selfContainedBuild = false;
            # dotnetFlags = [
            #   "-p:UseAppHost=false"
            #   "-nodeReuse:false"
            #   "-maxCpuCount:1"
            #   "-p:RuntimeIdentifier="
            #   "-p:RuntimeIdentifiers="
            # ];

            # dotnetRestoreFlags = [
            #   "-p:RuntimeIdentifier="
            #   "-p:RuntimeIdentifiers="
            # ];
            # dotnetRestoreFlags = [
            #   "-p:MSBuildEnableWorkloadResolver=false"
            #   "-v detailed"
            #   "-p:NuGetAudit=false"
            #   "-p:TargetFrameworks=netstandard2.0"
            #   "-p:RestoreSources=[https://api.nuget.org/v3/index.json](https://api.nuget.org/v3/index.json)"
            # ];
            # dotnetBuildFlags = [
            #   "-p:MSBuildEnableWorkloadResolver=false"
            #   "-p:TargetFrameworks=netstandard2.0"
            # ];
            # dotnetRestoreFlags = [ "-v detailed" ];
            # projectFile = "Src/CSharpier.Cli/CSharpier.Cli.csproj";
            projectFile = "Src/CSharpier.Generators/CSharpier.Generators.csproj";
            # projectFile = "CSharpier.slnx";
            nugetDeps = ./2.json;

            dotnet-sdk = pkgs.dotnetCorePackages.combinePackages [
              # pkgs.dotnetCorePackages.sdk_10_0
              pkgs.dotnetCorePackages.sdk_11_0
              # pkgs.dotnetCorePackages.sdk_8_0
            ];
            dotnet-runtime = pkgs.dotnetCorePackages.runtime_10_0;
          };
        }
      );
    };
}
