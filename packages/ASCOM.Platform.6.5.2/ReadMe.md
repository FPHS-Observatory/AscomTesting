# ASCOM Platform Components via NuGet

This repository contains a copy of the compiled binaries taken from the ASCOM Developer Components installer together with a `.nuspec` file for creating a NuGet package.

This package can be easily installed when developing ASCOM drivers or applications and allows development to proceed without having to install the ASCOM Platform. This is probably of most use when using a build server, where it is burdensome to have to install ASCOM on each build agent. Note: the ASCOM Profile utility will generate runtime errors is the ASCOM Profile Store has not been created in the registry. A PowerShell script is included that will bootstrap the minimum required profile store entries to allow the Profile component to work.

No originality is claimed, this is simply an alternative mechanism for distributing the binaries from the ASCOM Platform. This is a derivative work and the binaries are redistributed with permission, under the Creative Commons license of the ASCOM Platform.

All other content is Copyright © 2016-18 Tigra Astronomy, all rights reserved. You may use this content under the terms of [Tigra MIT License](http://tigra.mit-license.org "MIT License for Tigra Astronomy").



Tim Long,  
Tigra Astronomy [http://tigra-astronomy.com](http://tigra-astronomy.com "Tigra Astronomy Web Site")  
February 2020

Copyright © 2010-21 Tigra Astronomy, all rights reserved

# Updating

This repository is open source and if you need to update it then we suggest proceeding as follows.
You will need the `GitVersion command line tools` installed.

1. Fork the repository.
2. In your fork, create a release branch named for the platform version,
e.g. `release/6.5.1` for ASCOM Platform 6.5 SP1
3. Copy updated binaries from `%ProgramFiles(x86)%\ASCOM\Platform 6 Developer Components\Components\Platform6` into the `lib\net40` folder.
4. Copy the same binaries again into the `lib\net35` folder, but **omit the `ASCOM.Utilities.Video*` files** as they are not compatible with .NET 3.5
5. Edit the `root\ASCOM.Platform.nuspec` file and check everything is correct;
add any release notes you think are needed.
Leave the version alone, it should be `0.0.0`. The version is set by GitVersion during packing.
6. Run `Build-Packages.ps1' and verify that it builds the packages with the correct semantic version.
You are building pre-release packages and your semantic version should look like `6.5.1-beta.1`
7. Optional: If you have an API key, you can run `Push-MyGetPackages.ps1` to push the packages to The ASCOM Initiative's prerelease feed. If you would like an API key, just ask in the `ASCOM-Developers` group. You can also push the packages to a private feed of your choice but please **do not push the packages to nuget.org**.
8. Submit a pull request back to the upstream repository targetting the `master` branch.

Your PR will be built by our build server and your packages will be published on the TiGra Astronomy OSS integration build feed. You can find this feed at http://teamcity.tigra-astronomy.com:8111/guestAuth/app/nuget/feed/TigraOss/TigraOSS/v3/index.json

In due course we will merge your PR and push the nuget packages to nuget.org