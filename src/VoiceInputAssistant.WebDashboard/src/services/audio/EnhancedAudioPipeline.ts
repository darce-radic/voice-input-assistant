/**
 * Enhanced Audio Pipeline - Inspired by Heyito patterns
 * Integrates Voice Activity Detection, noise reduction, and multi-provider STT
 */

export interface VADOptions {
  threshold?: number;
  bufferSize?: number;
  smoothingTimeConstant?: number;
  minSpeechDuration?: number;
  maxSilenceDuration?: number;
}

export interface STTConfig {
  provider: 'browser' | 'whisper' | 'deepgram' | 'google' | 'azure';
  apiKey?: string;
  model?: string;
  language?: string;
  continuous?: boolean;
  interimResults?: boolean;
}

export interface AudioPipelineConfig {
  vad: VADOptions;
  stt: STTConfig;
  noiseReduction?: boolean;
  echoCancellation?: boolean;
  autoGainControl?: boolean;
}

/**
 * Voice Activity Detection - Based on energy threshold and frequency analysis
 */
export class VoiceActivityDetector {
  private threshold: number;
  private bufferSize: number;
  private smoothingTimeConstant: number;
  private minSpeechDuration: number;
  private maxSilenceDuration: number;
  private analyser: AnalyserNode | null = null;
  private dataArray: Float32Array | null = null;
  private speechStartTime: number | null = null;
  private lastSpeechTime: number | null = null;
  private isSpeechActive = false;

  constructor(options: VADOptions = {}) {
    this.threshold = options.threshold || 0.01;
    this.bufferSize = options.bufferSize || 1024;
    this.smoothingTimeConstant = options.smoothingTimeConstant || 0.8;
    this.minSpeechDuration = options.minSpeechDuration || 100; // ms
    this.maxSilenceDuration = options.maxSilenceDuration || 500; // ms
  }

  setup(audioContext: AudioContext, source: MediaStreamAudioSourceNode): void {
    this.analyser = audioContext.createAnalyser();
    this.analyser.fftSize = this.bufferSize * 2;
    this.analyser.smoothingTimeConstant = this.smoothingTimeConstant;
    this.dataArray = new Float32Array(this.analyser.frequencyBinCount);
    
    source.connect(this.analyser);
  }

  process(): { isSpeaking: boolean; energy: number; confidence: number } {
    if (!this.analyser || !this.dataArray) {
      return { isSpeaking: false, energy: 0, confidence: 0 };
    }

    this.analyser.getFloatFrequencyData(this.dataArray as any);
    
    // Calculate energy in speech frequency range (300Hz - 3400Hz)
    const speechBinStart = Math.floor(300 / (44100 / this.analyser.fftSize));
    const speechBinEnd = Math.floor(3400 / (44100 / this.analyser.fftSize));
    
    let speechEnergy = 0;
    for (let i = speechBinStart; i < speechBinEnd; i++) {
      speechEnergy += Math.pow(10, this.dataArray[i] / 20); // Convert dB to linear
    }
    speechEnergy /= (speechBinEnd - speechBinStart);
    
    const currentTime = Date.now();
    const isSpeaking = speechEnergy > this.threshold;
    
    // Apply temporal smoothing
    if (isSpeaking) {
      if (!this.isSpeechActive) {
        this.speechStartTime = currentTime;
      }
      this.lastSpeechTime = currentTime;
    }
    
    // Determine final speech state with hysteresis
    let finalSpeechState = false;
    const timeSinceSpeechStart = this.speechStartTime ? currentTime - this.speechStartTime : 0;
    const timeSinceLastSpeech = this.lastSpeechTime ? currentTime - this.lastSpeechTime : Infinity;
    
    if (isSpeaking && timeSinceSpeechStart >= this.minSpeechDuration) {
      finalSpeechState = true;
    } else if (this.isSpeechActive && timeSinceLastSpeech <= this.maxSilenceDuration) {
      finalSpeechState = true;
    }
    
    if (!finalSpeechState) {
      this.speechStartTime = null;
    }
    
    this.isSpeechActive = finalSpeechState;
    
    const confidence = Math.min(speechEnergy / this.threshold, 1.0);
    
    return {
      isSpeaking: finalSpeechState,
      energy: speechEnergy,
      confidence
    };
  }
}

/**
 * STT Provider Interface - Abstraction for multiple speech recognition services
 */
export interface STTProvider {
  start(): Promise<void>;
  stop(): void;
  transcribe(audioBlob: Blob): Promise<string>;
  onResult(callback: (result: STTResult) => void): void;
  onError(callback: (error: Error) => void): void;
}

export interface STTResult {
  transcript: string;
  confidence: number;
  isFinal: boolean;
  timestamp: number;
}

/**
 * Browser Native STT Provider - Uses Web Speech API
 */
export class BrowserSTTProvider implements STTProvider {
  private recognition: SpeechRecognition | null = null;
  private resultCallback?: (result: STTResult) => void;
  private errorCallback?: (error: Error) => void;
  private config: STTConfig;

  constructor(config: STTConfig) {
    this.config = config;
    this.setupRecognition();
  }

  private setupRecognition(): void {
    if (!('webkitSpeechRecognition' in window) && !('SpeechRecognition' in window)) {
      throw new Error('Speech recognition not supported in this browser');
    }

    const SpeechRecognitionImpl = window.SpeechRecognition || window.webkitSpeechRecognition;
    this.recognition = new SpeechRecognitionImpl();
    
    this.recognition.continuous = this.config.continuous ?? true;
    this.recognition.interimResults = this.config.interimResults ?? true;
    this.recognition.lang = this.config.language || 'en-US';
    
    this.recognition.onresult = (event: SpeechRecognitionEvent) => {
      for (let i = event.resultIndex; i < event.results.length; i++) {
        const result = event.results[i];
        if (this.resultCallback) {
          this.resultCallback({
            transcript: result[0].transcript,
            confidence: result[0].confidence,
            isFinal: result.isFinal,
            timestamp: Date.now()
          });
        }
      }
    };
    
    this.recognition.onerror = (event: SpeechRecognitionErrorEvent) => {
      if (this.errorCallback) {
        this.errorCallback(new Error(`Speech recognition error: ${event.error}`));
      }
    };
  }

  async start(): Promise<void> {
    if (this.recognition) {
      this.recognition.start();
    }
  }

  stop(): void {
    if (this.recognition) {
      this.recognition.stop();
    }
  }

  async transcribe(audioBlob: Blob): Promise<string> {
    // For blob transcription, we'd need to implement audio processing
    throw new Error('Blob transcription not supported by browser STT');
  }

  onResult(callback: (result: STTResult) => void): void {
    this.resultCallback = callback;
  }

  onError(callback: (error: Error) => void): void {
    this.errorCallback = callback;
  }
}

/**
 * STT Provider Factory - Creates appropriate provider based on configuration
 */
export class STTProviderFactory {
  static createProvider(config: STTConfig): STTProvider {
    switch (config.provider) {
      case 'browser':
        return new BrowserSTTProvider(config);
      case 'whisper':
        // TODO: Implement WhisperAPIProvider based on Heyito patterns
        throw new Error('Whisper provider not implemented yet');
      case 'deepgram':
        // TODO: Implement DeepgramProvider based on Heyito patterns
        throw new Error('Deepgram provider not implemented yet');
      default:
        throw new Error(`Unknown STT provider: ${config.provider}`);
    }
  }
}

/**
 * Enhanced Audio Pipeline - Main orchestrator class
 */
export class EnhancedAudioPipeline {
  private audioContext: AudioContext | null = null;
  private mediaStream: MediaStream | null = null;
  private sourceNode: MediaStreamAudioSourceNode | null = null;
  private vad: VoiceActivityDetector;
  private sttProvider: STTProvider;
  private isRecording = false;
  private config: AudioPipelineConfig;
  
  // Event callbacks
  private onSpeechStart?: () => void;
  private onSpeechEnd?: () => void;
  private onTranscript?: (result: STTResult) => void;
  private onError?: (error: Error) => void;
  private onVADUpdate?: (vadResult: { isSpeaking: boolean; energy: number; confidence: number }) => void;

  constructor(config: AudioPipelineConfig) {
    this.config = config;
    this.vad = new VoiceActivityDetector(config.vad);
    this.sttProvider = STTProviderFactory.createProvider(config.stt);
    this.setupSTTCallbacks();
  }

  private setupSTTCallbacks(): void {
    this.sttProvider.onResult((result) => {
      if (this.onTranscript) {
        this.onTranscript(result);
      }
    });

    this.sttProvider.onError((error) => {
      if (this.onError) {
        this.onError(error);
      }
    });
  }

  async initialize(): Promise<void> {
    try {
      // Request microphone permission
      this.mediaStream = await navigator.mediaDevices.getUserMedia({
        audio: {
          echoCancellation: this.config.echoCancellation ?? true,
          autoGainControl: this.config.autoGainControl ?? true,
          noiseSuppression: this.config.noiseReduction ?? true,
          sampleRate: 44100,
          channelCount: 1
        }
      });

      // Setup audio context
      this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
      this.sourceNode = this.audioContext.createMediaStreamSource(this.mediaStream);
      
      // Setup VAD
      this.vad.setup(this.audioContext, this.sourceNode);
      
      console.log('Enhanced Audio Pipeline initialized successfully');
    } catch (error) {
      throw new Error(`Failed to initialize audio pipeline: ${error}`);
    }
  }

  async startRecording(): Promise<void> {
    if (!this.audioContext || !this.mediaStream) {
      throw new Error('Audio pipeline not initialized');
    }

    this.isRecording = true;
    await this.sttProvider.start();
    
    // Start VAD monitoring
    this.startVADMonitoring();
    
    console.log('Enhanced Audio Pipeline recording started');
  }

  stopRecording(): void {
    this.isRecording = false;
    this.sttProvider.stop();
    
    console.log('Enhanced Audio Pipeline recording stopped');
  }

  private startVADMonitoring(): void {
    let wasSpeaking = false;
    const monitorVAD = () => {
      if (!this.isRecording) return;
      
      const vadResult = this.vad.process();
      
      if (this.onVADUpdate) {
        this.onVADUpdate(vadResult);
      }
      
      // Trigger speech start/end events
      if (vadResult.isSpeaking && !wasSpeaking) {
        if (this.onSpeechStart) {
          this.onSpeechStart();
        }
      } else if (!vadResult.isSpeaking && wasSpeaking) {
        if (this.onSpeechEnd) {
          this.onSpeechEnd();
        }
      }
      wasSpeaking = vadResult.isSpeaking;
      
      // Continue monitoring
      requestAnimationFrame(monitorVAD);
    };
    
    monitorVAD();
  }

  // Event listener methods
  onSpeechStarted(callback: () => void): void {
    this.onSpeechStart = callback;
  }

  onSpeechEnded(callback: () => void): void {
    this.onSpeechEnd = callback;
  }

  onTranscriptReceived(callback: (result: STTResult) => void): void {
    this.onTranscript = callback;
  }

  onErrorOccurred(callback: (error: Error) => void): void {
    this.onError = callback;
  }

  onVADUpdated(callback: (vadResult: { isSpeaking: boolean; energy: number; confidence: number }) => void): void {
    this.onVADUpdate = callback;
  }

  // Utility methods
  getAudioLevel(): number {
    return this.vad.process().energy;
  }

  isSpeechActive(): boolean {
    return this.vad.process().isSpeaking;
  }

  async destroy(): Promise<void> {
    this.stopRecording();
    
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(track => track.stop());
    }
    
    if (this.audioContext) {
      await this.audioContext.close();
    }
    
    console.log('Enhanced Audio Pipeline destroyed');
  }
}