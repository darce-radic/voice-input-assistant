#!/bin/bash

# Voice Input Assistant - Installation Script
# This script sets up the development environment and installs all dependencies

echo "🎤 Voice Input Assistant - Setup Script"
echo "======================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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

# Check if Node.js is installed
check_node() {
    print_status "Checking Node.js installation..."
    
    if command -v node &> /dev/null; then
        NODE_VERSION=$(node --version)
        print_success "Node.js is installed: $NODE_VERSION"
        
        # Check if version is >= 18
        NODE_MAJOR_VERSION=$(echo $NODE_VERSION | cut -d'.' -f1 | sed 's/v//')
        if [ "$NODE_MAJOR_VERSION" -lt 18 ]; then
            print_warning "Node.js version should be >= 18.0.0 for best compatibility"
        fi
    else
        print_error "Node.js is not installed. Please install Node.js >= 18.0.0"
        exit 1
    fi
}

# Check if npm is installed
check_npm() {
    print_status "Checking npm installation..."
    
    if command -v npm &> /dev/null; then
        NPM_VERSION=$(npm --version)
        print_success "npm is installed: v$NPM_VERSION"
    else
        print_error "npm is not installed. Please install npm"
        exit 1
    fi
}

# Install dependencies
install_dependencies() {
    print_status "Installing dependencies..."
    
    # Clear npm cache to avoid conflicts
    print_status "Clearing npm cache..."
    npm cache clean --force
    
    # Install dependencies
    print_status "Installing production and development dependencies..."
    npm install
    
    if [ $? -eq 0 ]; then
        print_success "Dependencies installed successfully"
    else
        print_error "Failed to install dependencies"
        exit 1
    fi
}

# Setup environment files
setup_environment() {
    print_status "Setting up environment files..."
    
    if [ ! -f ".env" ]; then
        if [ -f ".env.example" ]; then
            cp .env.example .env
            print_success "Created .env file from .env.example"
            print_warning "Please update .env file with your actual API keys"
        else
            print_warning ".env.example not found, creating basic .env file"
            cat > .env << EOF
NODE_ENV=development
REACT_APP_ENV=development
GENERATE_SOURCEMAP=true
HTTPS=false
REACT_APP_MOCK_API=true
REACT_APP_DEBUG_MODE=true
EOF
            print_success "Created basic .env file"
        fi
    else
        print_success ".env file already exists"
    fi
}

# Create necessary directories
create_directories() {
    print_status "Creating necessary directories..."
    
    mkdir -p public/icons
    mkdir -p public/screenshots
    mkdir -p src/assets
    mkdir -p src/types
    mkdir -p src/hooks
    mkdir -p src/contexts
    mkdir -p src/utils
    
    print_success "Directory structure created"
}

# Generate VAPID keys for push notifications
generate_vapid_keys() {
    print_status "Generating VAPID keys for push notifications..."
    
    if command -v npx &> /dev/null; then
        # Install web-push if not already installed
        npm list web-push &> /dev/null
        if [ $? -ne 0 ]; then
            print_status "Installing web-push package..."
            npm install --save-dev web-push
        fi
        
        print_status "Generating VAPID keys..."
        npx web-push generate-vapid-keys > vapid-keys.txt 2>&1
        
        if [ -f "vapid-keys.txt" ]; then
            print_success "VAPID keys generated and saved to vapid-keys.txt"
            print_warning "Please add these keys to your .env file"
            cat vapid-keys.txt
        else
            print_warning "Could not generate VAPID keys automatically"
            print_status "You can generate them later with: npx web-push generate-vapid-keys"
        fi
    else
        print_warning "npx not available, skipping VAPID key generation"
    fi
}

# Setup Git hooks (optional)
setup_git_hooks() {
    if [ -d ".git" ]; then
        print_status "Setting up Git pre-commit hooks..."
        
        cat > .git/hooks/pre-commit << 'EOF'
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
EOF
        
        chmod +x .git/hooks/pre-commit
        print_success "Git pre-commit hooks setup"
    else
        print_status "Not a Git repository, skipping Git hooks setup"
    fi
}

# Verify installation
verify_installation() {
    print_status "Verifying installation..."
    
    # Check if critical files exist
    critical_files=("package.json" "tsconfig.json" "craco.config.js" ".env")
    
    for file in "${critical_files[@]}"; do
        if [ -f "$file" ]; then
            print_success "✓ $file exists"
        else
            print_error "✗ $file is missing"
        fi
    done
    
    # Try to run type checking
    print_status "Running type check test..."
    npm run type-check
    
    if [ $? -eq 0 ]; then
        print_success "Type checking passed"
    else
        print_warning "Type checking failed - this may be normal if source files are missing"
    fi
}

# Main installation process
main() {
    echo ""
    print_status "Starting Voice Input Assistant setup..."
    echo ""
    
    check_node
    check_npm
    create_directories
    setup_environment
    install_dependencies
    generate_vapid_keys
    setup_git_hooks
    verify_installation
    
    echo ""
    print_success "🎉 Setup completed successfully!"
    echo ""
    echo -e "${BLUE}Next steps:${NC}"
    echo "1. Update your .env file with actual API keys"
    echo "2. Run 'npm start' to start the development server"
    echo "3. Run 'npm run https' for HTTPS development (required for PWA features)"
    echo "4. Run 'npm run build' to create a production build"
    echo "5. Run 'npm run pwa-test' to test PWA functionality"
    echo ""
    echo -e "${BLUE}Available scripts:${NC}"
    echo "• npm start          - Start development server"
    echo "• npm run https      - Start HTTPS development server"
    echo "• npm run build      - Create production build"
    echo "• npm run test       - Run tests"
    echo "• npm run lint       - Run ESLint"
    echo "• npm run type-check - Run TypeScript type checking"
    echo "• npm run pwa-test   - Build and serve for PWA testing"
    echo ""
    print_warning "Remember to configure your API keys in the .env file!"
}

# Run the main function
main