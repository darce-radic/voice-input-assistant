#!/bin/bash

# Voice Input Assistant - Deployment Script
# This script automates the deployment process to Netlify

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_color() {
    COLOR=$1
    MESSAGE=$2
    echo -e "${COLOR}${MESSAGE}${NC}"
}

# Check if we're in the right directory
if [ ! -f "package.json" ]; then
    print_color $RED "❌ Error: package.json not found. Please run this script from the marketing directory."
    exit 1
fi

print_color $GREEN "🚀 Voice Input Assistant - Deployment Script"
echo ""

# Check if Netlify CLI is installed
if ! command -v netlify &> /dev/null; then
    print_color $YELLOW "⚠️  Netlify CLI not found. Installing..."
    npm install -g netlify-cli
    print_color $GREEN "✅ Netlify CLI installed successfully"
fi

# Build the project
print_color $YELLOW "📦 Building the project..."
npm run build

if [ $? -eq 0 ]; then
    print_color $GREEN "✅ Build completed successfully"
else
    print_color $RED "❌ Build failed. Please fix the errors and try again."
    exit 1
fi

# Check if .env.local exists
if [ ! -f ".env.local" ]; then
    print_color $YELLOW "⚠️  Warning: .env.local not found. Using default environment variables."
    print_color $YELLOW "   Create .env.local from .env.example for local development."
fi

# Deploy to Netlify
print_color $YELLOW "🌐 Deploying to Netlify..."

# Check if site is already linked
if [ ! -f ".netlify/state.json" ]; then
    print_color $YELLOW "📝 First time deployment detected. Initializing Netlify..."
    netlify init
fi

# Deploy based on argument
if [ "$1" == "--prod" ] || [ "$1" == "-p" ]; then
    print_color $YELLOW "🚀 Deploying to production..."
    netlify deploy --prod
    
    if [ $? -eq 0 ]; then
        print_color $GREEN "✅ Successfully deployed to production!"
        print_color $GREEN "   Your site is live at: $(netlify status --json | grep -o '"url":"[^"]*' | grep -o '[^"]*$')"
    fi
elif [ "$1" == "--preview" ] || [ "$1" == "-d" ]; then
    print_color $YELLOW "👁️  Creating preview deployment..."
    netlify deploy
    
    if [ $? -eq 0 ]; then
        print_color $GREEN "✅ Preview deployment created!"
        print_color $GREEN "   Check the URL above to preview your changes."
    fi
else
    # Default to preview
    print_color $YELLOW "👁️  Creating preview deployment (default)..."
    print_color $YELLOW "   Use --prod or -p flag for production deployment"
    netlify deploy
    
    if [ $? -eq 0 ]; then
        print_color $GREEN "✅ Preview deployment created!"
        print_color $YELLOW "   To deploy to production, run: ./scripts/deploy.sh --prod"
    fi
fi

# Post-deployment tasks
echo ""
print_color $GREEN "📋 Post-deployment checklist:"
echo "   [ ] Test all pages and navigation"
echo "   [ ] Verify download links work"
echo "   [ ] Check responsive design"
echo "   [ ] Test contact forms"
echo "   [ ] Verify environment variables in Netlify dashboard"
echo ""

# Show helpful commands
print_color $YELLOW "📚 Helpful commands:"
echo "   netlify open        - Open Netlify dashboard"
echo "   netlify status      - Check deployment status"
echo "   netlify env:list    - List environment variables"
echo "   netlify functions:list - List serverless functions"
echo ""

print_color $GREEN "✨ Deployment script completed!"