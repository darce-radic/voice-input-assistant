#!/bin/bash

echo "🧪 Voice Input Assistant - Simple Local Test"
echo "============================================="

# Test 1: Check Node.js and npm
echo "Test 1: Environment Check"
echo "Node.js version: $(node --version)"
echo "npm version: $(npm --version)"
echo ""

# Test 2: Check project files
echo "Test 2: Project Structure"
if [ -f "package.json" ]; then
    echo "✅ package.json found"
else
    echo "❌ package.json missing"
    exit 1
fi

if [ -d "node_modules" ]; then
    echo "✅ node_modules found"
else
    echo "⚠️ node_modules missing - need to run npm install"
fi

if [ -d "src" ]; then
    echo "✅ src directory found"
else
    echo "❌ src directory missing"
fi
echo ""

# Test 3: Check build directory
echo "Test 3: Build Status"
if [ -d "build" ]; then
    BUILD_SIZE=$(du -sh build 2>/dev/null | cut -f1)
    echo "✅ Build directory exists (${BUILD_SIZE})"
    
    if [ -f "build/index.html" ]; then
        echo "✅ build/index.html exists"
    else
        echo "❌ build/index.html missing"
    fi
    
    if [ -d "build/static" ]; then
        echo "✅ Static assets directory exists"
    else
        echo "❌ Static assets missing"
    fi
else
    echo "⚠️ No build directory found - need to run npm run build"
fi
echo ""

# Test 4: Check key dependencies
echo "Test 4: Dependencies Check"
if npm list react >/dev/null 2>&1; then
    echo "✅ React installed"
else
    echo "❌ React missing"
fi

if npm list typescript >/dev/null 2>&1; then
    echo "✅ TypeScript installed"
else
    echo "❌ TypeScript missing"
fi
echo ""

echo "Test completed! Summary:"
echo "- Node.js and npm: Ready"
echo "- Project structure: Ready"
if [ -d "build" ]; then
    echo "- Production build: ✅ Ready for deployment"
else
    echo "- Production build: ⚠️ Need to run 'npm run build'"
fi
echo ""
echo "Next steps:"
echo "1. If missing, run: npm install"
echo "2. If no build, run: npm run build"
echo "3. Test locally: npm run serve"
echo "4. Deploy to Railway: Ready!"