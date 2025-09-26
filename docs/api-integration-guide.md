# API Integration Guide

## 🚀 Quick Start

The Voice Input Assistant provides both REST and gRPC APIs for integration with front-end applications.

### Base URLs
- **REST API**: `http://localhost:5000` (dev) / `https://api.voiceinputassistant.com` (prod)
- **gRPC**: `http://localhost:5002` (dev) / `https://grpc.voiceinputassistant.com` (prod)
- **WebSocket**: `ws://localhost:5000/hubs/voice` (dev)

### API Documentation
- **Swagger UI**: `http://localhost:5000/swagger` (development only)
- **Health Check**: `http://localhost:5000/health`

## 🔐 Authentication

### 1. Register User
```javascript
const response = await fetch('http://localhost:5000/api/auth/register', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'SecurePassword123!',
    confirmPassword: 'SecurePassword123!',
    firstName: 'John',
    lastName: 'Doe',
    organization: 'Acme Corp' // optional
  })
});

const { token, refreshToken, user } = await response.json();
```

### 2. Login
```javascript
const response = await fetch('http://localhost:5000/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'SecurePassword123!'
  })
});

const { token, refreshToken, expiresAt, user } = await response.json();

// Store tokens securely
localStorage.setItem('accessToken', token);
localStorage.setItem('refreshToken', refreshToken);
```

### 3. Refresh Token
```javascript
const response = await fetch('http://localhost:5000/api/auth/refresh', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    token: localStorage.getItem('accessToken'),
    refreshToken: localStorage.getItem('refreshToken')
  })
});

const { token, refreshToken } = await response.json();
```

### 4. Authenticated Requests
```javascript
const makeAuthenticatedRequest = async (url, options = {}) => {
  const token = localStorage.getItem('accessToken');
  
  return fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });
};
```

## 🎤 Speech Recognition

### Transcribe Audio
```javascript
// Convert audio to base64
const audioToBase64 = (audioBlob) => {
  return new Promise((resolve) => {
    const reader = new FileReader();
    reader.onloadend = () => resolve(reader.result.split(',')[1]);
    reader.readAsDataURL(audioBlob);
  });
};

// Transcribe audio
const transcribeAudio = async (audioBlob) => {
  const base64Audio = await audioToBase64(audioBlob);
  
  const response = await makeAuthenticatedRequest('/api/speech/transcribe', {
    method: 'POST',
    body: JSON.stringify({
      audioData: base64Audio,
      language: 'en-US',
      engine: 'Whisper', // or 'Azure', 'Google', 'OpenAI'
      applicationContext: 'WebApp',
      options: {
        enablePunctuation: true,
        enableCapitalization: true,
        enableProfanityFilter: false
      }
    })
  });
  
  const { text, confidence, duration, metadata } = await response.json();
  return { text, confidence, duration, metadata };
};
```

### Submit Feedback
```javascript
const submitFeedback = async (originalText, correctedText, rating) => {
  const response = await makeAuthenticatedRequest('/api/speech/feedback', {
    method: 'POST',
    body: JSON.stringify({
      originalText,
      correctedText,
      rating, // 1-5
      engine: 'Whisper',
      applicationContext: 'WebApp'
    })
  });
  
  return response.json();
};
```

## 📡 Real-time Streaming (WebSocket)

### Connect to WebSocket
```javascript
import * as signalR from '@microsoft/signalr';

class VoiceAssistantHub {
  constructor() {
    this.connection = null;
  }
  
  async connect(token) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/voice', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();
    
    // Register event handlers
    this.connection.on('TranscriptionUpdate', (data) => {
      console.log('Partial transcription:', data);
    });
    
    this.connection.on('TranscriptionComplete', (data) => {
      console.log('Final transcription:', data);
    });
    
    this.connection.on('Error', (error) => {
      console.error('Hub error:', error);
    });
    
    // Start connection
    await this.connection.start();
    console.log('Connected to VoiceAssistant hub');
  }
  
  async startStreaming(language = 'en-US') {
    await this.connection.invoke('StartRecognition', { language });
  }
  
  async sendAudioChunk(audioData) {
    await this.connection.invoke('SendAudioChunk', audioData);
  }
  
  async stopStreaming() {
    await this.connection.invoke('StopRecognition');
  }
  
  async disconnect() {
    if (this.connection) {
      await this.connection.stop();
    }
  }
}

// Usage
const hub = new VoiceAssistantHub();
await hub.connect(accessToken);
await hub.startStreaming();
// ... send audio chunks
await hub.stopStreaming();
```

## 🎯 gRPC-Web Integration

### Setup gRPC-Web Client
```bash
npm install grpc-web google-protobuf
```

### Generate Client Code
```bash
# Download proto files from the repository
protoc -I=./protos voice_assistant.proto \
  --js_out=import_style=commonjs:./generated \
  --grpc-web_out=import_style=commonjs,mode=grpcwebtext:./generated
```

### Use gRPC Client
```javascript
import { VoiceAssistantClient } from './generated/voice_assistant_grpc_web_pb';
import { 
  GetStatusRequest,
  StartRecognitionRequest,
  ProcessTextRequest 
} from './generated/voice_assistant_pb';

class GrpcVoiceAssistant {
  constructor() {
    this.client = new VoiceAssistantClient('http://localhost:5002');
  }
  
  async getStatus() {
    const request = new GetStatusRequest();
    
    return new Promise((resolve, reject) => {
      this.client.getStatus(request, {}, (err, response) => {
        if (err) {
          reject(err);
        } else {
          resolve({
            isActive: response.getIsactive(),
            version: response.getVersion(),
            uptime: response.getUptime()
          });
        }
      });
    });
  }
  
  async startRecognition(settings) {
    const request = new StartRecognitionRequest();
    request.setLanguage(settings.language || 'en-US');
    request.setEngine(settings.engine || 'Whisper');
    request.setEnablecontinuous(settings.continuous || false);
    
    return new Promise((resolve, reject) => {
      this.client.startRecognition(request, {}, (err, response) => {
        if (err) {
          reject(err);
        } else {
          resolve({
            sessionId: response.getSessionid(),
            message: response.getMessage()
          });
        }
      });
    });
  }
  
  async processText(text, options = {}) {
    const request = new ProcessTextRequest();
    request.setText(text);
    request.setTone(options.tone || 'Natural');
    request.setFormat(options.format || 'None');
    request.setCapitalization(options.capitalization || 'Sentence');
    request.setCorrectgrammar(options.correctGrammar !== false);
    
    return new Promise((resolve, reject) => {
      this.client.processText(request, {}, (err, response) => {
        if (err) {
          reject(err);
        } else {
          resolve({
            processedText: response.getProcessedtext(),
            confidence: response.getConfidence(),
            corrections: response.getCorrectionsList()
          });
        }
      });
    });
  }
}

// Usage
const grpcClient = new GrpcVoiceAssistant();
const status = await grpcClient.getStatus();
const session = await grpcClient.startRecognition({ language: 'en-US' });
const result = await grpcClient.processText('hello world', { 
  tone: 'Professional',
  correctGrammar: true 
});
```

## 🎨 React Integration Example

### Custom Hook for Voice Input
```jsx
import { useState, useCallback, useRef } from 'react';

export function useVoiceInput(options = {}) {
  const [isRecording, setIsRecording] = useState(false);
  const [transcription, setTranscription] = useState('');
  const [isProcessing, setIsProcessing] = useState(false);
  const mediaRecorderRef = useRef(null);
  const audioChunksRef = useRef([]);
  
  const startRecording = useCallback(async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const mediaRecorder = new MediaRecorder(stream);
      mediaRecorderRef.current = mediaRecorder;
      audioChunksRef.current = [];
      
      mediaRecorder.ondataavailable = (event) => {
        audioChunksRef.current.push(event.data);
      };
      
      mediaRecorder.onstop = async () => {
        const audioBlob = new Blob(audioChunksRef.current, { type: 'audio/wav' });
        await processAudio(audioBlob);
      };
      
      mediaRecorder.start();
      setIsRecording(true);
    } catch (error) {
      console.error('Failed to start recording:', error);
    }
  }, []);
  
  const stopRecording = useCallback(() => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      mediaRecorderRef.current.stream.getTracks().forEach(track => track.stop());
      setIsRecording(false);
    }
  }, [isRecording]);
  
  const processAudio = async (audioBlob) => {
    setIsProcessing(true);
    try {
      const base64Audio = await audioToBase64(audioBlob);
      const response = await fetch('/api/speech/transcribe', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          audioData: base64Audio,
          language: options.language || 'en-US',
          engine: options.engine || 'Whisper'
        })
      });
      
      const { text } = await response.json();
      setTranscription(text);
      
      if (options.onTranscription) {
        options.onTranscription(text);
      }
    } catch (error) {
      console.error('Failed to process audio:', error);
    } finally {
      setIsProcessing(false);
    }
  };
  
  return {
    isRecording,
    isProcessing,
    transcription,
    startRecording,
    stopRecording
  };
}

// Usage in component
function VoiceInputComponent() {
  const {
    isRecording,
    isProcessing,
    transcription,
    startRecording,
    stopRecording
  } = useVoiceInput({
    language: 'en-US',
    onTranscription: (text) => console.log('Transcribed:', text)
  });
  
  return (
    <div>
      <button 
        onMouseDown={startRecording}
        onMouseUp={stopRecording}
        disabled={isProcessing}
      >
        {isRecording ? '🔴 Recording...' : '🎤 Hold to Record'}
      </button>
      {isProcessing && <p>Processing...</p>}
      {transcription && <p>Transcription: {transcription}</p>}
    </div>
  );
}
```

## 🔧 CORS Configuration

For production deployments, update CORS settings in `appsettings.json`:

```json
{
  "VoiceAssistant": {
    "Security": {
      "AllowedOrigins": [
        "https://yourdomain.com",
        "https://app.yourdomain.com"
      ]
    }
  }
}
```

## 📊 Error Handling

### Standard Error Response Format
```json
{
  "error": {
    "code": "INVALID_REQUEST",
    "message": "The request is invalid",
    "details": {
      "field": "audioData",
      "reason": "Audio data is required"
    }
  },
  "timestamp": "2024-01-01T00:00:00Z",
  "traceId": "abc123"
}
```

### Common Error Codes
- `UNAUTHORIZED` - Invalid or missing authentication token
- `FORBIDDEN` - Insufficient permissions
- `INVALID_REQUEST` - Request validation failed
- `RATE_LIMITED` - Too many requests
- `INTERNAL_ERROR` - Server error

### Error Handling Example
```javascript
class ApiClient {
  async makeRequest(url, options) {
    try {
      const response = await fetch(url, options);
      
      if (!response.ok) {
        const error = await response.json();
        throw new ApiError(error.error.code, error.error.message, error.error.details);
      }
      
      return response.json();
    } catch (error) {
      if (error instanceof ApiError) {
        // Handle API errors
        switch (error.code) {
          case 'UNAUTHORIZED':
            // Refresh token or redirect to login
            await this.refreshToken();
            break;
          case 'RATE_LIMITED':
            // Wait and retry
            await this.waitAndRetry();
            break;
          default:
            console.error('API Error:', error);
        }
      } else {
        // Handle network errors
        console.error('Network Error:', error);
      }
      throw error;
    }
  }
}
```

## 📈 Rate Limiting

Default rate limits:
- **10 requests/second**
- **100 requests/minute**
- **1000 requests/hour**

Headers returned:
- `X-RateLimit-Limit` - Request limit
- `X-RateLimit-Remaining` - Remaining requests
- `X-RateLimit-Reset` - Reset timestamp

## 🚀 Best Practices

1. **Token Management**
   - Store tokens securely (HttpOnly cookies preferred)
   - Implement automatic token refresh
   - Clear tokens on logout

2. **Audio Processing**
   - Use Web Audio API for preprocessing
   - Compress audio before sending (opus/webm)
   - Implement chunked streaming for long recordings

3. **Error Recovery**
   - Implement exponential backoff for retries
   - Provide user feedback for errors
   - Log errors for debugging

4. **Performance**
   - Cache frequently used data
   - Implement request debouncing
   - Use WebSocket for real-time features

5. **Security**
   - Always use HTTPS in production
   - Validate input on client and server
   - Implement CSRF protection

## 📚 Additional Resources

- [API Reference](./api-reference.md)
- [WebSocket Events](./websocket-events.md)
- [gRPC Proto Files](../protos/)
- [Postman Collection](../postman/voice-assistant.json)
- [OpenAPI Specification](../swagger/openapi.json)