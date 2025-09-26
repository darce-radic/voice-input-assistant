# Voice Input Assistant - Deployment Script (Windows)
# This script automates the deployment process to Netlify

# Enable strict mode
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Define colors for output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Check if we're in the right directory
if (-not (Test-Path "package.json")) {
    Write-ColorOutput "❌ Error: package.json not found. Please run this script from the marketing directory." -Color "Red"
    exit 1
}

Write-ColorOutput "🚀 Voice Input Assistant - Deployment Script" -Color "Green"
Write-Host ""

# Check if Netlify CLI is installed
try {
    $null = Get-Command netlify -ErrorAction Stop
    Write-ColorOutput "✅ Netlify CLI found" -Color "Green"
} catch {
    Write-ColorOutput "⚠️  Netlify CLI not found. Installing..." -Color "Yellow"
    npm install -g netlify-cli
    Write-ColorOutput "✅ Netlify CLI installed successfully" -Color "Green"
}

# Build the project
Write-ColorOutput "📦 Building the project..." -Color "Yellow"
npm run build

if ($LASTEXITCODE -eq 0) {
    Write-ColorOutput "✅ Build completed successfully" -Color "Green"
} else {
    Write-ColorOutput "❌ Build failed. Please fix the errors and try again." -Color "Red"
    exit 1
}

# Check if .env.local exists
if (-not (Test-Path ".env.local")) {
    Write-ColorOutput "⚠️  Warning: .env.local not found. Using default environment variables." -Color "Yellow"
    Write-ColorOutput "   Create .env.local from .env.example for local development." -Color "Yellow"
}

# Deploy to Netlify
Write-ColorOutput "🌐 Deploying to Netlify..." -Color "Yellow"

# Check if site is already linked
if (-not (Test-Path ".netlify/state.json")) {
    Write-ColorOutput "📝 First time deployment detected. Initializing Netlify..." -Color "Yellow"
    netlify init
}

# Get deployment type from argument
$deploymentType = $args[0]

# Deploy based on argument
if ($deploymentType -eq "--prod" -or $deploymentType -eq "-p") {
    Write-ColorOutput "🚀 Deploying to production..." -Color "Yellow"
    netlify deploy --prod
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ Successfully deployed to production!" -Color "Green"
        
        # Try to get the site URL
        try {
            $status = netlify status --json | ConvertFrom-Json
            Write-ColorOutput "   Your site is live at: $($status.url)" -Color "Green"
        } catch {
            Write-ColorOutput "   Check Netlify dashboard for your site URL" -Color "Green"
        }
    }
} elseif ($deploymentType -eq "--preview" -or $deploymentType -eq "-d") {
    Write-ColorOutput "👁️  Creating preview deployment..." -Color "Yellow"
    netlify deploy
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ Preview deployment created!" -Color "Green"
        Write-ColorOutput "   Check the URL above to preview your changes." -Color "Green"
    }
} else {
    # Default to preview
    Write-ColorOutput "👁️  Creating preview deployment (default)..." -Color "Yellow"
    Write-ColorOutput "   Use --prod or -p flag for production deployment" -Color "Yellow"
    netlify deploy
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ Preview deployment created!" -Color "Green"
        Write-ColorOutput "   To deploy to production, run: .\scripts\deploy.ps1 --prod" -Color "Yellow"
    }
}

# Post-deployment tasks
Write-Host ""
Write-ColorOutput "📋 Post-deployment checklist:" -Color "Green"
Write-Host "   [ ] Test all pages and navigation"
Write-Host "   [ ] Verify download links work"
Write-Host "   [ ] Check responsive design"
Write-Host "   [ ] Test contact forms"
Write-Host "   [ ] Verify environment variables in Netlify dashboard"
Write-Host ""

# Show helpful commands
Write-ColorOutput "📚 Helpful commands:" -Color "Yellow"
Write-Host "   netlify open        - Open Netlify dashboard"
Write-Host "   netlify status      - Check deployment status"
Write-Host "   netlify env:list    - List environment variables"
Write-Host "   netlify functions:list - List serverless functions"
Write-Host ""

Write-ColorOutput "✨ Deployment script completed!" -Color "Green"