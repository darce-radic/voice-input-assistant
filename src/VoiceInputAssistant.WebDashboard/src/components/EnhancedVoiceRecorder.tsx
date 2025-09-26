import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  Card,
  CardContent,
  Button,
  Typography,
  LinearProgress,
  Box,
  Chip,
  Alert,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Switch,
  FormControlLabel,
  Paper,
  Grid
} from '@mui/material';
import {
  Mic,
  MicOff,
  VolumeUp,
  Settings,
  PlayArrow,
  Stop
} from '@mui/icons-material';
import {
  EnhancedAudioPipeline,
  AudioPipelineConfig,
  STTResult,
  STTConfig,
  VADOptions
} from '../services/audio/EnhancedAudioPipeline';

interface EnhancedVoiceRecorderProps {
  onTranscriptionReceived?: (transcript: string, confidence: number) => void;
  onError?: (error: string) => void;
  defaultConfig?: Partial<AudioPipelineConfig>;
}

export const EnhancedVoiceRecorder: React.FC<EnhancedVoiceRecorderProps> = ({
  onTranscriptionReceived,
  onError,
  defaultConfig
}) => {
  // State management
  const [isRecording, setIsRecording] = useState(false);
  const [isInitialized, setIsInitialized] = useState(false);
  const [currentTranscript, setCurrentTranscript] = useState('');
  const [finalTranscripts, setFinalTranscripts] = useState<string[]>([]);
  const [audioLevel, setAudioLevel] = useState(0);
  const [isSpeaking, setIsSpeaking] = useState(false);
  const [confidence, setConfidence] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [showSettings, setShowSettings] = useState(false);

  // Configuration state
  const [sttProvider, setSttProvider] = useState<'browser' | 'whisper' | 'deepgram'>('browser');
  const [language, setLanguage] = useState('en-US');
  const [vadThreshold, setVadThreshold] = useState(0.01);
  const [continuousMode, setContinuousMode] = useState(true);
  const [interimResults, setInterimResults] = useState(true);

  // Refs
  const pipelineRef = useRef<EnhancedAudioPipeline | null>(null);
  const animationFrameRef = useRef<number>();

  // Create pipeline configuration
  const createConfig = useCallback((): AudioPipelineConfig => {
    const vadOptions: VADOptions = {
      threshold: vadThreshold,
      bufferSize: 1024,
      smoothingTimeConstant: 0.8,
      minSpeechDuration: 100,
      maxSilenceDuration: 500,
      ...defaultConfig?.vad
    };

    const sttConfig: STTConfig = {
      provider: sttProvider,
      language,
      continuous: continuousMode,
      interimResults,
      ...defaultConfig?.stt
    };

    return {
      vad: vadOptions,
      stt: sttConfig,
      noiseReduction: true,
      echoCancellation: true,
      autoGainControl: true,
      ...defaultConfig
    };
  }, [sttProvider, language, vadThreshold, continuousMode, interimResults, defaultConfig]);

  // Initialize audio pipeline
  const initializePipeline = useCallback(async () => {
    try {
      const config = createConfig();
      const pipeline = new EnhancedAudioPipeline(config);

      // Setup event listeners
      pipeline.onTranscriptReceived((result: STTResult) => {
        if (result.isFinal) {
          setFinalTranscripts(prev => [...prev, result.transcript]);
          setCurrentTranscript('');
          if (onTranscriptionReceived) {
            onTranscriptionReceived(result.transcript, result.confidence);
          }
        } else {
          setCurrentTranscript(result.transcript);
        }
        setConfidence(result.confidence);
      });

      pipeline.onSpeechStarted(() => {
        setIsSpeaking(true);
      });

      pipeline.onSpeechEnded(() => {
        setIsSpeaking(false);
      });

      pipeline.onVADUpdated((vadResult) => {
        setAudioLevel(vadResult.energy);
        setIsSpeaking(vadResult.isSpeaking);
      });

      pipeline.onErrorOccurred((err) => {
        const errorMessage = err.message;
        setError(errorMessage);
        if (onError) {
          onError(errorMessage);
        }
      });

      await pipeline.initialize();
      pipelineRef.current = pipeline;
      setIsInitialized(true);
      setError(null);
    } catch (err) {
      const errorMessage = `Failed to initialize audio pipeline: ${err}`;
      setError(errorMessage);
      if (onError) {
        onError(errorMessage);
      }
    }
  }, [createConfig, onTranscriptionReceived, onError]);

  // Start recording
  const startRecording = useCallback(async () => {
    if (!pipelineRef.current) {
      await initializePipeline();
    }

    try {
      if (pipelineRef.current) {
        await pipelineRef.current.startRecording();
        setIsRecording(true);
        setError(null);
      }
    } catch (err) {
      const errorMessage = `Failed to start recording: ${err}`;
      setError(errorMessage);
      if (onError) {
        onError(errorMessage);
      }
    }
  }, [initializePipeline, onError]);

  // Stop recording
  const stopRecording = useCallback(() => {
    if (pipelineRef.current) {
      pipelineRef.current.stopRecording();
      setIsRecording(false);
      setIsSpeaking(false);
      setAudioLevel(0);
    }
  }, []);

  // Toggle recording
  const toggleRecording = useCallback(() => {
    if (isRecording) {
      stopRecording();
    } else {
      startRecording();
    }
  }, [isRecording, startRecording, stopRecording]);

  // Clear transcripts
  const clearTranscripts = useCallback(() => {
    setCurrentTranscript('');
    setFinalTranscripts([]);
  }, []);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (pipelineRef.current) {
        pipelineRef.current.destroy();
      }
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, []);

  // Audio level visualization
  const audioLevelPercent = Math.min(audioLevel * 100, 100);

  return (
    <Card sx={{ maxWidth: 800, margin: 'auto' }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <Typography variant="h5" component="h2">
            Enhanced Voice Recorder
          </Typography>
          <Button
            startIcon={<Settings />}
            onClick={() => setShowSettings(!showSettings)}
            variant="outlined"
            size="small"
          >
            Settings
          </Button>
        </Box>

        {/* Error Display */}
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {/* Settings Panel */}
        {showSettings && (
          <Paper sx={{ p: 2, mb: 2, bgcolor: 'grey.50' }}>
            <Typography variant="h6" gutterBottom>
              Configuration
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <FormControl fullWidth>
                  <InputLabel>STT Provider</InputLabel>
                  <Select
                    value={sttProvider}
                    onChange={(e) => setSttProvider(e.target.value as any)}
                    disabled={isRecording}
                  >
                    <MenuItem value="browser">Browser Native</MenuItem>
                    <MenuItem value="whisper">Whisper API</MenuItem>
                    <MenuItem value="deepgram">Deepgram</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} sm={6}>
                <FormControl fullWidth>
                  <InputLabel>Language</InputLabel>
                  <Select
                    value={language}
                    onChange={(e) => setLanguage(e.target.value)}
                    disabled={isRecording}
                  >
                    <MenuItem value="en-US">English (US)</MenuItem>
                    <MenuItem value="en-GB">English (UK)</MenuItem>
                    <MenuItem value="es-ES">Spanish</MenuItem>
                    <MenuItem value="fr-FR">French</MenuItem>
                    <MenuItem value="de-DE">German</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12}>
                <Typography gutterBottom>VAD Threshold: {vadThreshold}</Typography>
                <input
                  type="range"
                  min="0.001"
                  max="0.1"
                  step="0.001"
                  value={vadThreshold}
                  onChange={(e) => setVadThreshold(parseFloat(e.target.value))}
                  disabled={isRecording}
                  style={{ width: '100%' }}
                />
              </Grid>
              <Grid item xs={12}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={continuousMode}
                      onChange={(e) => setContinuousMode(e.target.checked)}
                      disabled={isRecording}
                    />
                  }
                  label="Continuous Recognition"
                />
                <FormControlLabel
                  control={
                    <Switch
                      checked={interimResults}
                      onChange={(e) => setInterimResults(e.target.checked)}
                      disabled={isRecording}
                    />
                  }
                  label="Show Interim Results"
                />
              </Grid>
            </Grid>
          </Paper>
        )}

        {/* Status Indicators */}
        <Box display="flex" gap={1} mb={2}>
          <Chip
            icon={isRecording ? <Mic /> : <MicOff />}
            label={isRecording ? 'Recording' : 'Stopped'}
            color={isRecording ? 'success' : 'default'}
            variant={isRecording ? 'filled' : 'outlined'}
          />
          <Chip
            icon={<VolumeUp />}
            label={isSpeaking ? 'Speaking' : 'Silent'}
            color={isSpeaking ? 'warning' : 'default'}
            variant={isSpeaking ? 'filled' : 'outlined'}
          />
          {confidence > 0 && (
            <Chip
              label={`Confidence: ${(confidence * 100).toFixed(0)}%`}
              color={confidence > 0.7 ? 'success' : confidence > 0.4 ? 'warning' : 'error'}
              variant="outlined"
            />
          )}
        </Box>

        {/* Audio Level Visualization */}
        <Box mb={2}>
          <Typography variant="body2" gutterBottom>
            Audio Level
          </Typography>
          <LinearProgress
            variant="determinate"
            value={audioLevelPercent}
            color={isSpeaking ? 'success' : 'primary'}
            sx={{ height: 8, borderRadius: 4 }}
          />
        </Box>

        {/* Control Buttons */}
        <Box display="flex" gap={2} justifyContent="center" mb={3}>
          <Button
            variant="contained"
            color={isRecording ? 'error' : 'primary'}
            size="large"
            startIcon={isRecording ? <Stop /> : <PlayArrow />}
            onClick={toggleRecording}
            disabled={!isInitialized && !isRecording}
          >
            {isRecording ? 'Stop Recording' : 'Start Recording'}
          </Button>
          <Button
            variant="outlined"
            onClick={clearTranscripts}
            disabled={finalTranscripts.length === 0 && !currentTranscript}
          >
            Clear
          </Button>
        </Box>

        {/* Current (Interim) Transcript */}
        {currentTranscript && (
          <Paper sx={{ p: 2, mb: 2, bgcolor: 'info.light', color: 'info.contrastText' }}>
            <Typography variant="body2" color="inherit">
              <em>{currentTranscript}</em>
            </Typography>
          </Paper>
        )}

        {/* Final Transcripts */}
        {finalTranscripts.length > 0 && (
          <Paper sx={{ p: 2, maxHeight: 200, overflow: 'auto' }}>
            <Typography variant="h6" gutterBottom>
              Transcripts
            </Typography>
            {finalTranscripts.map((transcript, index) => (
              <Typography key={index} variant="body1" paragraph>
                <strong>{index + 1}:</strong> {transcript}
              </Typography>
            ))}
          </Paper>
        )}

        {/* Usage Instructions */}
        {!isRecording && finalTranscripts.length === 0 && (
          <Paper sx={{ p: 2, bgcolor: 'grey.50' }}>
            <Typography variant="body2" color="text.secondary">
              💡 <strong>Enhanced Features:</strong>
              <br />• Advanced Voice Activity Detection (VAD) reduces false triggers
              <br />• Multiple STT provider support (Browser, Whisper, Deepgram)
              <br />• Real-time audio level monitoring and speech detection
              <br />• Configurable sensitivity and language settings
              <br />• Inspired by Heyito voice tool patterns
            </Typography>
          </Paper>
        )}
      </CardContent>
    </Card>
  );
};

export default EnhancedVoiceRecorder;