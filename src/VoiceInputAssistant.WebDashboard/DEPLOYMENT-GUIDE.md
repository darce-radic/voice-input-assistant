# 🚀 Voice Input Assistant - Complete Deployment Guide

This comprehensive guide covers deploying your Voice Input Assistant to Railway and building Windows installer packages.

## 📋 Table of Contents

1. [Railway Deployment](#railway-deployment)
2. [Windows App Installation Package](#windows-app-installation-package)
3. [Prerequisites](#prerequisites)
4. [Environment Setup](#environment-setup)
5. [Deployment Options](#deployment-options)
6. [Troubleshooting](#troubleshooting)

## 🚂 Railway Deployment

Railway is an excellent platform for deploying web applications with automatic HTTPS, custom domains, and seamless CI/CD integration.

### Prerequisites for Railway

```bash
# Install Railway CLI
npm install -g @railway/cli

# Login to Railway
railway login

# Link your project (run in project root)
railway link
```

### Quick Railway Deployment

```bash
# Deploy to production
npm run railway-deploy

# Deploy to staging  
npm run railway-deploy-staging

# Deploy specific environment
./deploy-railway.sh production
./deploy-railway.sh staging
./deploy-railway.sh development
```

### Railway Configuration

The project includes:

- **`railway.json`** - Railway service configuration
- **`Dockerfile.railway`** - Optimized Docker setup for Railway
- **`deploy-railway.sh`** - Automated deployment script

#### Railway Environment Variables

The deployment script automatically sets these variables:

```bash
NODE_ENV=production
REACT_APP_ENV=production
GENERATE_SOURCEMAP=false
REACT_APP_MOCK_API=false
REACT_APP_ENABLE_ANALYTICS=true
REACT_APP_BASE_URL=https://voice-input-assistant.up.railway.app
PORT=3000
```

#### Custom Domain Setup

1. **Add custom domain in Railway dashboard:**
   ```
   Domain: voiceinputassistant.com
   ```

2. **Update DNS records:**
   ```
   Type: CNAME
   Name: @
   Value: your-app.up.railway.app
   ```

3. **Update environment variables:**
   ```bash
   railway variables set REACT_APP_BASE_URL=https://voiceinputassistant.com
   railway variables set REACT_APP_DOMAIN=voiceinputassistant.com
   ```

### Railway Management Commands

```bash
# View deployment status
npm run railway-status
railway status

# View logs  
npm run railway-logs
railway logs

# Open app in browser
npm run railway-open
railway open

# Manage environment variables
railway variables
railway variables set KEY=value
railway variables delete KEY

# Rollback deployment
railway rollback

# View metrics
railway metrics
```

### Railway Advantages

✅ **Zero Configuration** - Deploy with just `railway up`  
✅ **Automatic HTTPS** - SSL certificates automatically provisioned  
✅ **Custom Domains** - Easy domain management  
✅ **Environment Management** - Built-in staging/production environments  
✅ **Database Add-ons** - PostgreSQL, Redis, MySQL available  
✅ **Affordable Pricing** - $5/month hobby plan, usage-based scaling  
✅ **Git Integration** - Automatic deploys from GitHub  

## 💻 Windows App Installation Package

Create professional Windows installers (.msi, .exe, portable) using Electron.

### Prerequisites for Windows App

```bash
# Install required dependencies
npm install --save-dev electron electron-builder
npm install --save-dev electron-is-dev electron-updater
npm install --save-dev electron-window-state electron-context-menu
npm install --save-dev concurrently wait-on

# For Windows development on non-Windows systems
npm install --save-dev wine  # macOS/Linux only
```

### Windows App Development

```bash
# Start React development server + Electron
npm run electron-dev

# Build React app and run Electron
npm run electron-build

# Run Electron with built React app
npm run electron
```

### Building Windows Installers

```bash
# Build all installer types (NSIS, MSI, Portable)
npm run dist

# Build specific installer types
npm run pack-win       # Development build
electron-builder --win # Production build

# Clean build artifacts
npm run dist-clean
```

#### Generated Installer Files

The build process creates:

- **`Voice-Input-Assistant-Setup-1.0.0.exe`** - NSIS installer (recommended)
- **`Voice-Input-Assistant-1.0.0-x64.msi`** - MSI installer (enterprise)
- **`Voice-Input-Assistant-1.0.0-Portable.exe`** - Portable version
- **`Voice-Input-Assistant-1.0.0-ia32.exe`** - 32-bit version

### Windows App Features

🖥️ **Native Desktop Experience**  
- System tray integration  
- Global keyboard shortcuts  
- File associations (.via files)  
- Protocol handlers (voice-assistant://)  
- Auto-updater support  
- Window state persistence  

⌨️ **Global Shortcuts**  
- `Ctrl+Shift+R` - Toggle recording  
- `Ctrl+Shift+S` - Stop recording  
- `Ctrl+Shift+O` - Open application  

🔗 **System Integration**  
- Windows Start Menu  
- Desktop shortcuts  
- File associations  
- URL protocol handling  
- Context menus  

### Auto-Updates

The Windows app includes automatic update functionality:

```javascript
// Auto-updater configuration in main.js
autoUpdater.checkForUpdatesAndNotify();

// Update events
autoUpdater.on('update-available', () => {
  // Notify user of available update
});

autoUpdater.on('update-downloaded', () => {
  // Prompt user to restart and install
});
```

## 🔧 Prerequisites

### System Requirements

**For Web Development:**
- Node.js >= 18.0.0
- npm >= 8.0.0
- Modern web browser

**For Windows App Development:**
- All web requirements plus:
- Windows 10/11 (or Wine for cross-platform)
- Visual Studio Build Tools (Windows)
- Python 3.x (for native modules)

**For Railway Deployment:**
- Railway CLI
- Docker (optional, for local testing)
- Git repository

### Development Tools Installation

```bash
# Install Node.js (if not installed)
# Download from: https://nodejs.org/

# Install Railway CLI globally
npm install -g @railway/cli

# Install project dependencies
npm install

# Install additional Electron dependencies
npm install --save-dev electron electron-builder

# For cross-platform builds (optional)
npm install --save-dev electron-builder-notarize  # macOS
npm install --save-dev wine                       # Linux/macOS
```

## ⚙️ Environment Setup

### 1. Environment Variables

Create environment files for different deployments:

**`.env.production`** (Railway production):
```env
NODE_ENV=production
REACT_APP_ENV=production
REACT_APP_BASE_URL=https://voiceinputassistant.com
REACT_APP_ENABLE_ANALYTICS=true
REACT_APP_MOCK_API=false
GENERATE_SOURCEMAP=false
```

**`.env.staging`** (Railway staging):
```env
NODE_ENV=production
REACT_APP_ENV=staging
REACT_APP_BASE_URL=https://staging-voiceinputassistant.up.railway.app
REACT_APP_DEBUG_MODE=true
GENERATE_SOURCEMAP=true
```

### 2. API Keys Configuration

Add your API keys to Railway environment variables:

```bash
# Speech recognition services
railway variables set REACT_APP_GOOGLE_SPEECH_API_KEY=your_key
railway variables set REACT_APP_AZURE_SPEECH_KEY=your_key
railway variables set REACT_APP_OPENAI_API_KEY=your_key

# Analytics and monitoring
railway variables set REACT_APP_GA_TRACKING_ID=your_tracking_id
railway variables set REACT_APP_SENTRY_DSN=your_sentry_dsn

# Push notifications
railway variables set REACT_APP_VAPID_PUBLIC_KEY=your_vapid_key
railway variables set VAPID_PRIVATE_KEY=your_vapid_private_key
```

### 3. Domain Configuration

For production deployment with custom domain:

```bash
# Set custom domain variables
railway variables set REACT_APP_DOMAIN=voiceinputassistant.com
railway variables set REACT_APP_BASE_URL=https://voiceinputassistant.com

# Update CORS origins
railway variables set REACT_APP_CORS_ORIGIN=https://voiceinputassistant.com,https://www.voiceinputassistant.com
```

## 🎯 Deployment Options

### Option 1: Railway (Recommended for Web)

**Advantages:**
- Simple deployment (`railway up`)
- Automatic HTTPS and domain management
- Built-in CI/CD with GitHub integration
- Database add-ons available
- Affordable pricing ($5/month hobby plan)
- Zero server management

**Best for:**
- Web-first deployment
- Rapid prototyping
- Small to medium applications
- Teams wanting minimal DevOps overhead

**Deployment Steps:**
```bash
# 1. Setup Railway
railway login
railway link

# 2. Deploy
npm run railway-deploy

# 3. Configure domain (optional)
# Set custom domain in Railway dashboard
# Update DNS records
```

### Option 2: Traditional Cloud (AWS/Azure/GCP)

**Use the existing deployment scripts:**
```bash
# Deploy to AWS S3 + CloudFront
./deploy.sh production

# Deploy to staging
./deploy.sh staging
```

### Option 3: Docker Deployment

**Use Docker Compose:**
```bash
# Start full stack locally
docker-compose --profile development up

# Deploy to production
docker-compose --profile production up -d
```

### Option 4: Windows Desktop App

**For enterprise distribution:**
```bash
# Build all installer types
npm run dist

# Distribute via:
# - Microsoft Store (requires certification)
# - Enterprise deployment (MSI files)
# - Direct download (NSIS installers)
# - Portable versions
```

## 🔍 Troubleshooting

### Railway Deployment Issues

**Problem: Build fails with "out of memory"**
```bash
# Solution: Increase build resources
railway variables set NODE_OPTIONS="--max-old-space-size=4096"
```

**Problem: Environment variables not loaded**
```bash
# Check variables are set
railway variables

# Restart service
railway up --detach
```

**Problem: Custom domain not working**
```bash
# Verify DNS configuration
nslookup voiceinputassistant.com

# Check Railway domain settings
railway domains
```

### Windows App Build Issues

**Problem: "python not found" error**
```bash
# Install Python and Visual Studio Build Tools
# Or use npm config to specify python path
npm config set python /path/to/python
```

**Problem: "electron-builder" fails**
```bash
# Clear node_modules and reinstall
npm run clean-all
npm install

# Install Windows SDK (Windows only)
npm install --global windows-build-tools
```

**Problem: Code signing errors**
```bash
# Disable code signing for development
# In package.json build.win section:
"win": {
  "verifyUpdateCodeSignature": false,
  "signAndEditExecutable": false
}
```

### General Issues

**Problem: Port conflicts**
```bash
# Kill processes using port 3000
npx kill-port 3000

# Use different port
PORT=3001 npm start
```

**Problem: HTTPS issues in development**
```bash
# Generate self-signed certificates
npm run generate-certs

# Or use Railway's built-in HTTPS
railway open
```

**Problem: Service worker not updating**
```bash
# Clear service worker cache
# In browser dev tools: Application > Storage > Clear storage

# Force service worker update
# In browser dev tools: Application > Service Workers > Update
```

## 🚀 Production Deployment Checklist

### Pre-Deployment

- [ ] Run all tests: `npm test`
- [ ] Type checking: `npm run type-check`  
- [ ] Linting: `npm run lint`
- [ ] Build locally: `npm run build`
- [ ] Test PWA: `npm run pwa-test`

### Railway Deployment

- [ ] Environment variables configured
- [ ] Custom domain setup (if applicable)  
- [ ] SSL certificate verified
- [ ] Health checks passing
- [ ] Performance monitoring enabled

### Windows App

- [ ] Electron app tested locally
- [ ] All installer types built
- [ ] Auto-updater configured
- [ ] Code signing setup (production)
- [ ] Distribution channels prepared

### Post-Deployment

- [ ] Health checks passing
- [ ] Analytics tracking working
- [ ] Error monitoring active
- [ ] Performance metrics baseline
- [ ] User feedback collection enabled
- [ ] Documentation updated

---

## 📞 Support

- **Railway Issues**: [Railway Discord](https://discord.gg/railway)
- **Electron Issues**: [Electron GitHub](https://github.com/electron/electron/issues)
- **Project Issues**: Create an issue in your repository

**Happy Deploying! 🚀**