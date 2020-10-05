$ErrorActionPreference = "Stop"

Remove-Item -LiteralPath Ae.Dns.Console.zip -ErrorAction Ignore

dotnet publish misc/Ae.Dns.Console --configuration Release --runtime linux-arm64

Compress-Archive -Path misc/Ae.Dns.Console/bin/Release/netcoreapp3.1/linux-arm64/publish/* -DestinationPath Ae.Dns.Console.zip