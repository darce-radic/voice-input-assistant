# Voice Input Assistant 🎤

> Revolutionary speech-to-text software that works anywhere on Windows with AI-powered post-processing.

[![Build Status](https://github.com/yourusername/voice-input-assistant/workflows/CI/badge.svg)](https://github.com/yourusername/voice-input-assistant/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Next.js](https://img.shields.io/badge/Next.js-13.4-000000)](https://nextjs.org/)

## 🚀 Quick Start

### Prerequisites

- **Windows 10/11** (for desktop app)
- **Node.js 18+** ([Download](https://nodejs.org/))
- **NET 8 SDK** ([Download](https://dotnet.microsoft.com/))
- **Git** ([Download](https://git-scm.com/))

### One-Command Setup

```bash
# Clone and setup the entire project
git clone https://github.com/yourusername/voice-input-assistant.git
cd voice-input-assistant
setup.bat
```

### Manual Setup

```bash
# 1. Install root dependencies
npm install

# 2. Setup desktop application
cd apps/desktop
dotnet restore
cd ../..

# 3. Setup web dashboard
cd apps/web-dashboard
npm install
cd ../..

# 4. Build shared packages
cd packages/common
npm install && npm run build
cd ../..
```

### Development

```bash
# Start all development servers
npm run dev

# Or start individual components
npm run desktop:dev    # Windows desktop app
npm run web:dev        # Web dashboard (http://localhost:3001)
```

## 🏗️ Project Structure

```
voice-input-assistant/
├── apps/
│   ├── desktop/              # WPF Desktop Application (.NET 8)
│   ├── web-dashboard/        # Next.js Web Dashboard
│   └── marketing/           # Marketing Website
├── packages/
│   ├── common/              # Shared TypeScript types & utilities
│   ├── proto/               # Protocol buffer definitions
│   └── ci-scripts/          # CI/CD utilities
├── docs/                    # Documentation
├── .github/                 # GitHub workflows
└── tools/                   # Development tools
```

## ✨ Features

### 🎯 Core Features
- **System-wide speech recognition** - Works in any Windows application
- **Multiple STT engines** - Whisper (local), Azure, OpenAI, Google
- **AI-powered post-processing** - Grammar, tone, clarity improvements
- **Protected field detection** - Prevents text injection into password fields
- **Per-app profiles** - Customized settings for different applications
- **Offline-first architecture** - Local processing with optional cloud features

### 🔐 Security & Privacy
- **Local processing by default** - Your voice stays on your device
- **End-to-end encryption** - When using cloud features
- **GDPR/CCPA compliant** - Complete data control
- **HIPAA-ready** - Enterprise security features
- **Protected field detection** - Never inserts into password fields

### 🌟 Advanced Features
- **Voice Activity Detection** - Automatic start/stop based on speech
- **Custom vocabularies** - Industry-specific terms and phrases
- **Team collaboration** - Shared dictionaries and settings
- **Usage analytics** - Detailed insights and reporting
- **Multi-language support** - 50+ languages supported

## 🛠️ Development

### Available Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Start all development servers |
| `npm run build` | Build all projects |
| `npm run test` | Run all tests |
| `npm run lint` | Lint all code |
| `npm run type-check` | TypeScript type checking |
| `npm run desktop:dev` | Start desktop app only |
| `npm run web:dev` | Start web dashboard only |

### Desktop App Development

```bash
cd apps/desktop

# Run the application
dotnet run

# Build for release
dotnet publish -c Release -r win-x64 --self-contained false

# Run tests
dotnet test
```

### Web Dashboard Development

```bash
cd apps/web-dashboard

# Start development server
npm run dev

# Build for production
npm run build

# Run tests
npm run test
```

## 🔧 Configuration

### Environment Variables

Create `.env.local` files in each app directory:

**Desktop App (`apps/desktop/.env`)**
```env
# Speech Recognition APIs (optional)
AZURE_SPEECH_KEY=your_azure_key
AZURE_SPEECH_REGION=eastus
OPENAI_API_KEY=your_openai_key
GOOGLE_CLOUD_KEY_FILE=path/to/service-account.json

# Application Settings
VOICE_INPUT_LOG_LEVEL=Information
VOICE_INPUT_DATA_PATH=C:\Users\{username}\AppData\Roaming\VoiceInputAssistant
```

**Web Dashboard (`apps/web-dashboard/.env.local`)**
```env
# Database
DATABASE_URL=postgresql://user:pass@localhost:5432/voiceinput

# Authentication
NEXTAUTH_URL=http://localhost:3001
NEXTAUTH_SECRET=your_secret_here
SUPABASE_URL=your_supabase_url
SUPABASE_ANON_KEY=your_supabase_key

# Stripe (for billing)
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_SECRET_KEY=sk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
```

### Speech Engine Setup

#### Local Engines (No API keys required)
- **Whisper Local** - Automatically downloads models on first use
- **Windows Speech** - Uses built-in Windows Speech Recognition

#### Cloud Engines (API keys required)
- **Azure Speech** - Sign up at [Azure Cognitive Services](https://azure.microsoft.com/services/cognitive-services/speech-services/)
- **OpenAI Whisper** - Get API key from [OpenAI](https://openai.com/api/)
- **Google Cloud** - Setup at [Google Cloud Speech-to-Text](https://cloud.google.com/speech-to-text)

## 📊 Usage

### First Time Setup

1. **Launch the desktop app** - It will appear in your system tray
2. **Configure your preferences** - Right-click tray icon → Settings
3. **Choose speech engine** - Local (free) or cloud (requires API key)
4. **Set up hotkeys** - Default: F12 for push-to-talk
5. **Test in any app** - Try dictating in Notepad

### Basic Usage

1. **Push-to-talk** - Hold F12 and speak
2. **Voice activation** - Enable VAD for hands-free operation
3. **Per-app settings** - Different engines/tones for different apps
4. **History & search** - Review and reuse previous transcriptions

### Web Dashboard

- **Analytics** - View usage statistics and accuracy metrics
- **Billing** - Manage subscription and view invoices
- **Team management** - Share vocabularies and settings
- **Data export** - Download your transcription history

## 🧪 Testing

### Running Tests

```bash
# All tests
npm run test

# Desktop app tests
cd apps/desktop && dotnet test

# Web dashboard tests
cd apps/web-dashboard && npm run test

# E2E tests
npm run test:e2e
```

### Test Coverage

- **Unit tests** - 90%+ coverage required
- **Integration tests** - API and database operations
- **E2E tests** - Complete user workflows
- **Performance tests** - Load testing for cloud APIs

## 📚 Documentation

- [Architecture Overview](docs/architecture.md)
- [API Reference](docs/api-reference.md)
- [Security Guide](docs/security-compliance.md)
- [Testing Strategy](docs/testing-strategy.md)
- [Deployment Guide](docs/deployment.md)
- [Contributing Guidelines](CONTRIBUTING.md)

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Workflow

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes and add tests
4. Run the test suite: `npm run test`
5. Commit your changes: `git commit -m 'Add amazing feature'`
6. Push to the branch: `git push origin feature/amazing-feature`
7. Open a Pull Request

## 📈 Roadmap

### v0.2.0 - Enhanced Recognition
- [ ] Diarization (speaker separation)
- [ ] Real-time translation
- [ ] Custom model fine-tuning
- [ ] Mobile companion app

### v0.3.0 - Enterprise Features
- [ ] SSO/SAML integration
- [ ] Advanced admin controls
- [ ] Audit logging
- [ ] SOC2 compliance

### v1.0.0 - Production Ready
- [ ] Performance optimizations
- [ ] Advanced AI features
- [ ] Multi-platform support
- [ ] Professional services

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation** - Check our [docs](docs/) directory
- **Issues** - Report bugs on [GitHub Issues](https://github.com/yourusername/voice-input-assistant/issues)
- **Discussions** - Join the conversation on [GitHub Discussions](https://github.com/yourusername/voice-input-assistant/discussions)
- **Enterprise** - Contact us for enterprise support and custom solutions

## 🙏 Acknowledgments

- **OpenAI Whisper** - State-of-the-art speech recognition
- **Microsoft** - Azure Speech Services and .NET platform
- **Vercel** - Web hosting and deployment
- **Supabase** - Backend infrastructure and authentication

---

<div align="center">
  <strong>Built with ❤️ for productivity and accessibility</strong>
  <br>
  <br>
  <a href="https://voiceinputassistant.com">Website</a> •
  <a href="https://docs.voiceinputassistant.com">Docs</a> •
  <a href="https://github.com/yourusername/voice-input-assistant">GitHub</a> •
  <a href="https://twitter.com/voiceinputai">Twitter</a>
</div>