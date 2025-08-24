$certFriendlyName = "Loan Affordability Calculator Sign"

$cert = (Get-ChildItem -Path "cert:\CurrentUser\My" | Where-Object { $_.FriendlyName -like "*$certFriendlyName*" } | Select-Object -First 1).Thumbprint

if (-not $cert) {
    Write-Error "Certificate with FriendlyName containing '$certFriendlyName' not found."
    exit 1
}

Write-Host "Using certificate: $certFriendlyName ($cert)"

$ProjectFile = "..\src\LoanCalculator\LoanCalculatorMaui.csproj"
$TargetFramework = "net9.0-windows10.0.19041.0"
$Configuration = "release"
$OutputDir = "bin\$Configuration\$TargetFramework\publish\"

msbuild $ProjectFile `
    /t:Publish `
    /p:Configuration=$Configuration `
    /p:TargetFramework=$TargetFramework `
    /p:OutputPath=$OutputDir `
    /p:GenerateAppxPackageOnBuild=true `
    /p:Platform=x64

#    /p:AppxPackageSigningEnabled=true `
#    /p:AppxPackageBuildMode=StoreOnly `
#    /p:AppxPackageCertificateThumbprint=$cert
