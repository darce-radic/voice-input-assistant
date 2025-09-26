/**
 * Machine Learning Service for on-device AI processing
 * Includes voice activity detection, noise reduction, sentiment analysis, and more
 */

interface MLModel {
  name: string;
  version: string;
  size: number;
  loaded: boolean;
  accuracy: number;
}

interface VoiceActivityResult {
  isVoiceActive: boolean;
  confidence: number;
  energyLevel: number;
  spectralCentroid: number;
}

interface SentimentResult {
  sentiment: 'positive' | 'negative' | 'neutral';
  confidence: number;
  scores: {
    positive: number;
    negative: number;
    neutral: number;
  };
}

interface SpeakerIdentification {
  speakerId: string;
  confidence: number;
  voiceprint: Float32Array;
  characteristics: {
    pitch: number;
    tone: number;
    timbre: number;
    cadence: number;
  };
}

interface EmotionResult {
  emotion: 'happy' | 'sad' | 'angry' | 'neutral' | 'excited' | 'calm' | 'stressed';
  confidence: number;
  arousal: number; // 0-1 (calm to excited)
  valence: number; // 0-1 (negative to positive)
}

interface AudioFeatures {
  mfcc: Float32Array;
  spectralFeatures: {
    centroid: number;
    rolloff: number;
    flux: number;
    zcr: number; // Zero crossing rate
  };
  temporalFeatures: {
    rms: number;
    energy: number;
    onset: number[];
  };
}

class MLService {
  private models: Map<string, MLModel> = new Map();
  private audioContext: AudioContext | null = null;
  private vadModel: any = null; // Voice Activity Detection model
  private noiseReductionModel: any = null;
  private sentimentModel: any = null;
  private speakerModel: any = null;
  private emotionModel: any = null;
  
  private isInitialized = false;
  private workers: Map<string, Worker> = new Map();

  constructor() {
    this.init();
  }

  /**
   * Initialize the ML service
   */
  async init(): Promise<void> {
    try {
      // Check for WebAssembly support
      if (typeof WebAssembly === 'undefined') {
        console.warn('WebAssembly not supported - ML features will be limited');
      }

      // Initialize audio context
      this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();

      // Initialize web workers for heavy computations
      this.initializeWorkers();

      // Load basic models
      await this.loadEssentialModels();

      this.isInitialized = true;
      console.log('ML Service initialized successfully');
    } catch (error) {
      console.error('Failed to initialize ML Service:', error);
    }
  }

  /**
   * Initialize web workers for ML computations
   */
  private initializeWorkers(): void {
    // VAD Worker
    const vadWorkerCode = `
      self.onmessage = function(e) {
        const { audioData, sampleRate } = e.data;
        const result = performVAD(audioData, sampleRate);
        self.postMessage({ type: 'vad-result', result });
      };
      
      function performVAD(audioData, sampleRate) {
        // Simple energy-based VAD
        const frameSize = 1024;
        const frames = [];
        
        for (let i = 0; i < audioData.length - frameSize; i += frameSize) {
          const frame = audioData.slice(i, i + frameSize);
          const energy = calculateEnergy(frame);
          const zcr = calculateZCR(frame);
          const spectralCentroid = calculateSpectralCentroid(frame, sampleRate);
          
          frames.push({
            energy,
            zcr,
            spectralCentroid,
            isVoice: energy > 0.01 && zcr < 0.3 && spectralCentroid > 500
          });
        }
        
        const avgEnergy = frames.reduce((sum, f) => sum + f.energy, 0) / frames.length;
        const voiceFrames = frames.filter(f => f.isVoice).length;
        const voiceRatio = voiceFrames / frames.length;
        
        return {
          isVoiceActive: voiceRatio > 0.3,
          confidence: Math.min(voiceRatio * 2, 1),
          energyLevel: avgEnergy,
          spectralCentroid: frames.reduce((sum, f) => sum + f.spectralCentroid, 0) / frames.length
        };
      }
      
      function calculateEnergy(frame) {
        return frame.reduce((sum, sample) => sum + sample * sample, 0) / frame.length;
      }
      
      function calculateZCR(frame) {
        let crossings = 0;
        for (let i = 1; i < frame.length; i++) {
          if ((frame[i] >= 0) !== (frame[i-1] >= 0)) crossings++;
        }
        return crossings / frame.length;
      }
      
      function calculateSpectralCentroid(frame, sampleRate) {
        // Simple approximation using FFT-like approach
        let numerator = 0, denominator = 0;
        for (let i = 0; i < frame.length / 2; i++) {
          const freq = i * sampleRate / frame.length;
          const magnitude = Math.abs(frame[i]);
          numerator += freq * magnitude;
          denominator += magnitude;
        }
        return denominator > 0 ? numerator / denominator : 0;
      }
    `;

    const vadBlob = new Blob([vadWorkerCode], { type: 'application/javascript' });
    this.workers.set('vad', new Worker(URL.createObjectURL(vadBlob)));

    // Noise Reduction Worker
    const noiseReductionWorkerCode = `
      self.onmessage = function(e) {
        const { audioData, noiseProfile } = e.data;
        const cleanedAudio = spectralSubtraction(audioData, noiseProfile);
        self.postMessage({ type: 'noise-reduction-result', result: cleanedAudio });
      };
      
      function spectralSubtraction(signal, noiseProfile) {
        // Simple spectral subtraction implementation
        const alpha = 2.0; // Over-subtraction factor
        const result = new Float32Array(signal.length);
        
        // Apply spectral subtraction (simplified)
        for (let i = 0; i < signal.length; i++) {
          const noiseMagnitude = noiseProfile[i % noiseProfile.length];
          const signalMagnitude = Math.abs(signal[i]);
          const cleanMagnitude = Math.max(signalMagnitude - alpha * noiseMagnitude, 
                                        0.1 * signalMagnitude);
          result[i] = signal[i] >= 0 ? cleanMagnitude : -cleanMagnitude;
        }
        
        return result;
      }
    `;

    const noiseBlob = new Blob([noiseReductionWorkerCode], { type: 'application/javascript' });
    this.workers.set('noise-reduction', new Worker(URL.createObjectURL(noiseBlob)));
  }

  /**
   * Load essential ML models
   */
  private async loadEssentialModels(): Promise<void> {
    const models = [
      {
        name: 'vad',
        url: '/models/vad-model.json',
        description: 'Voice Activity Detection'
      },
      {
        name: 'sentiment',
        url: '/models/sentiment-model.json',
        description: 'Sentiment Analysis'
      }
    ];

    for (const model of models) {
      try {
        await this.loadModel(model.name, model.url);
      } catch (error) {
        console.warn(`Failed to load ${model.description} model:`, error);
        // Create fallback model
        this.createFallbackModel(model.name);
      }
    }
  }

  /**
   * Load a machine learning model
   */
  async loadModel(name: string, url: string): Promise<void> {
    try {
      console.log(`Loading model: ${name}`);
      
      // For demo purposes, we'll simulate model loading
      // In a real implementation, you would load TensorFlow.js, ONNX.js, or WebAssembly models
      
      const modelInfo: MLModel = {
        name,
        version: '1.0.0',
        size: Math.floor(Math.random() * 10000000), // Random size for demo
        loaded: true,
        accuracy: 0.85 + Math.random() * 0.1 // Random accuracy for demo
      };

      this.models.set(name, modelInfo);
      console.log(`Model ${name} loaded successfully`);
    } catch (error) {
      console.error(`Failed to load model ${name}:`, error);
      throw error;
    }
  }

  /**
   * Create fallback model for when real models fail to load
   */
  private createFallbackModel(name: string): void {
    const fallbackModel: MLModel = {
      name,
      version: '1.0.0-fallback',
      size: 1024,
      loaded: true,
      accuracy: 0.7 // Lower accuracy for fallback
    };

    this.models.set(name, fallbackModel);
    console.log(`Created fallback model for: ${name}`);
  }

  /**
   * Voice Activity Detection
   */
  async detectVoiceActivity(audioData: Float32Array, sampleRate: number = 44100): Promise<VoiceActivityResult> {
    if (!this.isInitialized) {
      await this.init();
    }

    return new Promise((resolve) => {
      const vadWorker = this.workers.get('vad');
      if (!vadWorker) {
        // Fallback implementation
        resolve(this.fallbackVAD(audioData));
        return;
      }

      vadWorker.onmessage = (e) => {
        if (e.data.type === 'vad-result') {
          resolve(e.data.result);
        }
      };

      vadWorker.postMessage({ audioData, sampleRate });
    });
  }

  /**
   * Fallback VAD implementation
   */
  private fallbackVAD(audioData: Float32Array): VoiceActivityResult {
    const energy = audioData.reduce((sum, sample) => sum + sample * sample, 0) / audioData.length;
    const threshold = 0.01;
    
    return {
      isVoiceActive: energy > threshold,
      confidence: Math.min(energy * 50, 1),
      energyLevel: energy,
      spectralCentroid: 1000 // Default value
    };
  }

  /**
   * Noise reduction using spectral subtraction
   */
  async reduceNoise(audioData: Float32Array, noiseProfile?: Float32Array): Promise<Float32Array> {
    if (!noiseProfile) {
      // Use first 100ms as noise profile
      const noiseLength = Math.min(4410, audioData.length); // 100ms at 44.1kHz
      noiseProfile = audioData.slice(0, noiseLength);
    }

    return new Promise((resolve) => {
      const worker = this.workers.get('noise-reduction');
      if (!worker) {
        // Simple fallback: apply basic high-pass filter
        resolve(this.applyHighPassFilter(audioData));
        return;
      }

      worker.onmessage = (e) => {
        if (e.data.type === 'noise-reduction-result') {
          resolve(e.data.result);
        }
      };

      worker.postMessage({ audioData, noiseProfile });
    });
  }

  /**
   * Simple high-pass filter fallback
   */
  private applyHighPassFilter(audioData: Float32Array, cutoff: number = 80): Float32Array {
    const result = new Float32Array(audioData.length);
    const alpha = cutoff / (cutoff + 44100 / (2 * Math.PI));
    
    result[0] = audioData[0];
    for (let i = 1; i < audioData.length; i++) {
      result[i] = alpha * result[i-1] + alpha * (audioData[i] - audioData[i-1]);
    }
    
    return result;
  }

  /**
   * Sentiment analysis on transcribed text
   */
  async analyzeSentiment(text: string): Promise<SentimentResult> {
    if (!text.trim()) {
      return {
        sentiment: 'neutral',
        confidence: 0,
        scores: { positive: 0.33, negative: 0.33, neutral: 0.34 }
      };
    }

    // Simple rule-based sentiment analysis (fallback)
    const positiveWords = ['good', 'great', 'excellent', 'amazing', 'wonderful', 'fantastic', 'love', 'like', 'happy', 'pleased'];
    const negativeWords = ['bad', 'terrible', 'awful', 'horrible', 'hate', 'dislike', 'sad', 'angry', 'frustrated', 'disappointed'];
    
    const words = text.toLowerCase().split(/\s+/);
    let positiveScore = 0;
    let negativeScore = 0;
    
    words.forEach(word => {
      if (positiveWords.includes(word)) positiveScore++;
      if (negativeWords.includes(word)) negativeScore++;
    });
    
    const total = positiveScore + negativeScore;
    const neutral = Math.max(0, words.length - total) / words.length;
    
    let sentiment: 'positive' | 'negative' | 'neutral' = 'neutral';
    let confidence = 0;
    
    if (positiveScore > negativeScore) {
      sentiment = 'positive';
      confidence = positiveScore / words.length;
    } else if (negativeScore > positiveScore) {
      sentiment = 'negative';
      confidence = negativeScore / words.length;
    } else {
      confidence = neutral;
    }

    return {
      sentiment,
      confidence: Math.min(confidence * 3, 1), // Boost confidence
      scores: {
        positive: total > 0 ? positiveScore / total : 0.33,
        negative: total > 0 ? negativeScore / total : 0.33,
        neutral: total > 0 ? neutral : 0.34
      }
    };
  }

  /**
   * Speaker identification and voice printing
   */
  async identifySpeaker(audioData: Float32Array): Promise<SpeakerIdentification> {
    const features = await this.extractAudioFeatures(audioData);
    
    // Simplified speaker identification based on audio features
    const speakerId = this.generateSpeakerId(features);
    const voiceprint = this.generateVoiceprint(features);
    
    return {
      speakerId,
      confidence: 0.75 + Math.random() * 0.2, // Demo confidence
      voiceprint,
      characteristics: {
        pitch: this.estimatePitch(audioData),
        tone: features.spectralFeatures.centroid / 4000, // Normalized
        timbre: features.spectralFeatures.rolloff / 8000, // Normalized
        cadence: features.temporalFeatures.onset.length / (audioData.length / 44100) // Onsets per second
      }
    };
  }

  /**
   * Emotion recognition from voice
   */
  async recognizeEmotion(audioData: Float32Array): Promise<EmotionResult> {
    const features = await this.extractAudioFeatures(audioData);
    
    // Simple emotion recognition based on audio characteristics
    const energy = features.temporalFeatures.energy;
    const pitch = this.estimatePitch(audioData);
    const zcr = this.calculateZCR(audioData);
    
    // Map features to emotions (simplified approach)
    let emotion: EmotionResult['emotion'] = 'neutral';
    let arousal = 0.5;
    let valence = 0.5;
    
    if (energy > 0.1 && pitch > 200) {
      emotion = zcr > 0.1 ? 'excited' : 'happy';
      arousal = 0.8;
      valence = 0.8;
    } else if (energy < 0.05 && pitch < 150) {
      emotion = 'sad';
      arousal = 0.2;
      valence = 0.2;
    } else if (energy > 0.15 && zcr > 0.15) {
      emotion = 'angry';
      arousal = 0.9;
      valence = 0.1;
    } else if (energy < 0.03) {
      emotion = 'calm';
      arousal = 0.1;
      valence = 0.6;
    } else if (energy > 0.2) {
      emotion = 'stressed';
      arousal = 0.9;
      valence = 0.3;
    }
    
    return {
      emotion,
      confidence: 0.6 + Math.random() * 0.3,
      arousal,
      valence
    };
  }

  /**
   * Extract comprehensive audio features
   */
  async extractAudioFeatures(audioData: Float32Array): Promise<AudioFeatures> {
    const mfcc = this.calculateMFCC(audioData);
    const spectralFeatures = this.calculateSpectralFeatures(audioData);
    const temporalFeatures = this.calculateTemporalFeatures(audioData);
    
    return {
      mfcc,
      spectralFeatures,
      temporalFeatures
    };
  }

  /**
   * Calculate MFCC (Mel-frequency cepstral coefficients)
   */
  private calculateMFCC(audioData: Float32Array, numCoeffs: number = 13): Float32Array {
    // Simplified MFCC calculation
    // In a real implementation, you'd use proper FFT and mel filter banks
    const mfcc = new Float32Array(numCoeffs);
    const frameSize = 1024;
    
    for (let i = 0; i < numCoeffs; i++) {
      let sum = 0;
      for (let j = 0; j < Math.min(frameSize, audioData.length); j++) {
        sum += audioData[j] * Math.cos((Math.PI * i * (j + 0.5)) / frameSize);
      }
      mfcc[i] = sum / frameSize;
    }
    
    return mfcc;
  }

  /**
   * Calculate spectral features
   */
  private calculateSpectralFeatures(audioData: Float32Array) {
    const centroid = this.calculateSpectralCentroid(audioData);
    const rolloff = this.calculateSpectralRolloff(audioData);
    const flux = this.calculateSpectralFlux(audioData);
    const zcr = this.calculateZCR(audioData);
    
    return { centroid, rolloff, flux, zcr };
  }

  /**
   * Calculate temporal features
   */
  private calculateTemporalFeatures(audioData: Float32Array) {
    const rms = Math.sqrt(audioData.reduce((sum, x) => sum + x * x, 0) / audioData.length);
    const energy = audioData.reduce((sum, x) => sum + x * x, 0) / audioData.length;
    const onset = this.detectOnsets(audioData);
    
    return { rms, energy, onset };
  }

  /**
   * Calculate spectral centroid
   */
  private calculateSpectralCentroid(audioData: Float32Array, sampleRate: number = 44100): number {
    let numerator = 0;
    let denominator = 0;
    
    for (let i = 0; i < audioData.length / 2; i++) {
      const freq = i * sampleRate / audioData.length;
      const magnitude = Math.abs(audioData[i]);
      numerator += freq * magnitude;
      denominator += magnitude;
    }
    
    return denominator > 0 ? numerator / denominator : 0;
  }

  /**
   * Calculate spectral rolloff
   */
  private calculateSpectralRolloff(audioData: Float32Array, threshold: number = 0.85): number {
    const magnitudes = audioData.map(x => Math.abs(x));
    const totalEnergy = magnitudes.reduce((sum, mag) => sum + mag * mag, 0);
    const targetEnergy = totalEnergy * threshold;
    
    let cumulativeEnergy = 0;
    for (let i = 0; i < magnitudes.length; i++) {
      cumulativeEnergy += magnitudes[i] * magnitudes[i];
      if (cumulativeEnergy >= targetEnergy) {
        return i * 44100 / audioData.length; // Convert bin to frequency
      }
    }
    
    return 0;
  }

  /**
   * Calculate spectral flux
   */
  private calculateSpectralFlux(audioData: Float32Array): number {
    const frameSize = 1024;
    let flux = 0;
    
    for (let i = frameSize; i < audioData.length - frameSize; i += frameSize) {
      const frame1 = audioData.slice(i - frameSize, i);
      const frame2 = audioData.slice(i, i + frameSize);
      
      let diff = 0;
      for (let j = 0; j < frameSize; j++) {
        diff += Math.pow(Math.abs(frame2[j]) - Math.abs(frame1[j]), 2);
      }
      flux += Math.sqrt(diff);
    }
    
    return flux / (audioData.length / frameSize);
  }

  /**
   * Calculate zero crossing rate
   */
  private calculateZCR(audioData: Float32Array): number {
    let crossings = 0;
    for (let i = 1; i < audioData.length; i++) {
      if ((audioData[i] >= 0) !== (audioData[i-1] >= 0)) {
        crossings++;
      }
    }
    return crossings / audioData.length;
  }

  /**
   * Detect onset points in audio
   */
  private detectOnsets(audioData: Float32Array): number[] {
    const onsets: number[] = [];
    const frameSize = 1024;
    const threshold = 0.1;
    
    for (let i = frameSize; i < audioData.length - frameSize; i += frameSize) {
      const frame = audioData.slice(i, i + frameSize);
      const energy = frame.reduce((sum, x) => sum + x * x, 0) / frameSize;
      
      if (energy > threshold) {
        onsets.push(i);
      }
    }
    
    return onsets;
  }

  /**
   * Estimate fundamental frequency (pitch)
   */
  private estimatePitch(audioData: Float32Array): number {
    // Simple autocorrelation-based pitch estimation
    const minPeriod = 80; // ~550 Hz
    const maxPeriod = 400; // ~110 Hz
    
    let bestPeriod = 0;
    let bestCorrelation = 0;
    
    for (let period = minPeriod; period <= maxPeriod; period++) {
      let correlation = 0;
      let normalizer = 0;
      
      for (let i = 0; i < audioData.length - period; i++) {
        correlation += audioData[i] * audioData[i + period];
        normalizer += audioData[i] * audioData[i];
      }
      
      if (normalizer > 0) {
        correlation /= normalizer;
        if (correlation > bestCorrelation) {
          bestCorrelation = correlation;
          bestPeriod = period;
        }
      }
    }
    
    return bestPeriod > 0 ? 44100 / bestPeriod : 0;
  }

  /**
   * Generate speaker ID based on voice characteristics
   */
  private generateSpeakerId(features: AudioFeatures): string {
    const hash = this.hashFeatures(features);
    return `speaker-${hash.toString(16)}`;
  }

  /**
   * Generate voice print
   */
  private generateVoiceprint(features: AudioFeatures): Float32Array {
    // Combine MFCC with spectral features
    const voiceprint = new Float32Array(20);
    
    // Copy MFCC coefficients
    for (let i = 0; i < Math.min(13, features.mfcc.length); i++) {
      voiceprint[i] = features.mfcc[i];
    }
    
    // Add spectral features
    voiceprint[13] = features.spectralFeatures.centroid / 4000;
    voiceprint[14] = features.spectralFeatures.rolloff / 8000;
    voiceprint[15] = features.spectralFeatures.flux;
    voiceprint[16] = features.spectralFeatures.zcr;
    voiceprint[17] = features.temporalFeatures.rms;
    voiceprint[18] = features.temporalFeatures.energy;
    voiceprint[19] = features.temporalFeatures.onset.length / 100; // Normalized
    
    return voiceprint;
  }

  /**
   * Hash audio features for speaker identification
   */
  private hashFeatures(features: AudioFeatures): number {
    let hash = 0;
    const str = features.mfcc.join(',') + features.spectralFeatures.centroid;
    
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    
    return Math.abs(hash);
  }

  /**
   * Get loaded models info
   */
  getLoadedModels(): MLModel[] {
    return Array.from(this.models.values());
  }

  /**
   * Check if service is ready
   */
  isReady(): boolean {
    return this.isInitialized && this.models.size > 0;
  }

  /**
   * Get model info
   */
  getModelInfo(name: string): MLModel | null {
    return this.models.get(name) || null;
  }

  /**
   * Cleanup resources
   */
  dispose(): void {
    // Terminate workers
    this.workers.forEach(worker => worker.terminate());
    this.workers.clear();
    
    // Close audio context
    if (this.audioContext && this.audioContext.state !== 'closed') {
      this.audioContext.close();
    }
    
    this.models.clear();
    this.isInitialized = false;
  }
}

export default MLService;