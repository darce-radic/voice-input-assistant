# Voice Input Assistant - PowerShell Installation Script
# This script sets up the development environment on Windows

param(
    [switch]$SkipNodeCheck,
    [switch]$SkipGitHooks,
    [switch]$Verbose
)

# Colors for output
$Colors = @{
    Info = "Blue"
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
}

function Write-StatusMessage {
    param([string]$Message, [string]$Type = "Info")
    $color = $Colors[$Type]
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] " -NoNewline
    Write-Host "[$Type] " -ForegroundColor $color -NoNewline
    Write-Host $Message
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "=" * 50 -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host "=" * 50 -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "🎤 Voice Input Assistant - PowerShell Setup Script" -ForegroundColor Magenta
Write-Host "=================================================" -ForegroundColor Magenta
Write-Host ""

# Check PowerShell version
$psVersion = $PSVersionTable.PSVersion
Write-StatusMessage "PowerShell Version: $psVersion" "Info"

if ($psVersion.Major -lt 5) {
    Write-StatusMessage "PowerShell 5.0 or higher is recommended" "Warning"
}

# Check if Node.js is installed
Write-Section "Checking Node.js Installation"

try {
    $nodeVersion = node --version 2>$null
    if ($nodeVersion) {
        Write-StatusMessage "Node.js is installed: $nodeVersion" "Success"
        
        # Check version
        $majorVersion = [int]($nodeVersion -replace 'v(\d+)\..*', '$1')
        if ($majorVersion -lt 18) {
            Write-StatusMessage "Node.js version should be >= 18.0.0 for best compatibility" "Warning"
        }
    } else {
        throw "Node.js not found"
    }
} catch {
    if (-not $SkipNodeCheck) {
        Write-StatusMessage "Node.js is not installed or not in PATH" "Error"
        Write-StatusMessage "Please install Node.js >= 18.0.0 from https://nodejs.org/" "Error"
        Write-StatusMessage "Or use -SkipNodeCheck to skip this check" "Info"
        exit 1
    } else {
        Write-StatusMessage "Skipping Node.js check as requested" "Warning"
    }
}

# Check if npm is installed
try {
    $npmVersion = npm --version 2>$null
    if ($npmVersion) {
        Write-StatusMessage "npm is installed: v$npmVersion" "Success"
    } else {
        throw "npm not found"
    }
} catch {
    Write-StatusMessage "npm is not installed or not in PATH" "Error"
    exit 1
}

# Create necessary directories
Write-Section "Creating Directory Structure"

$directories = @(
    "public\icons",
    "public\screenshots",
    "src\assets",
    "src\types",
    "src\hooks",
    "src\contexts",
    "src\utils"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-StatusMessage "Created directory: $dir" "Success"
    } else {
        Write-StatusMessage "Directory already exists: $dir" "Info"
    }
}

# Setup environment files
Write-Section "Setting Up Environment Files"

if (-not (Test-Path ".env")) {
    if (Test-Path ".env.example") {
        Copy-Item ".env.example" ".env"
        Write-StatusMessage "Created .env file from .env.example" "Success"
        Write-StatusMessage "Please update .env file with your actual API keys" "Warning"
    } else {
        Write-StatusMessage ".env.example not found, creating basic .env file" "Warning"
        
        $envContent = @"
NODE_ENV=development
REACT_APP_ENV=development
GENERATE_SOURCEMAP=true
HTTPS=false
REACT_APP_MOCK_API=true
REACT_APP_DEBUG_MODE=true
"@
        
        $envContent | Out-File -FilePath ".env" -Encoding UTF8
        Write-StatusMessage "Created basic .env file" "Success"
    }
} else {
    Write-StatusMessage ".env file already exists" "Success"
}

# Install dependencies
Write-Section "Installing Dependencies"

Write-StatusMessage "Clearing npm cache..." "Info"
try {
    npm cache clean --force
    Write-StatusMessage "npm cache cleared" "Success"
} catch {
    Write-StatusMessage "Could not clear npm cache, continuing..." "Warning"
}

Write-StatusMessage "Installing production and development dependencies..." "Info"
Write-StatusMessage "This may take a few minutes..." "Info"

try {
    $installOutput = npm install 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-StatusMessage "Dependencies installed successfully" "Success"
        if ($Verbose) {
            Write-Host $installOutput
        }
    } else {
        throw "npm install failed"
    }
} catch {
    Write-StatusMessage "Failed to install dependencies" "Error"
    Write-StatusMessage "Error: $($_.Exception.Message)" "Error"
    exit 1
}

# Generate VAPID keys for push notifications
Write-Section "Generating VAPID Keys"

try {
    # Check if web-push is available
    $webPushInstalled = npm list web-push 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-StatusMessage "Installing web-push package..." "Info"
        npm install --save-dev web-push
    }
    
    Write-StatusMessage "Generating VAPID keys..." "Info"
    $vapidOutput = npx web-push generate-vapid-keys 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        $vapidOutput | Out-File -FilePath "vapid-keys.txt" -Encoding UTF8
        Write-StatusMessage "VAPID keys generated and saved to vapid-keys.txt" "Success"
        Write-StatusMessage "Please add these keys to your .env file" "Warning"
        Write-Host ""
        Write-Host $vapidOutput -ForegroundColor Yellow
        Write-Host ""
    } else {
        Write-StatusMessage "Could not generate VAPID keys automatically" "Warning"
        Write-StatusMessage "You can generate them later with: npx web-push generate-vapid-keys" "Info"
    }
} catch {
    Write-StatusMessage "VAPID key generation failed: $($_.Exception.Message)" "Warning"
    Write-StatusMessage "You can generate them later with: npx web-push generate-vapid-keys" "Info"
}

# Setup Git hooks (optional)
Write-Section "Setting Up Git Hooks"

if ((Test-Path ".git") -and (-not $SkipGitHooks)) {
    Write-StatusMessage "Setting up Git pre-commit hooks..." "Info"
    
    $gitHookPath = ".git\hooks\pre-commit"
    $hookContent = @'
#!/bin/sh
echo "Running pre-commit checks..."

# Run type checking
echo "Running TypeScript type checking..."
npm run type-check
if [ $? -ne 0 ]; then
    echo "TypeScript type checking failed. Commit aborted."
    exit 1
fi

# Run linting
echo "Running ESLint..."
npm run lint
if [ $? -ne 0 ]; then
    echo "ESLint failed. Commit aborted."
    exit 1
fi

echo "Pre-commit checks passed!"
'@
    
    try {
        $hookContent | Out-File -FilePath $gitHookPath -Encoding ASCII
        # Make executable (if on WSL or Git Bash)
        if (Get-Command "chmod" -ErrorAction SilentlyContinue) {
            chmod +x $gitHookPath
        }
        Write-StatusMessage "Git pre-commit hooks setup" "Success"
    } catch {
        Write-StatusMessage "Could not setup Git hooks: $($_.Exception.Message)" "Warning"
    }
} elseif ($SkipGitHooks) {
    Write-StatusMessage "Skipping Git hooks setup as requested" "Info"
} else {
    Write-StatusMessage "Not a Git repository, skipping Git hooks setup" "Info"
}

# Verify installation
Write-Section "Verifying Installation"

$criticalFiles = @("package.json", "tsconfig.json", "craco.config.js", ".env")

foreach ($file in $criticalFiles) {
    if (Test-Path $file) {
        Write-StatusMessage "✓ $file exists" "Success"
    } else {
        Write-StatusMessage "✗ $file is missing" "Error"
    }
}

# Try to run type checking
Write-StatusMessage "Running type check test..." "Info"
try {
    $typeCheckOutput = npm run type-check 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-StatusMessage "Type checking passed" "Success"
    } else {
        Write-StatusMessage "Type checking failed - this may be normal if source files are missing" "Warning"
        if ($Verbose) {
            Write-Host $typeCheckOutput
        }
    }
} catch {
    Write-StatusMessage "Could not run type checking" "Warning"
}

# Final summary
Write-Section "Setup Complete!"

Write-Host "🎉 Setup completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Update your .env file with actual API keys"
Write-Host "2. Run 'npm start' to start the development server"
Write-Host "3. Run 'npm run https' for HTTPS development (required for PWA features)"
Write-Host "4. Run 'npm run build' to create a production build"
Write-Host "5. Run 'npm run pwa-test' to test PWA functionality"
Write-Host ""
Write-Host "Available scripts:" -ForegroundColor Cyan
Write-Host "• npm start          - Start development server"
Write-Host "• npm run https      - Start HTTPS development server"
Write-Host "• npm run build      - Create production build"
Write-Host "• npm run test       - Run tests"
Write-Host "• npm run lint       - Run ESLint"
Write-Host "• npm run type-check - Run TypeScript type checking"
Write-Host "• npm run pwa-test   - Build and serve for PWA testing"
Write-Host ""
Write-Host "⚠️  Remember to configure your API keys in the .env file!" -ForegroundColor Yellow
Write-Host ""

# Check if Windows Terminal or modern console
if ($env:WT_SESSION -or $env:ConEmuANSI) {
    Write-Host "💡 Tip: Use Windows Terminal for the best development experience!" -ForegroundColor Magenta
}

Write-StatusMessage "Installation script completed successfully" "Success"