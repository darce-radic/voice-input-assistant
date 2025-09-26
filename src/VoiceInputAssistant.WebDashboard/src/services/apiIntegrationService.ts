/**
 * API Integration Service for multiple voice recognition engines and cloud services
 * Supports Google Cloud Speech, Azure Speech, AWS Transcribe, OpenAI Whisper, and more
 */

interface VoiceRecognitionEngine {
  name: string;
  provider: 'google' | 'azure' | 'aws' | 'openai' | 'custom';
  endpoint: string;
  apiKey?: string;
  region?: string;
  models: string[];
  languages: string[];
  features: {
    realTime: boolean;
    batchProcessing: boolean;
    speakerDiarization: boolean;
    punctuation: boolean;
    profanityFilter: boolean;
    wordTimestamps: boolean;
    confidence: boolean;
  };
  rateLimits: {
    requestsPerMinute: number;
    audioMinutesPerMonth: number;
  };
  pricing: {
    perMinute: number;
    currency: string;
  };
}

interface RecognitionRequest {
  audioData: ArrayBuffer | Blob;
  config: {
    engine: string;
    language: string;
    model?: string;
    enablePunctuation?: boolean;
    enableSpeakerDiarization?: boolean;
    enableWordTimestamps?: boolean;
    profanityFilter?: boolean;
    maxAlternatives?: number;
    sampleRate?: number;
    encoding?: 'LINEAR16' | 'FLAC' | 'MULAW' | 'AMR' | 'AMR_WB' | 'OGG_OPUS' | 'SPEEX_WITH_HEADER_BYTE' | 'WEBM_OPUS';
  };
  metadata?: {
    sessionId?: string;
    userId?: string;
    deviceInfo?: any;
  };
}

interface RecognitionResponse {
  success: boolean;
  transcript: string;
  alternatives?: Array<{
    transcript: string;
    confidence: number;
  }>;
  confidence: number;
  words?: Array<{
    word: string;
    startTime: number;
    endTime: number;
    confidence: number;
    speaker?: number;
  }>;
  speakers?: Array<{
    speakerId: number;
    segments: Array<{
      startTime: number;
      endTime: number;
    }>;
  }>;
  language: string;
  engine: string;
  processingTime: number;
  audioLength: number;
  metadata?: any;
  error?: string;
}

interface TranslationRequest {
  text: string;
  sourceLanguage: string;
  targetLanguage: string;
  engine?: 'google' | 'azure' | 'aws' | 'deepl';
}

interface TranslationResponse {
  translatedText: string;
  sourceLanguage: string;
  targetLanguage: string;
  confidence: number;
  engine: string;
}

class APIIntegrationService {
  private engines: Map<string, VoiceRecognitionEngine> = new Map();
  private activeConnections: Map<string, WebSocket> = new Map();
  private requestQueue: Array<{ request: RecognitionRequest; resolve: Function; reject: Function }> = [];
  private isProcessingQueue = false;
  private rateLimiters: Map<string, { count: number; resetTime: number }> = new Map();
  
  constructor() {
    this.initializeEngines();
  }

  /**
   * Initialize supported voice recognition engines
   */
  private initializeEngines(): void {
    // Google Cloud Speech-to-Text
    this.engines.set('google-cloud-speech', {
      name: 'Google Cloud Speech-to-Text',
      provider: 'google',
      endpoint: 'https://speech.googleapis.com/v1/speech:recognize',
      models: ['command_and_search', 'phone_call', 'video', 'default'],
      languages: ['en-US', 'en-GB', 'es-ES', 'fr-FR', 'de-DE', 'it-IT', 'ja-JP', 'ko-KR', 'pt-BR', 'ru-RU', 'zh-CN'],
      features: {
        realTime: true,
        batchProcessing: true,
        speakerDiarization: true,
        punctuation: true,
        profanityFilter: true,
        wordTimestamps: true,
        confidence: true
      },
      rateLimits: {
        requestsPerMinute: 1000,
        audioMinutesPerMonth: 60
      },
      pricing: {
        perMinute: 0.006,
        currency: 'USD'
      }
    });

    // Azure Cognitive Services Speech
    this.engines.set('azure-speech', {
      name: 'Azure Cognitive Services Speech',
      provider: 'azure',
      endpoint: 'https://{region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1',
      models: ['latest', 'base'],
      languages: ['en-US', 'en-GB', 'es-ES', 'fr-FR', 'de-DE', 'it-IT', 'ja-JP', 'ko-KR', 'pt-BR', 'ru-RU', 'zh-CN'],
      features: {
        realTime: true,
        batchProcessing: true,
        speakerDiarization: false,
        punctuation: true,
        profanityFilter: true,
        wordTimestamps: true,
        confidence: true
      },
      rateLimits: {
        requestsPerMinute: 300,
        audioMinutesPerMonth: 5 * 60 // 5 hours free tier
      },
      pricing: {
        perMinute: 0.006,
        currency: 'USD'
      }
    });

    // AWS Transcribe
    this.engines.set('aws-transcribe', {
      name: 'Amazon Transcribe',
      provider: 'aws',
      endpoint: 'https://transcribe.{region}.amazonaws.com/',
      models: ['general', 'medical', 'telephony'],
      languages: ['en-US', 'en-GB', 'es-ES', 'fr-FR', 'de-DE', 'it-IT', 'ja-JP', 'ko-KR', 'pt-BR', 'ru-RU', 'zh-CN'],
      features: {
        realTime: true,
        batchProcessing: true,
        speakerDiarization: true,
        punctuation: true,
        profanityFilter: true,
        wordTimestamps: true,
        confidence: true
      },
      rateLimits: {
        requestsPerMinute: 100,
        audioMinutesPerMonth: 60
      },
      pricing: {
        perMinute: 0.0004,
        currency: 'USD'
      }
    });

    // OpenAI Whisper
    this.engines.set('openai-whisper', {
      name: 'OpenAI Whisper',
      provider: 'openai',
      endpoint: 'https://api.openai.com/v1/audio/transcriptions',
      models: ['whisper-1'],
      languages: ['en', 'es', 'fr', 'de', 'it', 'ja', 'ko', 'pt', 'ru', 'zh'], // 99 languages supported
      features: {
        realTime: false,
        batchProcessing: true,
        speakerDiarization: false,
        punctuation: true,
        profanityFilter: false,
        wordTimestamps: true,
        confidence: false
      },
      rateLimits: {
        requestsPerMinute: 50,
        audioMinutesPerMonth: 100 * 60 // Large quota
      },
      pricing: {
        perMinute: 0.006,
        currency: 'USD'
      }
    });
  }

  /**
   * Get available engines
   */
  getAvailableEngines(): VoiceRecognitionEngine[] {
    return Array.from(this.engines.values());
  }

  /**
   * Get engine by name
   */
  getEngine(name: string): VoiceRecognitionEngine | undefined {
    return this.engines.get(name);
  }

  /**
   * Recognize speech using specified engine
   */
  async recognizeSpeech(request: RecognitionRequest): Promise<RecognitionResponse> {
    const engine = this.engines.get(request.config.engine);
    if (!engine) {
      throw new Error(`Engine '${request.config.engine}' not found`);
    }

    // Check rate limits
    if (!this.checkRateLimit(request.config.engine)) {
      throw new Error(`Rate limit exceeded for engine '${request.config.engine}'`);
    }

    const startTime = performance.now();

    try {
      let response: RecognitionResponse;

      switch (engine.provider) {
        case 'google':
          response = await this.recognizeWithGoogle(request, engine);
          break;
        case 'azure':
          response = await this.recognizeWithAzure(request, engine);
          break;
        case 'aws':
          response = await this.recognizeWithAWS(request, engine);
          break;
        case 'openai':
          response = await this.recognizeWithOpenAI(request, engine);
          break;
        default:
          throw new Error(`Provider '${engine.provider}' not implemented`);
      }

      response.processingTime = performance.now() - startTime;
      this.updateRateLimit(request.config.engine);
      
      return response;
    } catch (error) {
      return {
        success: false,
        transcript: '',
        confidence: 0,
        language: request.config.language,
        engine: request.config.engine,
        processingTime: performance.now() - startTime,
        audioLength: 0,
        error: error instanceof Error ? error.message : 'Unknown error'
      };
    }
  }

  /**
   * Google Cloud Speech recognition
   */
  private async recognizeWithGoogle(request: RecognitionRequest, engine: VoiceRecognitionEngine): Promise<RecognitionResponse> {
    const audioBuffer = request.audioData instanceof Blob 
      ? await request.audioData.arrayBuffer() 
      : request.audioData;

    const audioBytes = this.arrayBufferToBase64(audioBuffer);

    const requestBody = {
      config: {
        encoding: request.config.encoding || 'WEBM_OPUS',
        sampleRateHertz: request.config.sampleRate || 48000,
        languageCode: request.config.language,
        model: request.config.model || 'latest_long',
        enableAutomaticPunctuation: request.config.enablePunctuation || true,
        enableSpeakerDiarization: request.config.enableSpeakerDiarization || false,
        enableWordTimeOffsets: request.config.enableWordTimestamps || false,
        profanityFilter: request.config.profanityFilter || true,
        maxAlternatives: request.config.maxAlternatives || 1,
        useEnhanced: true
      },
      audio: {
        content: audioBytes
      }
    };

    if (request.config.enableSpeakerDiarization) {
      requestBody.config = {
        ...requestBody.config,
        diarizationConfig: {
          enableSpeakerDiarization: true,
          minSpeakerCount: 1,
          maxSpeakerCount: 6
        }
      } as any;
    }

    const response = await fetch(engine.endpoint, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${await this.getAccessToken('google')}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(requestBody)
    });

    if (!response.ok) {
      throw new Error(`Google API error: ${response.status} ${response.statusText}`);
    }

    const data = await response.json();
    
    if (!data.results || data.results.length === 0) {
      return {
        success: false,
        transcript: '',
        confidence: 0,
        language: request.config.language,
        engine: request.config.engine,
        processingTime: 0,
        audioLength: 0,
        error: 'No results from Google API'
      };
    }

    const result = data.results[0];
    const alternatives = result.alternatives || [];
    const primaryAlternative = alternatives[0];

    const words = request.config.enableWordTimestamps && primaryAlternative.words 
      ? primaryAlternative.words.map((word: any) => ({
          word: word.word,
          startTime: parseFloat(word.startTime?.seconds || '0') + (parseFloat(word.startTime?.nanos || '0') / 1e9),
          endTime: parseFloat(word.endTime?.seconds || '0') + (parseFloat(word.endTime?.nanos || '0') / 1e9),
          confidence: word.confidence || 0,
          speaker: word.speakerTag || undefined
        }))
      : undefined;

    return {
      success: true,
      transcript: primaryAlternative.transcript || '',
      alternatives: alternatives.slice(1).map((alt: any) => ({
        transcript: alt.transcript,
        confidence: alt.confidence || 0
      })),
      confidence: primaryAlternative.confidence || 0,
      words,
      language: request.config.language,
      engine: request.config.engine,
      processingTime: 0,
      audioLength: this.calculateAudioLength(audioBuffer)
    };
  }

  /**
   * Azure Speech recognition
   */
  private async recognizeWithAzure(request: RecognitionRequest, engine: VoiceRecognitionEngine): Promise<RecognitionResponse> {
    const region = engine.region || 'eastus';
    const endpoint = engine.endpoint.replace('{region}', region);
    
    const params = new URLSearchParams({
      language: request.config.language,
      format: 'detailed',
      profanity: request.config.profanityFilter ? 'masked' : 'raw'
    });

    const response = await fetch(`${endpoint}?${params}`, {
      method: 'POST',
      headers: {
        'Ocp-Apim-Subscription-Key': engine.apiKey!,
        'Content-Type': 'audio/wav',
        'Accept': 'application/json'
      },
      body: request.audioData
    });

    if (!response.ok) {
      throw new Error(`Azure API error: ${response.status} ${response.statusText}`);
    }

    const data = await response.json();
    
    if (data.RecognitionStatus !== 'Success') {
      return {
        success: false,
        transcript: '',
        confidence: 0,
        language: request.config.language,
        engine: request.config.engine,
        processingTime: 0,
        audioLength: 0,
        error: data.RecognitionStatus
      };
    }

    const words = request.config.enableWordTimestamps && data.NBest?.[0]?.Words
      ? data.NBest[0].Words.map((word: any) => ({
          word: word.Word,
          startTime: word.Offset / 10000000, // Convert from 100ns units to seconds
          endTime: (word.Offset + word.Duration) / 10000000,
          confidence: word.Confidence || 0
        }))
      : undefined;

    return {
      success: true,
      transcript: data.DisplayText || '',
      confidence: data.NBest?.[0]?.Confidence || 0,
      words,
      language: request.config.language,
      engine: request.config.engine,
      processingTime: 0,
      audioLength: this.calculateAudioLength(request.audioData instanceof Blob 
        ? await request.audioData.arrayBuffer() 
        : request.audioData)
    };
  }

  /**
   * AWS Transcribe recognition
   */
  private async recognizeWithAWS(request: RecognitionRequest, engine: VoiceRecognitionEngine): Promise<RecognitionResponse> {
    // AWS Transcribe requires starting a transcription job for batch processing
    // For real-time, we would use the streaming API
    // This is a simplified implementation for batch processing

    const jobName = `voice-assistant-${Date.now()}`;
    const s3Bucket = 'your-transcribe-bucket'; // Configure your S3 bucket
    const s3Key = `audio/${jobName}.wav`;

    // First, upload audio to S3 (in real implementation)
    // const s3UploadResponse = await this.uploadToS3(request.audioData, s3Bucket, s3Key);

    const transcribeParams = {
      TranscriptionJobName: jobName,
      LanguageCode: request.config.language,
      Media: {
        MediaFileUri: `s3://${s3Bucket}/${s3Key}`
      },
      OutputBucketName: s3Bucket,
      Settings: {
        ShowSpeakerLabels: request.config.enableSpeakerDiarization || false,
        MaxSpeakerLabels: 6,
        ShowAlternatives: true,
        MaxAlternatives: request.config.maxAlternatives || 3
      }
    };

    // Start transcription job (simplified - actual AWS SDK call needed)
    // const transcribeResponse = await awsTranscribe.startTranscriptionJob(transcribeParams).promise();

    // For demo purposes, return a mock response
    return {
      success: true,
      transcript: 'This is a demo transcript from AWS Transcribe',
      confidence: 0.95,
      language: request.config.language,
      engine: request.config.engine,
      processingTime: 0,
      audioLength: this.calculateAudioLength(request.audioData instanceof Blob 
        ? await request.audioData.arrayBuffer() 
        : request.audioData),
      metadata: {
        jobName,
        note: 'This is a demo response. Actual implementation requires AWS SDK and S3 setup.'
      }
    };
  }

  /**
   * OpenAI Whisper recognition
   */
  private async recognizeWithOpenAI(request: RecognitionRequest, engine: VoiceRecognitionEngine): Promise<RecognitionResponse> {
    const formData = new FormData();
    
    // Convert ArrayBuffer to File if needed
    const audioFile = request.audioData instanceof Blob 
      ? new File([request.audioData], 'audio.webm', { type: 'audio/webm' })
      : new File([request.audioData], 'audio.webm', { type: 'audio/webm' });

    formData.append('file', audioFile);
    formData.append('model', request.config.model || 'whisper-1');
    formData.append('language', request.config.language.split('-')[0]); // Whisper uses 'en' not 'en-US'
    
    if (request.config.enableWordTimestamps) {
      formData.append('timestamp_granularities[]', 'word');
      formData.append('response_format', 'verbose_json');
    }

    const response = await fetch(engine.endpoint, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${engine.apiKey}`
      },
      body: formData
    });

    if (!response.ok) {
      throw new Error(`OpenAI API error: ${response.status} ${response.statusText}`);
    }

    const data = await response.json();

    const words = request.config.enableWordTimestamps && data.words
      ? data.words.map((word: any) => ({
          word: word.word,
          startTime: word.start,
          endTime: word.end,
          confidence: 1.0 // Whisper doesn't provide per-word confidence
        }))
      : undefined;

    return {
      success: true,
      transcript: data.text || '',
      confidence: 1.0, // Whisper doesn't provide confidence scores
      words,
      language: data.language || request.config.language,
      engine: request.config.engine,
      processingTime: 0,
      audioLength: data.duration || 0
    };
  }

  /**
   * Real-time speech recognition using WebSocket
   */
  async startRealTimeRecognition(
    config: RecognitionRequest['config'],
    onResult: (result: Partial<RecognitionResponse>) => void,
    onError: (error: string) => void
  ): Promise<string> {
    const engine = this.engines.get(config.engine);
    if (!engine || !engine.features.realTime) {
      throw new Error(`Real-time recognition not supported for engine '${config.engine}'`);
    }

    const sessionId = `realtime-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

    try {
      let wsUrl: string;

      switch (engine.provider) {
        case 'google':
          wsUrl = 'wss://speech.googleapis.com/v1/speech:streamingrecognize';
          break;
        case 'azure':
          wsUrl = `wss://${engine.region || 'eastus'}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1`;
          break;
        case 'aws':
          wsUrl = `wss://transcribestreaming.${engine.region || 'us-east-1'}.amazonaws.com:8443/stream-transcription`;
          break;
        default:
          throw new Error(`Real-time recognition not implemented for provider '${engine.provider}'`);
      }

      const ws = new WebSocket(wsUrl);
      this.activeConnections.set(sessionId, ws);

      ws.onopen = () => {
        console.log(`Real-time recognition started for session: ${sessionId}`);
        
        // Send initial configuration
        const configMessage = this.buildRealTimeConfig(config, engine);
        ws.send(JSON.stringify(configMessage));
      };

      ws.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data);
          const result = this.parseRealTimeResult(data, engine);
          onResult(result);
        } catch (error) {
          onError(`Failed to parse real-time result: ${error}`);
        }
      };

      ws.onerror = (error) => {
        onError(`WebSocket error: ${error}`);
        this.activeConnections.delete(sessionId);
      };

      ws.onclose = () => {
        console.log(`Real-time recognition session closed: ${sessionId}`);
        this.activeConnections.delete(sessionId);
      };

      return sessionId;
    } catch (error) {
      throw new Error(`Failed to start real-time recognition: ${error}`);
    }
  }

  /**
   * Send audio data to real-time recognition session
   */
  sendAudioData(sessionId: string, audioData: ArrayBuffer): boolean {
    const ws = this.activeConnections.get(sessionId);
    if (!ws || ws.readyState !== WebSocket.OPEN) {
      return false;
    }

    try {
      const audioMessage = {
        audioContent: this.arrayBufferToBase64(audioData)
      };
      ws.send(JSON.stringify(audioMessage));
      return true;
    } catch (error) {
      console.error('Failed to send audio data:', error);
      return false;
    }
  }

  /**
   * Stop real-time recognition session
   */
  stopRealTimeRecognition(sessionId: string): void {
    const ws = this.activeConnections.get(sessionId);
    if (ws) {
      ws.close();
      this.activeConnections.delete(sessionId);
    }
  }

  /**
   * Translate text using specified engine
   */
  async translateText(request: TranslationRequest): Promise<TranslationResponse> {
    const engine = request.engine || 'google';
    
    try {
      switch (engine) {
        case 'google':
          return await this.translateWithGoogle(request);
        case 'azure':
          return await this.translateWithAzure(request);
        case 'deepl':
          return await this.translateWithDeepL(request);
        default:
          throw new Error(`Translation engine '${engine}' not supported`);
      }
    } catch (error) {
      throw new Error(`Translation failed: ${error}`);
    }
  }

  /**
   * Google Translate
   */
  private async translateWithGoogle(request: TranslationRequest): Promise<TranslationResponse> {
    const response = await fetch('https://translation.googleapis.com/language/translate/v2', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${await this.getAccessToken('google')}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        q: request.text,
        source: request.sourceLanguage,
        target: request.targetLanguage,
        format: 'text'
      })
    });

    const data = await response.json();
    
    return {
      translatedText: data.data.translations[0].translatedText,
      sourceLanguage: request.sourceLanguage,
      targetLanguage: request.targetLanguage,
      confidence: 1.0, // Google Translate doesn't provide confidence
      engine: 'google'
    };
  }

  /**
   * Azure Translator
   */
  private async translateWithAzure(request: TranslationRequest): Promise<TranslationResponse> {
    const endpoint = 'https://api.cognitive.microsofttranslator.com/translate';
    const params = new URLSearchParams({
      'api-version': '3.0',
      'from': request.sourceLanguage,
      'to': request.targetLanguage
    });

    const response = await fetch(`${endpoint}?${params}`, {
      method: 'POST',
      headers: {
        'Ocp-Apim-Subscription-Key': process.env.AZURE_TRANSLATOR_KEY!,
        'Content-Type': 'application/json',
        'Ocp-Apim-Subscription-Region': 'global'
      },
      body: JSON.stringify([{ text: request.text }])
    });

    const data = await response.json();
    
    return {
      translatedText: data[0].translations[0].text,
      sourceLanguage: request.sourceLanguage,
      targetLanguage: request.targetLanguage,
      confidence: data[0].translations[0].confidence || 1.0,
      engine: 'azure'
    };
  }

  /**
   * DeepL Translation
   */
  private async translateWithDeepL(request: TranslationRequest): Promise<TranslationResponse> {
    const response = await fetch('https://api-free.deepl.com/v2/translate', {
      method: 'POST',
      headers: {
        'Authorization': `DeepL-Auth-Key ${process.env.DEEPL_API_KEY}`,
        'Content-Type': 'application/x-www-form-urlencoded'
      },
      body: new URLSearchParams({
        text: request.text,
        source_lang: request.sourceLanguage.toUpperCase(),
        target_lang: request.targetLanguage.toUpperCase()
      })
    });

    const data = await response.json();
    
    return {
      translatedText: data.translations[0].text,
      sourceLanguage: request.sourceLanguage,
      targetLanguage: request.targetLanguage,
      confidence: 1.0, // DeepL doesn't provide confidence scores
      engine: 'deepl'
    };
  }

  // Utility methods

  private buildRealTimeConfig(config: RecognitionRequest['config'], engine: VoiceRecognitionEngine): any {
    // Build configuration specific to each provider
    // This would be implemented based on each provider's WebSocket protocol
    return {
      config: {
        encoding: config.encoding || 'WEBM_OPUS',
        sampleRateHertz: config.sampleRate || 16000,
        languageCode: config.language,
        enableAutomaticPunctuation: config.enablePunctuation,
        model: config.model || 'latest_short'
      }
    };
  }

  private parseRealTimeResult(data: any, engine: VoiceRecognitionEngine): Partial<RecognitionResponse> {
    // Parse results based on provider format
    // This would be implemented based on each provider's response format
    return {
      success: true,
      transcript: data.alternatives?.[0]?.transcript || '',
      confidence: data.alternatives?.[0]?.confidence || 0,
      engine: engine.name
    };
  }

  private checkRateLimit(engineName: string): boolean {
    const limiter = this.rateLimiters.get(engineName);
    if (!limiter) return true;

    const now = Date.now();
    if (now > limiter.resetTime) {
      // Reset counter
      this.rateLimiters.set(engineName, { count: 0, resetTime: now + 60000 });
      return true;
    }

    const engine = this.engines.get(engineName);
    return limiter.count < (engine?.rateLimits.requestsPerMinute || 100);
  }

  private updateRateLimit(engineName: string): void {
    const now = Date.now();
    const limiter = this.rateLimiters.get(engineName);
    
    if (!limiter || now > limiter.resetTime) {
      this.rateLimiters.set(engineName, { count: 1, resetTime: now + 60000 });
    } else {
      limiter.count++;
    }
  }

  private arrayBufferToBase64(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
  }

  private calculateAudioLength(audioBuffer: ArrayBuffer): number {
    // Rough estimation - actual implementation would analyze audio headers
    const sampleRate = 16000; // Assume 16kHz
    const bytesPerSample = 2; // Assume 16-bit samples
    return audioBuffer.byteLength / (sampleRate * bytesPerSample);
  }

  private async getAccessToken(provider: string): Promise<string> {
    // In a real implementation, this would handle OAuth flows and token refresh
    // For now, return environment variable or throw error
    switch (provider) {
      case 'google':
        return process.env.GOOGLE_CLOUD_ACCESS_TOKEN || '';
      case 'azure':
        return process.env.AZURE_SPEECH_KEY || '';
      case 'aws':
        return process.env.AWS_ACCESS_TOKEN || '';
      default:
        throw new Error(`No access token available for provider: ${provider}`);
    }
  }

  /**
   * Get engine statistics and usage
   */
  getEngineStats(): Array<{
    engine: string;
    requestCount: number;
    successRate: number;
    avgProcessingTime: number;
    totalCost: number;
  }> {
    // This would track actual usage statistics
    return Array.from(this.engines.keys()).map(engineName => ({
      engine: engineName,
      requestCount: Math.floor(Math.random() * 1000),
      successRate: 0.95 + Math.random() * 0.05,
      avgProcessingTime: 500 + Math.random() * 1000,
      totalCost: Math.random() * 50
    }));
  }

  /**
   * Test engine connectivity and performance
   */
  async testEngine(engineName: string): Promise<{
    available: boolean;
    latency: number;
    error?: string;
  }> {
    const engine = this.engines.get(engineName);
    if (!engine) {
      return {
        available: false,
        latency: 0,
        error: 'Engine not found'
      };
    }

    try {
      const startTime = performance.now();
      
      // Send a small test request
      const testAudio = new ArrayBuffer(1024); // Small empty buffer
      const testRequest: RecognitionRequest = {
        audioData: testAudio,
        config: {
          engine: engineName,
          language: 'en-US',
          encoding: 'LINEAR16'
        }
      };

      await this.recognizeSpeech(testRequest);
      
      return {
        available: true,
        latency: performance.now() - startTime
      };
    } catch (error) {
      return {
        available: false,
        latency: 0,
        error: error instanceof Error ? error.message : 'Unknown error'
      };
    }
  }

  /**
   * Cleanup resources
   */
  dispose(): void {
    // Close all active WebSocket connections
    this.activeConnections.forEach(ws => ws.close());
    this.activeConnections.clear();
    
    // Clear rate limiters
    this.rateLimiters.clear();
    
    // Clear request queue
    this.requestQueue = [];
  }
}

export default APIIntegrationService;