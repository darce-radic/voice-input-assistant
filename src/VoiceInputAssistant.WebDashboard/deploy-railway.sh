#!/bin/bash

# Voice Input Assistant - Railway Deployment Script
# Deploy to Railway with environment-specific configurations

echo "🚂 Voice Input Assistant - Railway Deployment"
echo "============================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT=${1:-"production"}
PROJECT_NAME="voice-input-assistant"

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Railway CLI is installed
check_railway_cli() {
    print_status "Checking Railway CLI installation..."
    
    if ! command -v railway &> /dev/null; then
        print_error "Railway CLI is not installed!"
        print_status "Install it with: npm install -g @railway/cli"
        print_status "Or visit: https://docs.railway.app/develop/cli"
        exit 1
    fi
    
    print_success "Railway CLI is installed"
    
    # Check if logged in
    if ! railway whoami &> /dev/null; then
        print_error "You are not logged into Railway!"
        print_status "Login with: railway login"
        exit 1
    fi
    
    print_success "Logged into Railway"
}

# Validate project structure
validate_project() {
    print_status "Validating project structure..."
    
    if [ ! -f "package.json" ]; then
        print_error "package.json not found. Are you in the project root?"
        exit 1
    fi
    
    if [ ! -f "Dockerfile.railway" ]; then
        print_error "Dockerfile.railway not found. Cannot deploy without it."
        exit 1
    fi
    
    print_success "Project structure validated"
}

# Set environment variables
set_environment_variables() {
    print_status "Setting environment variables for $ENVIRONMENT..."
    
    case $ENVIRONMENT in
        "production")
            railway variables set NODE_ENV=production
            railway variables set REACT_APP_ENV=production
            railway variables set GENERATE_SOURCEMAP=false
            railway variables set REACT_APP_DEBUG_MODE=false
            railway variables set REACT_APP_MOCK_API=false
            railway variables set REACT_APP_ENABLE_ANALYTICS=true
            railway variables set REACT_APP_BASE_URL=https://$PROJECT_NAME.up.railway.app
            print_success "Production environment variables set"
            ;;
        "staging")
            railway variables set NODE_ENV=production
            railway variables set REACT_APP_ENV=staging
            railway variables set GENERATE_SOURCEMAP=true
            railway variables set REACT_APP_DEBUG_MODE=true
            railway variables set REACT_APP_MOCK_API=false
            railway variables set REACT_APP_ENABLE_ANALYTICS=false
            railway variables set REACT_APP_BASE_URL=https://$PROJECT_NAME-staging.up.railway.app
            print_success "Staging environment variables set"
            ;;
        "development")
            railway variables set NODE_ENV=development
            railway variables set REACT_APP_ENV=development
            railway variables set GENERATE_SOURCEMAP=true
            railway variables set REACT_APP_DEBUG_MODE=true
            railway variables set REACT_APP_MOCK_API=true
            railway variables set REACT_APP_ENABLE_ANALYTICS=false
            railway variables set REACT_APP_BASE_URL=https://$PROJECT_NAME-dev.up.railway.app
            print_success "Development environment variables set"
            ;;
        *)
            print_error "Unknown environment: $ENVIRONMENT"
            print_status "Valid environments: production, staging, development"
            exit 1
            ;;
    esac
    
    # Set common variables
    railway variables set REACT_APP_VERSION=$(node -p "require('./package.json').version")
    railway variables set REACT_APP_BUILD_DATE=$(date -u +%Y-%m-%dT%H:%M:%SZ)
    railway variables set REACT_APP_RAILWAY_PROJECT=$PROJECT_NAME
    railway variables set PORT=3000
}

# Run pre-deployment checks
run_pre_checks() {
    print_status "Running pre-deployment checks..."
    
    # Check Node.js version
    NODE_VERSION=$(node --version)
    print_status "Node.js version: $NODE_VERSION"
    
    # Install dependencies
    print_status "Installing dependencies..."
    npm ci --silent
    
    # Run TypeScript check
    print_status "Running TypeScript type checking..."
    if npm run type-check; then
        print_success "TypeScript checks passed"
    else
        print_warning "TypeScript issues found, but continuing..."
    fi
    
    # Run linting
    print_status "Running ESLint..."
    if npm run lint; then
        print_success "Linting passed"
    else
        print_warning "Linting issues found, but continuing..."
    fi
    
    # Test build locally
    print_status "Testing local build..."
    if npm run build; then
        print_success "Local build successful"
        rm -rf build # Clean up
    else
        print_error "Local build failed!"
        exit 1
    fi
}

# Deploy to Railway
deploy_to_railway() {
    print_status "Deploying to Railway ($ENVIRONMENT environment)..."
    
    # Use Railway-specific Dockerfile
    cp Dockerfile.railway Dockerfile
    
    # Deploy with Railway
    if railway up --detach; then
        print_success "Deployment initiated successfully!"
        
        # Wait a moment for deployment to start
        sleep 5
        
        # Get deployment status
        print_status "Checking deployment status..."
        railway status
        
        # Get the deployment URL
        DEPLOYMENT_URL=$(railway domain)
        if [ -n "$DEPLOYMENT_URL" ]; then
            print_success "🚀 Application deployed to: $DEPLOYMENT_URL"
        else
            print_warning "Deployment URL not available yet. Check Railway dashboard."
        fi
        
    else
        print_error "Railway deployment failed!"
        exit 1
    fi
    
    # Clean up temporary Dockerfile
    rm -f Dockerfile
}

# Perform health check
health_check() {
    print_status "Performing health check..."
    
    DEPLOYMENT_URL=$(railway domain)
    if [ -z "$DEPLOYMENT_URL" ]; then
        print_warning "Cannot perform health check - deployment URL not available"
        return
    fi
    
    # Wait for deployment to be ready
    print_status "Waiting for deployment to be ready..."
    sleep 30
    
    # Health check with retry
    MAX_RETRIES=5
    RETRY_COUNT=0
    
    while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
        if curl -f -s "$DEPLOYMENT_URL/health" > /dev/null 2>&1; then
            print_success "✅ Health check passed!"
            print_success "🌐 Application is live at: $DEPLOYMENT_URL"
            return
        else
            RETRY_COUNT=$((RETRY_COUNT + 1))
            print_warning "Health check failed (attempt $RETRY_COUNT/$MAX_RETRIES)"
            if [ $RETRY_COUNT -lt $MAX_RETRIES ]; then
                sleep 10
            fi
        fi
    done
    
    print_warning "Health check failed after $MAX_RETRIES attempts"
    print_status "The application might still be starting up. Check manually at: $DEPLOYMENT_URL"
}

# Show deployment summary
show_summary() {
    echo ""
    print_success "🎉 Railway Deployment Complete!"
    echo ""
    echo -e "${BLUE}Deployment Summary:${NC}"
    echo "• Environment: $ENVIRONMENT"
    echo "• Project: $PROJECT_NAME"
    echo "• Timestamp: $(date)"
    
    DEPLOYMENT_URL=$(railway domain)
    if [ -n "$DEPLOYMENT_URL" ]; then
        echo "• URL: $DEPLOYMENT_URL"
        echo ""
        echo -e "${BLUE}Next Steps:${NC}"
        echo "• Visit your application: $DEPLOYMENT_URL"
        echo "• Check Railway dashboard: https://railway.app/dashboard"
        echo "• View logs: railway logs"
        echo "• Monitor performance: railway metrics"
    fi
    
    echo ""
    echo -e "${BLUE}Useful Railway Commands:${NC}"
    echo "• railway logs          - View application logs"
    echo "• railway status        - Check deployment status"
    echo "• railway metrics       - View performance metrics"
    echo "• railway variables     - Manage environment variables"
    echo "• railway rollback      - Rollback to previous version"
}

# Show help
show_help() {
    echo "Voice Input Assistant - Railway Deployment Script"
    echo ""
    echo "Usage: $0 [environment]"
    echo ""
    echo "Environments:"
    echo "  production   - Deploy to production environment (default)"
    echo "  staging      - Deploy to staging environment"
    echo "  development  - Deploy to development environment"
    echo ""
    echo "Prerequisites:"
    echo "  - Railway CLI installed (npm install -g @railway/cli)"
    echo "  - Logged into Railway (railway login)"
    echo "  - Project connected to Railway (railway link)"
    echo ""
    echo "Examples:"
    echo "  $0                     # Deploy to production"
    echo "  $0 production          # Deploy to production"
    echo "  $0 staging            # Deploy to staging"
    echo "  $0 development        # Deploy to development"
}

# Main deployment process
main() {
    echo ""
    print_status "Starting Railway deployment for environment: $ENVIRONMENT"
    echo ""
    
    check_railway_cli
    validate_project
    run_pre_checks
    set_environment_variables
    deploy_to_railway
    health_check
    show_summary
}

# Handle command line arguments
case "$1" in
    "help"|"--help"|"-h")
        show_help
        exit 0
        ;;
esac

# Run the main function
main