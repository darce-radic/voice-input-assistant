# 🎤 Voice Input Assistant - Web Dashboard

A modern, feature-rich Progressive Web App (PWA) for voice input processing with real-time transcription, machine learning capabilities, and advanced audio processing.

## ✨ Features

- **🎙️ Real-time Voice Recording & Transcription** - Advanced voice activity detection with WebRTC
- **🧠 Machine Learning Integration** - Emotion detection, voice activity analysis, and sentiment analysis
- **⌨️ Configurable Keyboard Hotkeys** - Customizable shortcuts for all major functions
- **📱 Progressive Web App** - Full PWA support with offline functionality and native app-like experience
- **🔄 Offline Support** - Advanced caching strategies with background sync
- **📊 Performance Monitoring** - Real-time web vitals tracking and optimization
- **🌐 Cross-Platform** - Works on desktop, mobile, and tablet devices
- **🔒 Privacy-First** - All processing can be done locally with optional cloud integration
- **🎨 Modern UI/UX** - Responsive design with accessibility features

## 🚀 Quick Start

### Prerequisites

- **Node.js** >= 18.0.0
- **npm** >= 8.0.0 (comes with Node.js)
- Modern web browser with WebRTC support
- HTTPS environment for PWA features (development server included)

### Installation

#### Option 1: Automated Setup (Recommended)

**For Unix/Linux/macOS:**
```bash
chmod +x install.sh
./install.sh
```

**For Windows (PowerShell):**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
.\install.ps1
```

#### Option 2: Manual Setup

1. **Clone and navigate to the project:**
   ```bash
   git clone <repository-url>
   cd VoiceInputAssistant.WebDashboard
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Create environment file:**
   ```bash
   cp .env.example .env
   # Edit .env with your API keys and configuration
   ```

4. **Generate VAPID keys for push notifications:**
   ```bash
   npx web-push generate-vapid-keys
   ```

### Development

#### Start Development Server
```bash
npm start
```
The app will be available at `http://localhost:3000`

#### Start HTTPS Development Server (Required for PWA features)
```bash
npm run https
```
The app will be available at `https://localhost:3443`

#### Other Development Commands
```bash
npm run build        # Create production build
npm run test         # Run tests
npm run lint         # Run ESLint
npm run type-check   # Run TypeScript type checking
npm run pwa-test     # Build and serve for PWA testing
```

## 🚀 Deployment

### Automated Deployment

The project includes automated deployment scripts for different environments:

```bash
# Deploy to staging (default)
./deploy.sh

# Deploy to production
./deploy.sh production

# Build and test locally
./deploy.sh test

# Show help
./deploy.sh help
```

### Manual Deployment

1. **Build for production:**
   ```bash
   npm run build
   ```

2. **Upload the `build/` directory to your web server**

3. **Configure your web server:**
   - Serve all routes through `index.html` for SPA routing
   - Set proper headers for service worker and manifest files
   - Enable HTTPS for PWA functionality

### Environment Configuration

The app supports multiple deployment environments:

- **Production** (`REACT_APP_ENV=production`): Optimized build, no source maps
- **Staging** (`REACT_APP_ENV=staging`): Production build with debugging enabled  
- **Development** (`REACT_APP_ENV=development`): Full debugging and hot reload
- **Test** (`REACT_APP_ENV=test`): Testing environment with mock data

## ⚙️ Configuration

### Environment Variables

Create a `.env` file in the project root:

```env
# Environment
NODE_ENV=development
REACT_APP_ENV=development
GENERATE_SOURCEMAP=true

# API Configuration
REACT_APP_API_BASE_URL=https://your-api-server.com
REACT_APP_WEBSOCKET_URL=wss://your-websocket-server.com

# Authentication
REACT_APP_AUTH0_DOMAIN=your-domain.auth0.com
REACT_APP_AUTH0_CLIENT_ID=your-client-id

# Push Notifications (generate with web-push)
REACT_APP_VAPID_PUBLIC_KEY=your-vapid-public-key

# Features
REACT_APP_MOCK_API=false
REACT_APP_DEBUG_MODE=false
REACT_APP_ANALYTICS_ID=your-analytics-id

# Development
HTTPS=true
SSL_CRT_FILE=path/to/cert.crt
SSL_KEY_FILE=path/to/cert.key
```

### Service Configuration

The application uses several configurable services:

#### WebRTC Service
```typescript
// Configure audio processing
webrtcService.configure({
  sampleRate: 16000,
  channels: 1,
  vadSensitivity: 0.7,
  enableEchoCancellation: true
});
```

#### Machine Learning Service
```typescript
// Configure ML models
mlService.configure({
  voiceActivityThreshold: 0.5,
  emotionDetectionEnabled: true,
  sentimentAnalysisEnabled: true
});
```

#### Hotkey Service
```typescript
// Configure custom hotkeys
hotkeyService.register('ctrl+r', () => {
  voiceService.startRecording();
});
```

## 📱 PWA Features

### Installation

The app can be installed as a PWA on:

- **Desktop:** Chrome, Edge, Safari (Add to Applications)
- **Android:** Chrome, Firefox (Add to Home Screen)
- **iOS:** Safari (Add to Home Screen)

### Offline Functionality

- **Runtime Caching:** API responses, assets, and fonts
- **Background Sync:** Queue offline actions for when connection returns
- **Offline Fallbacks:** Custom offline pages and cached content

### Native Features

- **Push Notifications:** Real-time alerts and updates
- **File System Access:** Import/export configurations and transcriptions
- **Share API:** Share transcriptions and recordings
- **Wake Lock:** Keep screen active during recording
- **Vibration:** Haptic feedback for mobile devices

## 🔧 Advanced Configuration

### Custom Build Configuration

The project uses CRACO for advanced webpack customization. See `craco.config.js` for:

- Bundle splitting optimization
- Service worker configuration  
- PWA manifest customization
- Development server settings

### Performance Optimization

Built-in performance features:

- **Code Splitting:** Lazy loading of components and services
- **Bundle Analysis:** Built-in bundle size monitoring
- **Image Optimization:** Automatic image compression and WebP conversion
- **Font Optimization:** Preload and optimize web fonts
- **Caching Strategies:** Intelligent caching for different resource types

### Accessibility

- **Screen Reader Support:** Full ARIA labeling and semantic HTML
- **Keyboard Navigation:** Complete keyboard accessibility
- **High Contrast Mode:** Support for system dark/light themes
- **Focus Management:** Proper focus handling for modals and interactions

## 🧪 Testing

### Unit Tests
```bash
npm test
```

### PWA Testing
```bash
npm run pwa-test
```

This builds the app and serves it with a local HTTPS server for testing PWA functionality.

### Performance Testing

Use the built-in performance monitoring:

```typescript
import { performanceService } from './services/performanceService';

// Monitor web vitals
performanceService.startMonitoring();

// Get performance report
const report = performanceService.getPerformanceReport();
```

## 🔍 Troubleshooting

### Common Issues

**PWA not installing:**
- Ensure you're using HTTPS
- Check that manifest.json is properly served
- Verify service worker registration

**Microphone not working:**
- Grant microphone permissions
- Use HTTPS (required for getUserMedia)
- Check browser compatibility

**Hot reloading not working:**
- Clear browser cache
- Restart development server
- Check for port conflicts

**Build fails:**
- Clear npm cache: `npm cache clean --force`
- Delete node_modules and reinstall: `rm -rf node_modules && npm install`
- Check Node.js version compatibility

### Debug Mode

Enable debug mode for detailed logging:

```env
REACT_APP_DEBUG_MODE=true
```

This enables:
- Verbose console logging
- Performance metrics display
- Service worker debug information
- WebRTC connection details

## 📚 API Reference

### Core Services

- **VoiceRecordingService:** Audio capture and processing
- **TranscriptionService:** Speech-to-text conversion
- **WebRTCService:** Real-time communication and audio analysis
- **MLService:** Machine learning for voice activity and emotion detection
- **HotkeyService:** Keyboard shortcut management
- **OfflineCacheService:** Offline functionality and sync
- **MobileFeaturesService:** Mobile-specific capabilities
- **PerformanceService:** Performance monitoring and optimization

### Events

The application uses a centralized event system:

```typescript
// Listen for recording events
eventBus.on('recording:started', (data) => {
  console.log('Recording started:', data);
});

// Listen for transcription events  
eventBus.on('transcription:complete', (data) => {
  console.log('Transcription:', data.text);
});
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

### Development Guidelines

- Use TypeScript for all new code
- Follow the existing code style (ESLint configured)
- Add tests for new features
- Update documentation as needed
- Ensure PWA functionality is maintained

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## 🆘 Support

- **Documentation:** Check this README and inline code comments
- **Issues:** Report bugs and request features via GitHub issues
- **Discussions:** Join community discussions for questions and help

## 🎯 Roadmap

### Upcoming Features

- [ ] Multi-language transcription support
- [ ] Custom wake word detection
- [ ] Advanced voice analytics dashboard
- [ ] Team collaboration features
- [ ] Cloud storage integration
- [ ] Voice command processing
- [ ] Advanced ML model customization
- [ ] Real-time translation

### Performance Improvements

- [ ] WebAssembly integration for audio processing
- [ ] Enhanced offline model support
- [ ] Improved caching strategies
- [ ] Better mobile battery optimization

---

**Built with ❤️ using React, TypeScript, and modern web technologies.**