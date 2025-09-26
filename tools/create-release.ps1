# Voice Input Assistant Release Creation Script
# This script creates Squirrel releases for auto-update functionality

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [string]$OutputPath = ".\releases",
    [string]$PublishPath = ".\apps\desktop\bin\Release\net8.0-windows\win-x64\publish",
    [string]$ReleasesPath = ".\releases\packages",
    [string]$IconPath = ".\apps\desktop\Assets\icon.ico",
    [switch]$SignPackage = $false,
    [string]$CertificatePath = "",
    [string]$CertificatePassword = ""
)

# Ensure required tools are available
function Test-RequiredTools {
    Write-Host "Checking required tools..." -ForegroundColor Yellow
    
    $squirrelPath = Get-Command "Squirrel.exe" -ErrorAction SilentlyContinue
    if (-not $squirrelPath) {
        Write-Error "Squirrel.exe not found. Please install Squirrel via: dotnet tool install --global Codeification.Squirrel"
        return $false
    }
    
    $nugetPath = Get-Command "nuget.exe" -ErrorAction SilentlyContinue
    if (-not $nugetPath) {
        Write-Warning "nuget.exe not found. Attempting to download..."
        $nugetUrl = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
        $nugetExe = Join-Path $env:TEMP "nuget.exe"
        try {
            Invoke-WebRequest -Uri $nugetUrl -OutFile $nugetExe
            $env:PATH += ";$env:TEMP"
            Write-Host "NuGet downloaded successfully" -ForegroundColor Green
        }
        catch {
            Write-Error "Failed to download nuget.exe: $($_.Exception.Message)"
            return $false
        }
    }
    
    Write-Host "All required tools are available" -ForegroundColor Green
    return $true
}

function New-NuspecFile {
    param(
        [string]$Version,
        [string]$PublishPath,
        [string]$OutputPath
    )
    
    $nuspecPath = Join-Path $OutputPath "VoiceInputAssistant.nuspec"
    $nuspecContent = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
  <metadata>
    <id>VoiceInputAssistant</id>
    <version>$Version</version>
    <title>Voice Input Assistant</title>
    <authors>Voice Input Assistant Team</authors>
    <owners>Voice Input Assistant Team</owners>
    <licenseUrl>https://github.com/yourusername/voice-input-assistant/blob/main/LICENSE</licenseUrl>
    <projectUrl>https://github.com/yourusername/voice-input-assistant</projectUrl>
    <iconUrl>https://github.com/yourusername/voice-input-assistant/raw/main/apps/desktop/Assets/icon.png</iconUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <description>Revolutionary speech-to-text software that works anywhere on Windows with AI-powered post-processing.</description>
    <releaseNotes>
      See https://github.com/yourusername/voice-input-assistant/releases/tag/v$Version for detailed release notes.
    </releaseNotes>
    <copyright>Copyright © 2024 Voice Input Assistant Team</copyright>
    <tags>speech-to-text voice-recognition dictation accessibility windows</tags>
    <dependencies>
      <dependency id="Microsoft.WindowsDesktop.App" version="8.0.0" />
    </dependencies>
  </metadata>
  <files>
    <file src="$PublishPath\**\*" target="lib\net8.0-windows\" />
  </files>
</package>
"@
    
    Set-Content -Path $nuspecPath -Value $nuspecContent -Encoding UTF8
    Write-Host "Created nuspec file: $nuspecPath" -ForegroundColor Green
    return $nuspecPath
}

function New-NuGetPackage {
    param(
        [string]$NuspecPath,
        [string]$OutputPath
    )
    
    Write-Host "Creating NuGet package..." -ForegroundColor Yellow
    
    $nugetArgs = @(
        "pack",
        $NuspecPath,
        "-OutputDirectory", $OutputPath,
        "-NoPackageAnalysis",
        "-Verbosity", "normal"
    )
    
    $result = & nuget @nugetArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create NuGet package"
        return $null
    }
    
    $packagePath = Join-Path $OutputPath "VoiceInputAssistant.$Version.nupkg"
    if (Test-Path $packagePath) {
        Write-Host "NuGet package created: $packagePath" -ForegroundColor Green
        return $packagePath
    }
    else {
        Write-Error "NuGet package not found at expected path: $packagePath"
        return $null
    }
}

function New-SquirrelRelease {
    param(
        [string]$PackagePath,
        [string]$ReleasesPath,
        [string]$IconPath,
        [bool]$SignPackage,
        [string]$CertificatePath,
        [string]$CertificatePassword
    )
    
    Write-Host "Creating Squirrel release..." -ForegroundColor Yellow
    
    # Ensure releases directory exists
    if (-not (Test-Path $ReleasesPath)) {
        New-Item -ItemType Directory -Path $ReleasesPath -Force | Out-Null
    }
    
    $squirrelArgs = @(
        "--releasify", $PackagePath,
        "--releaseDir", $ReleasesPath,
        "--setupIcon", $IconPath,
        "--no-msi"
    )
    
    # Add code signing if requested
    if ($SignPackage -and $CertificatePath -and $CertificatePassword) {
        $squirrelArgs += @(
            "--signWithParams", "/f `"$CertificatePath`" /p `"$CertificatePassword`" /tr http://timestamp.digicert.com /td sha256 /fd sha256"
        )
        Write-Host "Package will be code signed" -ForegroundColor Green
    }
    
    Write-Host "Running Squirrel with args: $($squirrelArgs -join ' ')" -ForegroundColor Gray
    
    try {
        $result = & Squirrel @squirrelArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Squirrel failed with exit code: $LASTEXITCODE"
            Write-Host "Squirrel output:" -ForegroundColor Red
            Write-Host $result -ForegroundColor Red
            return $false
        }
        
        Write-Host "Squirrel release created successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "Error running Squirrel: $($_.Exception.Message)"
        return $false
    }
}

function Show-ReleaseInfo {
    param(
        [string]$ReleasesPath,
        [string]$Version
    )
    
    Write-Host "`n=== Release Information ===" -ForegroundColor Cyan
    Write-Host "Version: $Version" -ForegroundColor White
    Write-Host "Releases Path: $ReleasesPath" -ForegroundColor White
    
    $releaseFiles = Get-ChildItem -Path $ReleasesPath -File | Sort-Object Name
    
    Write-Host "`nGenerated Files:" -ForegroundColor Yellow
    foreach ($file in $releaseFiles) {
        $sizeKB = [math]::Round($file.Length / 1KB, 2)
        Write-Host "  $($file.Name) ($sizeKB KB)" -ForegroundColor Gray
    }
    
    $setupFile = $releaseFiles | Where-Object { $_.Name -like "Setup.exe" }
    if ($setupFile) {
        Write-Host "`n✅ Setup.exe installer created" -ForegroundColor Green
    }
    
    $releasesFile = $releaseFiles | Where-Object { $_.Name -eq "RELEASES" }
    if ($releasesFile) {
        Write-Host "✅ RELEASES file created for auto-updates" -ForegroundColor Green
    }
    
    $nupkgFile = $releaseFiles | Where-Object { $_.Name -like "*.nupkg" }
    if ($nupkgFile) {
        Write-Host "✅ Delta package created for updates" -ForegroundColor Green
    }
    
    Write-Host "`nUpload these files to your release server for auto-update functionality:" -ForegroundColor Cyan
    Write-Host "  - RELEASES" -ForegroundColor White
    Write-Host "  - *.nupkg files" -ForegroundColor White
    Write-Host "  - Setup.exe (for new installations)" -ForegroundColor White
}

# Main execution
Write-Host "Voice Input Assistant Release Creator" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor White
Write-Host "======================================" -ForegroundColor Cyan

# Validate inputs
if (-not (Test-Path $PublishPath)) {
    Write-Error "Publish path does not exist: $PublishPath"
    Write-Host "Please ensure the application has been published first using:" -ForegroundColor Yellow
    Write-Host "  dotnet publish apps/desktop/VoiceInputAssistant.csproj -c Release -r win-x64 --self-contained false" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-RequiredTools)) {
    exit 1
}

# Create output directories
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

if (-not (Test-Path $ReleasesPath)) {
    New-Item -ItemType Directory -Path $ReleasesPath -Force | Out-Null
}

try {
    # Step 1: Create nuspec file
    Write-Host "`nStep 1: Creating NuSpec file..." -ForegroundColor Yellow
    $nuspecPath = New-NuspecFile -Version $Version -PublishPath $PublishPath -OutputPath $OutputPath
    
    # Step 2: Create NuGet package
    Write-Host "`nStep 2: Creating NuGet package..." -ForegroundColor Yellow
    $packagePath = New-NuGetPackage -NuspecPath $nuspecPath -OutputPath $OutputPath
    if (-not $packagePath) {
        exit 1
    }
    
    # Step 3: Create Squirrel release
    Write-Host "`nStep 3: Creating Squirrel release..." -ForegroundColor Yellow
    $success = New-SquirrelRelease -PackagePath $packagePath -ReleasesPath $ReleasesPath -IconPath $IconPath -SignPackage $SignPackage -CertificatePath $CertificatePath -CertificatePassword $CertificatePassword
    if (-not $success) {
        exit 1
    }
    
    # Step 4: Show results
    Show-ReleaseInfo -ReleasesPath $ReleasesPath -Version $Version
    
    Write-Host "`n🎉 Release creation completed successfully!" -ForegroundColor Green
    Write-Host "Upload the contents of '$ReleasesPath' to your release server." -ForegroundColor Cyan
}
catch {
    Write-Error "An error occurred during release creation: $($_.Exception.Message)"
    exit 1
}