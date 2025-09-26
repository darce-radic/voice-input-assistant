'use client';

import React, { useState, useRef, useEffect } from 'react';
import { api } from '../../services/api';

interface VoiceRecognitionWidgetProps {}

export function VoiceRecognitionWidget({}: VoiceRecognitionWidgetProps) {
  const [isRecording, setIsRecording] = useState(false);
  const [transcript, setTranscript] = useState('');
  const [isListening, setIsListening] = useState(false);
  const [selectedEngine, setSelectedEngine] = useState('whisper-local');
  const [confidence, setConfidence] = useState(0);
  
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);

  const engines = [
    { id: 'whisper-local', name: 'Whisper Local', status: 'online' },
    { id: 'azure-speech', name: 'Azure Speech', status: 'online' },
    { id: 'openai-whisper', name: 'OpenAI Whisper', status: 'online' },
    { id: 'google-speech', name: 'Google Speech', status: 'offline' },
  ];

  useEffect(() => {
    // Cleanup on unmount
    return () => {
      if (mediaRecorderRef.current && isRecording) {
        mediaRecorderRef.current.stop();
      }
    };
  }, [isRecording]);

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ 
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          sampleRate: 16000,
        }
      });

      const mediaRecorder = new MediaRecorder(stream, {
        mimeType: 'audio/webm;codecs=opus'
      });

      mediaRecorderRef.current = mediaRecorder;
      audioChunksRef.current = [];

      mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          audioChunksRef.current.push(event.data);
        }
      };

      mediaRecorder.onstop = async () => {
        const audioBlob = new Blob(audioChunksRef.current, { type: 'audio/webm' });
        await processAudio(audioBlob);
        
        // Stop all audio tracks
        stream.getTracks().forEach(track => track.stop());
      };

      mediaRecorder.start(100); // Collect data every 100ms
      setIsRecording(true);
      setIsListening(true);
      setTranscript('Listening...');

      // Simulate real-time transcript updates
      const intervalId = setInterval(() => {
        if (isRecording) {
          const phrases = [
            'Hello, this is a test.',
            'The voice recognition is working well.',
            'Speech-to-text conversion in progress.',
            'Testing multiple speech engines.',
          ];
          const randomPhrase = phrases[Math.floor(Math.random() * phrases.length)];
          setTranscript(randomPhrase);
          setConfidence(Math.random() * 30 + 70); // 70-100% confidence
        }
      }, 2000);

      // Auto-stop after 30 seconds
      setTimeout(() => {
        if (mediaRecorderRef.current && isRecording) {
          stopRecording();
        }
        clearInterval(intervalId);
      }, 30000);

    } catch (error) {
      console.error('Error accessing microphone:', error);
      alert('Could not access microphone. Please check permissions.');
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      setIsRecording(false);
      setIsListening(false);
    }
  };

  const processAudio = async (audioBlob: Blob) => {
    try {
      setTranscript('Processing audio...');
      
      // Call Netlify Function for speech recognition
      const result = await api.transcribeAudio(audioBlob, selectedEngine);
      setTranscript(result.text);
      setConfidence(result.confidence);

    } catch (error) {
      console.error('Error processing audio:', error);
      setTranscript('Error processing audio. Please try again.');
      setConfidence(0);
    }
  };

  const getEngineStatusColor = (status: string) => {
    switch (status) {
      case 'online': return 'bg-success-500';
      case 'offline': return 'bg-error-500';
      default: return 'bg-warning-500';
    }
  };

  return (
    <div className="card p-6">
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-lg font-semibold">Voice Recognition</h3>
        <div className="flex items-center space-x-2">
          <div className={`w-2 h-2 rounded-full ${isRecording ? 'bg-error-500 recording-indicator' : 'bg-muted'}`} />
          <span className="text-xs text-muted-foreground">
            {isRecording ? 'Recording' : 'Ready'}
          </span>
        </div>
      </div>

      {/* Engine Selection */}
      <div className="mb-6">
        <label className="block text-sm font-medium text-muted-foreground mb-2">
          Speech Engine
        </label>
        <select
          value={selectedEngine}
          onChange={(e) => setSelectedEngine(e.target.value)}
          disabled={isRecording}
          className="w-full px-3 py-2 border border-input bg-background rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent"
        >
          {engines.map((engine) => (
            <option key={engine.id} value={engine.id} disabled={engine.status === 'offline'}>
              {engine.name} ({engine.status})
            </option>
          ))}
        </select>
      </div>

      {/* Voice Activity Visualization */}
      {isListening && (
        <div className="flex items-center justify-center space-x-1 mb-6">
          {[...Array(5)].map((_, i) => (
            <div
              key={i}
              className="w-1 h-8 bg-primary rounded-full voice-bar"
              style={{ animationDelay: `${i * 0.1}s` }}
            />
          ))}
        </div>
      )}

      {/* Transcript Display */}
      <div className="bg-accent/30 rounded-lg p-4 mb-6 min-h-[120px]">
        <div className="flex items-start justify-between mb-2">
          <span className="text-xs text-muted-foreground font-medium">Transcript</span>
          {confidence > 0 && (
            <span className="text-xs text-muted-foreground">
              Confidence: {confidence.toFixed(1)}%
            </span>
          )}
        </div>
        <p className="text-sm leading-relaxed">
          {transcript || 'Click "Start Recording" to begin voice recognition...'}
        </p>
      </div>

      {/* Controls */}
      <div className="flex items-center space-x-3">
        <button
          onClick={isRecording ? stopRecording : startRecording}
          disabled={selectedEngine === 'google-speech'}
          className={`flex-1 btn ${
            isRecording 
              ? 'bg-error-500 hover:bg-error-600 text-white' 
              : 'btn-primary'
          } px-4 py-3 text-sm font-medium`}
        >
          <svg className="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            {isRecording ? (
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 6h12v12H6z" />
            ) : (
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 11a7 7 0 01-7 7m0 0a7 7 0 01-7-7m7 7v4m0 0H8m4 0h4m-4-8a3 3 0 01-3-3V5a3 3 0 016 0v6a3 3 0 01-3 3z" />
            )}
          </svg>
          {isRecording ? 'Stop Recording' : 'Start Recording'}
        </button>

        <button
          onClick={() => setTranscript('')}
          disabled={isRecording}
          className="btn btn-ghost px-4 py-3 text-sm"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
        </button>
      </div>

      {/* Engine Status */}
      <div className="mt-6 pt-6 border-t">
        <h4 className="text-sm font-medium text-muted-foreground mb-3">Engine Status</h4>
        <div className="grid grid-cols-2 gap-2">
          {engines.map((engine) => (
            <div key={engine.id} className="flex items-center space-x-2 text-xs">
              <div className={`w-2 h-2 rounded-full ${getEngineStatusColor(engine.status)}`} />
              <span className="truncate">{engine.name}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}