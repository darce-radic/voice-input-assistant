# Voice Input Assistant v${version}

> Revolutionary speech-to-text software that works anywhere on Windows with AI-powered post-processing.

## 🎉 What's New

${changelog}

## 📥 Downloads

### 🖥️ Desktop Application (Windows)

| Package Type | Download | Description |
|--------------|----------|-------------|
| **Portable** | [VoiceInputAssistant-${version}-portable.zip](${portable_download_url}) | No installation required - just extract and run |
| **MSIX Package** | [VoiceInputAssistant-${version}.msix](${msix_download_url}) | Signed package for Windows Store distribution |
| **Installer** | [VoiceInputAssistant-${version}-installer.zip](${installer_download_url}) | Self-contained installer with all dependencies |

### 🌐 Web Dashboard

The web dashboard can be deployed to your preferred hosting platform using the provided build artifacts or by cloning this repository.

- **Vercel/Netlify**: Use the `apps/web-dashboard` directory
- **Docker**: Build using the provided Dockerfile
- **Self-hosted**: Deploy the Next.js application to any Node.js environment

## 🚀 Quick Start

### Desktop Application

1. **Download** the portable version for your platform
2. **Extract** the ZIP file to your desired location
3. **Run** `VoiceInputAssistant.exe`
4. **Configure** your speech recognition preferences
5. **Start dictating** in any Windows application with F12 (default hotkey)

### First-Time Setup

1. Choose your preferred speech recognition engine:
   - **Local Whisper** (recommended for privacy)
   - **Azure Speech** (requires API key)
   - **OpenAI Whisper** (requires API key)
   
2. Set up your hotkeys and voice activation preferences
3. Configure per-application profiles as needed
4. Test the system in Notepad or your favorite text editor

## 🔧 System Requirements

### Minimum Requirements

- **OS**: Windows 10 version 1903 (build 18362) or later
- **RAM**: 4 GB (8 GB recommended)
- **Storage**: 2 GB free space (additional space needed for Whisper models)
- **Audio**: Microphone (built-in or external)
- **Network**: Internet connection for cloud features and updates

### Recommended for Best Performance

- **OS**: Windows 11
- **RAM**: 16 GB or more
- **CPU**: Modern multi-core processor (Intel i5/AMD Ryzen 5 or better)
- **Storage**: SSD with 10+ GB free space
- **Audio**: Quality external microphone or headset

## 🆕 Key Features in This Release

- ✅ **Multi-engine speech recognition** - Whisper, Azure, OpenAI, Google
- ✅ **AI-powered post-processing** - Grammar correction and tone adjustment
- ✅ **System-wide compatibility** - Works in any Windows application
- ✅ **Protected field detection** - Never inserts text into password fields
- ✅ **Per-application profiles** - Different settings for different apps
- ✅ **Offline-first architecture** - Local processing with optional cloud features
- ✅ **Voice activity detection** - Automatic start/stop based on speech
- ✅ **Custom vocabularies** - Add industry-specific terms and phrases
- ✅ **Usage analytics** - Track accuracy and performance metrics
- ✅ **Auto-update system** - Stay up-to-date with the latest improvements

## 🐛 Bug Fixes & Improvements

${bug_fixes}

## ⚠️ Breaking Changes

${breaking_changes}

## 📊 Performance & Statistics

This release includes significant performance improvements:

- ⚡ **${performance_improvement}%** faster speech processing
- 🎯 **${accuracy_improvement}%** improvement in recognition accuracy
- 💾 **${memory_improvement}%** reduction in memory usage
- 🔋 **${cpu_improvement}%** lower CPU utilization

## 🔒 Security Updates

${security_updates}

## 🛠️ Developer Information

### Build Information

- **Build Date**: ${build_date}
- **Commit**: [`${commit_sha}`](${commit_url})
- **Build Number**: ${build_number}
- **.NET Version**: 8.0
- **Target Framework**: net8.0-windows

### Dependencies

Major dependency versions in this release:
- Serilog: ${serilog_version}
- NAudio: ${naudio_version}
- MaterialDesignThemes: ${material_design_version}
- Newtonsoft.Json: ${json_version}

## 📝 Changelog

### Features
${features}

### Bug Fixes
${fixes}

### Documentation
${docs}

### Chores
${chores}

## 🙏 Acknowledgments

Thanks to all contributors who made this release possible:

${contributors}

Special thanks to the open source community and the following projects:
- OpenAI Whisper
- Microsoft Speech Services
- NAudio
- Serilog
- Material Design in XAML

## 📞 Support

- **Documentation**: [docs.voiceinputassistant.com](https://docs.voiceinputassistant.com)
- **Issues**: [GitHub Issues](https://github.com/yourusername/voice-input-assistant/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/voice-input-assistant/discussions)
- **Email**: support@voiceinputassistant.com

## 🗓️ What's Next

Check out our [roadmap](https://github.com/yourusername/voice-input-assistant/projects) for upcoming features:

- 🔮 Real-time translation
- 👥 Speaker diarization
- 📱 Mobile companion app
- 🏢 Enterprise features
- 🔗 API integrations

---

**Full Changelog**: [v${previous_version}...v${version}](${compare_url})