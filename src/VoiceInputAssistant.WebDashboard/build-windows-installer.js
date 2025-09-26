// Voice Input Assistant - Windows Installer Build Script
// Creates MSI and NSIS installers using electron-builder

const { build } = require('electron-builder');
const path = require('path');
const fs = require('fs');

// Configuration for Windows installer
const windowsConfig = {
  appId: 'com.voiceinputassistant.app',
  productName: 'Voice Input Assistant',
  directories: {
    app: '.',
    output: 'dist/installers'
  },
  files: [
    'build/**/*',
    'electron/**/*',
    'package.json',
    '!node_modules/**/*',
    '!src/**/*',
    '!public/**/*',
    '!**/*.ts',
    '!**/*.tsx',
    '!**/*.map'
  ],
  extraResources: [
    {
      from: 'electron/assets',
      to: 'assets',
      filter: ['**/*']
    }
  ],
  
  // Windows-specific configuration
  win: {
    target: [
      {
        target: 'nsis',
        arch: ['x64', 'ia32']
      },
      {
        target: 'msi',
        arch: ['x64', 'ia32']
      },
      {
        target: 'portable',
        arch: ['x64']
      }
    ],
    icon: 'electron/assets/icon.ico',
    publisherName: 'Voice Input Assistant Inc.',
    verifyUpdateCodeSignature: false,
    artifactName: '${productName}-${version}-${arch}-${ext}',
    requestedExecutionLevel: 'asInvoker',
    signAndEditExecutable: false
  },
  
  // NSIS installer configuration
  nsis: {
    oneClick: false,
    perMachine: false,
    allowElevation: true,
    allowToChangeInstallationDirectory: true,
    installerIcon: 'electron/assets/installer-icon.ico',
    uninstallerIcon: 'electron/assets/uninstaller-icon.ico',
    installerHeaderIcon: 'electron/assets/installer-header.ico',
    createDesktopShortcut: true,
    createStartMenuShortcut: true,
    shortcutName: 'Voice Input Assistant',
    include: 'electron/installer/installer-script.nsh',
    guid: '12345678-1234-1234-1234-123456789012',
    warningsAsErrors: false,
    displayLanguageSelector: false,
    installerLanguages: ['en_US'],
    language: '1033', // English
    multiLanguageInstaller: false,
    packElevateHelper: true,
    deleteAppDataOnUninstall: false,
    menuCategory: 'Productivity',
    runAfterFinish: true,
    artifactName: '${productName}-Setup-${version}.${ext}'
  },
  
  // MSI installer configuration
  msi: {
    oneClick: false,
    perMachine: false,
    runAfterFinish: true,
    createDesktopShortcut: true,
    createStartMenuShortcut: true,
    menuCategory: 'Productivity',
    artifactName: '${productName}-${version}-${arch}.${ext}',
    upgradeCode: '12345678-1234-1234-1234-123456789012'
  },
  
  // Portable app configuration
  portable: {
    artifactName: '${productName}-${version}-Portable.${ext}',
    requestExecutionLevel: 'user'
  },
  
  // Application metadata
  copyright: 'Copyright © 2024 Voice Input Assistant Inc.',
  description: 'AI-powered voice input and transcription assistant for Windows',
  homepage: 'https://voiceinputassistant.com',
  
  // Auto-updater configuration
  publish: {
    provider: 'github',
    owner: 'voice-input-assistant',
    repo: 'voice-input-assistant',
    private: false,
    releaseType: 'release'
  },
  
  // Compression
  compression: 'maximum',
  
  // File associations
  fileAssociations: [
    {
      ext: 'via',
      name: 'Voice Input Assistant Project',
      description: 'Voice Input Assistant project file',
      icon: 'electron/assets/file-icon.ico',
      role: 'Editor'
    }
  ],
  
  // Protocol associations
  protocols: [
    {
      name: 'Voice Input Assistant',
      schemes: ['voice-assistant']
    }
  ],
  
  // Electron configuration
  electronVersion: '27.0.0', // Use latest stable version
  nodeGypRebuild: false,
  npmRebuild: true,
  buildDependenciesFromSource: false
};

// Pre-build checks
async function preBuildChecks() {
  console.log('🔍 Running pre-build checks...');
  
  // Check if build directory exists
  if (!fs.existsSync('build')) {
    console.error('❌ Build directory not found. Run "npm run build" first.');
    process.exit(1);
  }
  
  // Check if electron main file exists
  if (!fs.existsSync('electron/main.js')) {
    console.error('❌ Electron main.js not found.');
    process.exit(1);
  }
  
  // Check if package.json has required fields
  const packageJson = JSON.parse(fs.readFileSync('package.json', 'utf8'));
  if (!packageJson.main) {
    console.error('❌ package.json missing "main" field.');
    process.exit(1);
  }
  
  console.log('✅ Pre-build checks passed');
}

// Create installer assets
async function createInstallerAssets() {
  console.log('📁 Creating installer assets...');
  
  const assetsDir = 'electron/assets';
  if (!fs.existsSync(assetsDir)) {
    fs.mkdirSync(assetsDir, { recursive: true });
  }
  
  const installerDir = 'electron/installer';
  if (!fs.existsSync(installerDir)) {
    fs.mkdirSync(installerDir, { recursive: true });
  }
  
  // Create NSIS installer script if it doesn't exist
  const nsisScript = path.join(installerDir, 'installer-script.nsh');
  if (!fs.existsSync(nsisScript)) {
    const nsisContent = `
; Voice Input Assistant Installer Script
!define PRODUCT_NAME "Voice Input Assistant"
!define PRODUCT_VERSION "\${VERSION}"
!define PRODUCT_PUBLISHER "Voice Input Assistant Inc."
!define PRODUCT_WEB_SITE "https://voiceinputassistant.com"

; Registry keys for uninstallation
!define PRODUCT_UNINST_KEY "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; Custom pages
!include "MUI2.nsh"

; Modern UI Configuration
!define MUI_ABORTWARNING
!define MUI_ICON "\${BUILD_RESOURCES_DIR}\\installer-icon.ico"
!define MUI_UNICON "\${BUILD_RESOURCES_DIR}\\uninstaller-icon.ico"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "\${BUILD_RESOURCES_DIR}\\installer-header.bmp"
!define MUI_WELCOMEFINISHPAGE_BITMAP "\${BUILD_RESOURCES_DIR}\\installer-welcome.bmp"

; Installer pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages  
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Languages
!insertmacro MUI_LANGUAGE "English"

; Custom install actions
Section -Post
  ; Create registry entries
  WriteRegStr HKLM "SOFTWARE\\\${PRODUCT_NAME}" "" $INSTDIR
  WriteRegStr \${PRODUCT_UNINST_ROOT_KEY} "\${PRODUCT_UNINST_KEY}" "DisplayName" "\${PRODUCT_NAME}"
  WriteRegStr \${PRODUCT_UNINST_ROOT_KEY} "\${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\\Uninstall.exe"
  WriteRegStr \${PRODUCT_UNINST_ROOT_KEY} "\${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\\Voice Input Assistant.exe"
  WriteRegStr \${PRODUCT_UNINST_ROOT_KEY} "\${PRODUCT_UNINST_KEY}" "DisplayVersion" "\${PRODUCT_VERSION}"
  WriteRegStr \${PRODUCT_UNINST_ROOT_KEY} "\${PRODUCT_UNINST_KEY}" "Publisher" "\${PRODUCT_PUBLISHER}"
  WriteRegStr \${PRODUCT_UNINST_ROOT_KEY} "\${PRODUCT_UNINST_KEY}" "URLInfoAbout" "\${PRODUCT_WEB_SITE}"
  
  ; Set up file associations
  WriteRegStr HKCR ".via" "" "VoiceInputAssistant.Document"
  WriteRegStr HKCR "VoiceInputAssistant.Document" "" "Voice Input Assistant Project"
  WriteRegStr HKCR "VoiceInputAssistant.Document\\shell\\open\\command" "" '"$INSTDIR\\Voice Input Assistant.exe" "%1"'
  
  ; Register URL protocol
  WriteRegStr HKCR "voice-assistant" "" "URL:Voice Input Assistant Protocol"
  WriteRegStr HKCR "voice-assistant" "URL Protocol" ""
  WriteRegStr HKCR "voice-assistant\\shell\\open\\command" "" '"$INSTDIR\\Voice Input Assistant.exe" "%1"'
SectionEnd

; Uninstaller
Section Uninstall
  ; Remove registry entries
  DeleteRegKey \${PRODUCT_UNINST_ROOT_KEY} "\${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "SOFTWARE\\\${PRODUCT_NAME}"
  DeleteRegKey HKCR ".via"
  DeleteRegKey HKCR "VoiceInputAssistant.Document"
  DeleteRegKey HKCR "voice-assistant"
  
  ; Remove files and directories
  Delete "$INSTDIR\\Uninstall.exe"
  RMDir /r "$INSTDIR"
  
  ; Remove shortcuts
  Delete "$DESKTOP\\Voice Input Assistant.lnk"
  Delete "$SMPROGRAMS\\Voice Input Assistant.lnk"
  Delete "$STARTMENU\\Programs\\Voice Input Assistant.lnk"
SectionEnd
`;
    
    fs.writeFileSync(nsisScript, nsisContent.trim());
  }
  
  console.log('✅ Installer assets ready');
}

// Build installer
async function buildInstaller(platform = 'win', arch = 'x64') {
  console.log(`🔨 Building ${platform} installer for ${arch}...`);
  
  try {
    await build({
      targets: Platform.WINDOWS.createTarget(),
      config: windowsConfig
    });
    
    console.log('🎉 Windows installer built successfully!');
    console.log('📦 Installer files available in: dist/installers/');
    
    // List generated files
    const installerDir = 'dist/installers';
    if (fs.existsSync(installerDir)) {
      const files = fs.readdirSync(installerDir);
      console.log('\n📋 Generated installer files:');
      files.forEach(file => {
        const filePath = path.join(installerDir, file);
        const stats = fs.statSync(filePath);
        const size = (stats.size / 1024 / 1024).toFixed(2) + ' MB';
        console.log(`  • ${file} (${size})`);
      });
    }
    
  } catch (error) {
    console.error('❌ Build failed:', error);
    process.exit(1);
  }
}

// Clean build artifacts
function cleanBuildArtifacts() {
  const dirsToClean = ['dist/installers', 'dist/electron'];
  
  dirsToClean.forEach(dir => {
    if (fs.existsSync(dir)) {
      fs.rmSync(dir, { recursive: true, force: true });
      console.log(`🧹 Cleaned: ${dir}`);
    }
  });
}

// Main build process
async function main() {
  const args = process.argv.slice(2);
  const command = args[0] || 'build';
  
  console.log('🚀 Voice Input Assistant - Windows Installer Builder');
  console.log('====================================================');
  
  try {
    switch (command) {
      case 'clean':
        cleanBuildArtifacts();
        break;
        
      case 'prepare':
        await createInstallerAssets();
        break;
        
      case 'build':
      default:
        await preBuildChecks();
        await createInstallerAssets();
        await buildInstaller();
        break;
    }
    
  } catch (error) {
    console.error('❌ Process failed:', error.message);
    process.exit(1);
  }
}

// Handle script execution
if (require.main === module) {
  main().catch(console.error);
}

module.exports = {
  windowsConfig,
  buildInstaller,
  createInstallerAssets,
  cleanBuildArtifacts
};