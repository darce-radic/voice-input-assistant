#!/bin/bash

# Voice Input Assistant - Deployment Script
# This script builds the application for production and handles deployment

echo "🚀 Voice Input Assistant - Deployment Script"
echo "============================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
DEPLOY_TARGET=${1:-"staging"}
BUILD_DIR="build"
BACKUP_DIR="deployment-backup-$(date +%Y%m%d-%H%M%S)"

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

# Function to check if we're in the right directory
check_project_root() {
    if [ ! -f "package.json" ] || [ ! -f "tsconfig.json" ]; then
        print_error "This doesn't appear to be the project root directory"
        print_error "Please run this script from the Voice Input Assistant project root"
        exit 1
    fi
    
    print_success "Confirmed project root directory"
}

# Function to check Node.js and npm versions
check_environment() {
    print_status "Checking environment..."
    
    # Check Node.js
    if ! command -v node &> /dev/null; then
        print_error "Node.js is not installed"
        exit 1
    fi
    
    NODE_VERSION=$(node --version)
    print_success "Node.js version: $NODE_VERSION"
    
    # Check npm
    if ! command -v npm &> /dev/null; then
        print_error "npm is not installed"
        exit 1
    fi
    
    NPM_VERSION=$(npm --version)
    print_success "npm version: v$NPM_VERSION"
}

# Function to run pre-deployment checks
run_pre_checks() {
    print_status "Running pre-deployment checks..."
    
    # Check if .env exists
    if [ ! -f ".env" ]; then
        print_warning ".env file not found - using default configuration"
    fi
    
    # Run TypeScript type checking
    print_status "Running TypeScript type checking..."
    npm run type-check
    if [ $? -ne 0 ]; then
        print_error "TypeScript type checking failed"
        read -p "Do you want to continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    else
        print_success "TypeScript type checking passed"
    fi
    
    # Run ESLint
    print_status "Running ESLint..."
    npm run lint
    if [ $? -ne 0 ]; then
        print_warning "ESLint found issues"
        read -p "Do you want to continue anyway? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    else
        print_success "ESLint checks passed"
    fi
    
    # Run tests if available
    if npm run | grep -q "test"; then
        print_status "Running tests..."
        npm test -- --watchAll=false --coverage=false
        if [ $? -ne 0 ]; then
            print_warning "Tests failed"
            read -p "Do you want to continue anyway? (y/N): " -n 1 -r
            echo
            if [[ ! $REPLY =~ ^[Yy]$ ]]; then
                exit 1
            fi
        else
            print_success "All tests passed"
        fi
    fi
}

# Function to backup existing build
backup_existing_build() {
    if [ -d "$BUILD_DIR" ]; then
        print_status "Backing up existing build..."
        mkdir -p "$BACKUP_DIR"
        cp -r "$BUILD_DIR" "$BACKUP_DIR/"
        print_success "Backup created at $BACKUP_DIR"
    fi
}

# Function to set environment for deployment target
set_deployment_environment() {
    print_status "Setting environment for deployment target: $DEPLOY_TARGET"
    
    case $DEPLOY_TARGET in
        "production")
            export NODE_ENV=production
            export REACT_APP_ENV=production
            export GENERATE_SOURCEMAP=false
            print_status "Production environment configured"
            ;;
        "staging")
            export NODE_ENV=production
            export REACT_APP_ENV=staging
            export GENERATE_SOURCEMAP=true
            print_status "Staging environment configured"
            ;;
        "test")
            export NODE_ENV=production
            export REACT_APP_ENV=test
            export GENERATE_SOURCEMAP=true
            print_status "Test environment configured"
            ;;
        *)
            print_warning "Unknown deployment target: $DEPLOY_TARGET"
            print_warning "Using staging configuration"
            export NODE_ENV=production
            export REACT_APP_ENV=staging
            export GENERATE_SOURCEMAP=true
            ;;
    esac
}

# Function to build the application
build_application() {
    print_status "Building application for $DEPLOY_TARGET..."
    
    # Clean previous build
    if [ -d "$BUILD_DIR" ]; then
        rm -rf "$BUILD_DIR"
        print_status "Cleaned previous build directory"
    fi
    
    # Build the application
    npm run build
    
    if [ $? -eq 0 ]; then
        print_success "Build completed successfully"
    else
        print_error "Build failed"
        
        # Restore backup if exists
        if [ -d "$BACKUP_DIR/$BUILD_DIR" ]; then
            print_status "Restoring backup..."
            cp -r "$BACKUP_DIR/$BUILD_DIR" .
            print_success "Backup restored"
        fi
        
        exit 1
    fi
}

# Function to analyze build
analyze_build() {
    print_status "Analyzing build..."
    
    if [ -d "$BUILD_DIR" ]; then
        BUILD_SIZE=$(du -sh $BUILD_DIR | cut -f1)
        print_success "Build size: $BUILD_SIZE"
        
        # Check for large files
        print_status "Checking for large files (>1MB)..."
        find $BUILD_DIR -type f -size +1M -exec ls -lh {} \; | awk '{print $5, $9}' | while read size file; do
            print_warning "Large file: $file ($size)"
        done
        
        # Check service worker
        if [ -f "$BUILD_DIR/sw.js" ]; then
            print_success "Service worker found"
        else
            print_warning "Service worker not found"
        fi
        
        # Check manifest
        if [ -f "$BUILD_DIR/manifest.json" ]; then
            print_success "PWA manifest found"
        else
            print_warning "PWA manifest not found"
        fi
        
        # List build contents
        print_status "Build contents:"
        ls -la $BUILD_DIR
    else
        print_error "Build directory not found"
        exit 1
    fi
}

# Function to run post-build tests
run_post_build_tests() {
    print_status "Running post-build tests..."
    
    # Test if built files can be served
    if command -v python3 &> /dev/null; then
        print_status "Testing build with Python HTTP server..."
        cd $BUILD_DIR
        timeout 10s python3 -m http.server 8000 &
        SERVER_PID=$!
        cd ..
        
        sleep 2
        
        if curl -f http://localhost:8000 > /dev/null 2>&1; then
            print_success "Build serves correctly"
        else
            print_warning "Could not verify build serving"
        fi
        
        kill $SERVER_PID 2>/dev/null
    fi
    
    # Check critical files
    critical_files=("index.html" "static/js" "static/css")
    for file in "${critical_files[@]}"; do
        if [ -e "$BUILD_DIR/$file" ]; then
            print_success "✓ $file exists"
        else
            print_error "✗ Critical file missing: $file"
        fi
    done
}

# Function to create deployment package
create_deployment_package() {
    print_status "Creating deployment package..."
    
    PACKAGE_NAME="voice-assistant-$DEPLOY_TARGET-$(date +%Y%m%d-%H%M%S).tar.gz"
    
    # Include build directory and necessary config files
    tar -czf $PACKAGE_NAME $BUILD_DIR package.json public/manifest.json public/sw.js 2>/dev/null || tar -czf $PACKAGE_NAME $BUILD_DIR package.json
    
    if [ -f "$PACKAGE_NAME" ]; then
        PACKAGE_SIZE=$(du -sh $PACKAGE_NAME | cut -f1)
        print_success "Deployment package created: $PACKAGE_NAME ($PACKAGE_SIZE)"
    else
        print_error "Failed to create deployment package"
        exit 1
    fi
}

# Function to deploy based on target
deploy_to_target() {
    print_status "Deploying to $DEPLOY_TARGET..."
    
    case $DEPLOY_TARGET in
        "production")
            deploy_production
            ;;
        "staging")
            deploy_staging
            ;;
        "test")
            deploy_test
            ;;
        *)
            print_warning "No specific deployment configured for target: $DEPLOY_TARGET"
            print_status "Build is ready for manual deployment"
            ;;
    esac
}

# Function to deploy to production
deploy_production() {
    print_status "Deploying to production environment..."
    
    # Example: Upload to your production server
    # You would replace this with your actual deployment logic
    print_warning "Production deployment not configured"
    print_status "Please upload the contents of '$BUILD_DIR' to your production server"
    print_status "Or configure this script with your specific deployment commands"
    
    # Example configurations you might use:
    # - rsync to server: rsync -avz $BUILD_DIR/ user@server:/var/www/html/
    # - AWS S3: aws s3 sync $BUILD_DIR s3://your-bucket-name
    # - FTP: lftp -c "mirror -R $BUILD_DIR ftp://your-server"
    # - Docker: docker build -t voice-assistant . && docker push
}

# Function to deploy to staging
deploy_staging() {
    print_status "Deploying to staging environment..."
    
    # Example staging deployment
    print_warning "Staging deployment not configured"
    print_status "Please upload the contents of '$BUILD_DIR' to your staging server"
    
    # You might also want to:
    # - Upload to a staging S3 bucket
    # - Deploy to a staging Kubernetes namespace
    # - Use a different subdomain
}

# Function to deploy to test environment
deploy_test() {
    print_status "Deploying to test environment..."
    
    # For test environment, we might just run a local server
    if command -v npx &> /dev/null && npm list -g serve &> /dev/null || npm list serve &> /dev/null; then
        print_status "Starting test server..."
        print_success "Test deployment available at: http://localhost:3000"
        print_status "Press Ctrl+C to stop the server"
        npx serve -s $BUILD_DIR -l 3000
    else
        print_status "Installing serve package for test deployment..."
        npm install -g serve
        npx serve -s $BUILD_DIR -l 3000
    fi
}

# Function to cleanup
cleanup() {
    print_status "Cleaning up..."
    
    # Remove backup if deployment was successful
    if [ -d "$BACKUP_DIR" ]; then
        print_status "Removing backup directory..."
        rm -rf "$BACKUP_DIR"
        print_success "Cleanup completed"
    fi
}

# Function to show deployment summary
show_summary() {
    echo ""
    print_success "🎉 Deployment completed successfully!"
    echo ""
    echo -e "${BLUE}Deployment Summary:${NC}"
    echo "• Target: $DEPLOY_TARGET"
    echo "• Build directory: $BUILD_DIR"
    echo "• Build size: $(du -sh $BUILD_DIR 2>/dev/null | cut -f1 || echo 'Unknown')"
    echo "• Timestamp: $(date)"
    echo ""
    
    case $DEPLOY_TARGET in
        "production")
            echo -e "${BLUE}Production deployment:${NC}"
            echo "• Upload '$BUILD_DIR' contents to your production server"
            echo "• Update your web server configuration if needed"
            echo "• Test the deployed application"
            ;;
        "staging")
            echo -e "${BLUE}Staging deployment:${NC}"
            echo "• Upload '$BUILD_DIR' contents to your staging server"
            echo "• Share staging URL for testing and review"
            ;;
        "test")
            echo -e "${BLUE}Test deployment:${NC}"
            echo "• Test server should be running at http://localhost:3000"
            echo "• Test PWA functionality and all features"
            ;;
    esac
}

# Main deployment process
main() {
    echo ""
    print_status "Starting deployment process for target: $DEPLOY_TARGET"
    echo ""
    
    # Validate deployment target
    if [[ ! "$DEPLOY_TARGET" =~ ^(production|staging|test)$ ]]; then
        print_warning "Unknown deployment target: $DEPLOY_TARGET"
        print_status "Valid targets: production, staging, test"
    fi
    
    check_project_root
    check_environment
    run_pre_checks
    backup_existing_build
    set_deployment_environment
    build_application
    analyze_build
    run_post_build_tests
    create_deployment_package
    deploy_to_target
    
    if [[ "$DEPLOY_TARGET" != "test" ]]; then
        cleanup
    fi
    
    show_summary
}

# Handle command line arguments
case "$1" in
    "help"|"--help"|"-h")
        echo "Voice Input Assistant Deployment Script"
        echo ""
        echo "Usage: $0 [target]"
        echo ""
        echo "Targets:"
        echo "  production  - Deploy to production environment"
        echo "  staging     - Deploy to staging environment (default)"
        echo "  test        - Build and run local test server"
        echo ""
        echo "Examples:"
        echo "  $0                    # Deploy to staging"
        echo "  $0 production         # Deploy to production"
        echo "  $0 test               # Build and test locally"
        exit 0
        ;;
esac

# Run the main function
main