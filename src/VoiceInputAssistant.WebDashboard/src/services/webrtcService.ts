/**
 * WebRTC Service for real-time voice communication and streaming
 * Supports peer-to-peer audio/video calls, screen sharing, and real-time data channels
 */

export interface WebRTCConfig {
  iceServers: RTCIceServer[];
  enableAudio: boolean;
  enableVideo: boolean;
  enableDataChannel: boolean;
  audioConstraints?: MediaTrackConstraints;
  videoConstraints?: MediaTrackConstraints;
}

export interface CallSession {
  id: string;
  remoteUserId: string;
  status: 'connecting' | 'connected' | 'disconnected' | 'failed';
  startTime: Date;
  duration?: number;
  type: 'audio' | 'video' | 'screen-share';
}

export interface DataChannelMessage {
  type: 'text' | 'file' | 'voice-data' | 'transcription';
  payload: any;
  timestamp: number;
  senderId: string;
}

class WebRTCService {
  private peerConnection: RTCPeerConnection | null = null;
  private localStream: MediaStream | null = null;
  private remoteStream: MediaStream | null = null;
  private dataChannel: RTCDataChannel | null = null;
  private currentSession: CallSession | null = null;
  private eventListeners: Map<string, Function[]> = new Map();
  
  private defaultConfig: WebRTCConfig = {
    iceServers: [
      { urls: 'stun:stun.l.google.com:19302' },
      { urls: 'stun:stun1.l.google.com:19302' },
      // Add TURN servers for production
      // { urls: 'turn:your-turn-server.com:3478', username: 'user', credential: 'pass' }
    ],
    enableAudio: true,
    enableVideo: false,
    enableDataChannel: true,
    audioConstraints: {
      echoCancellation: true,
      noiseSuppression: true,
      autoGainControl: true,
      sampleRate: 44100,
      channelCount: 1
    }
  };

  constructor(private config: Partial<WebRTCConfig> = {}) {
    this.config = { ...this.defaultConfig, ...config };
  }

  /**
   * Initialize a new peer connection
   */
  private async initializePeerConnection(): Promise<RTCPeerConnection> {
    if (this.peerConnection) {
      this.closePeerConnection();
    }

    this.peerConnection = new RTCPeerConnection({
      iceServers: this.config.iceServers
    });

    // Set up event handlers
    this.setupPeerConnectionEvents();

    // Create data channel if enabled
    if (this.config.enableDataChannel) {
      this.dataChannel = this.peerConnection.createDataChannel('voice-assistant', {
        ordered: true,
        maxRetransmits: 3
      });
      this.setupDataChannelEvents(this.dataChannel);
    }

    return this.peerConnection;
  }

  /**
   * Set up peer connection event handlers
   */
  private setupPeerConnectionEvents(): void {
    if (!this.peerConnection) return;

    this.peerConnection.onicecandidate = (event) => {
      if (event.candidate) {
        this.emit('iceCandidate', event.candidate);
      }
    };

    this.peerConnection.oniceconnectionstatechange = () => {
      const state = this.peerConnection?.iceConnectionState;
      console.log('ICE connection state changed:', state);
      
      if (this.currentSession) {
        switch (state) {
          case 'connected':
            this.currentSession.status = 'connected';
            this.emit('callConnected', this.currentSession);
            break;
          case 'disconnected':
          case 'failed':
            this.currentSession.status = state === 'failed' ? 'failed' : 'disconnected';
            this.emit('callDisconnected', this.currentSession);
            break;
        }
      }
    };

    this.peerConnection.ontrack = (event) => {
      console.log('Received remote track:', event.track.kind);
      this.remoteStream = event.streams[0];
      this.emit('remoteStream', this.remoteStream);
    };

    this.peerConnection.ondatachannel = (event) => {
      const channel = event.channel;
      console.log('Received data channel:', channel.label);
      this.setupDataChannelEvents(channel);
    };
  }

  /**
   * Set up data channel event handlers
   */
  private setupDataChannelEvents(channel: RTCDataChannel): void {
    channel.onopen = () => {
      console.log('Data channel opened:', channel.label);
      this.emit('dataChannelOpen', channel);
    };

    channel.onclose = () => {
      console.log('Data channel closed:', channel.label);
      this.emit('dataChannelClose', channel);
    };

    channel.onerror = (error) => {
      console.error('Data channel error:', error);
      this.emit('dataChannelError', error);
    };

    channel.onmessage = (event) => {
      try {
        const message: DataChannelMessage = JSON.parse(event.data);
        this.emit('dataChannelMessage', message);
        this.handleDataChannelMessage(message);
      } catch (error) {
        console.error('Failed to parse data channel message:', error);
      }
    };
  }

  /**
   * Handle incoming data channel messages
   */
  private handleDataChannelMessage(message: DataChannelMessage): void {
    switch (message.type) {
      case 'transcription':
        this.emit('transcriptionReceived', message.payload);
        break;
      case 'voice-data':
        this.emit('voiceDataReceived', message.payload);
        break;
      case 'text':
        this.emit('textMessageReceived', message.payload);
        break;
      case 'file':
        this.emit('fileReceived', message.payload);
        break;
    }
  }

  /**
   * Get user media (audio/video)
   */
  async getUserMedia(constraints?: MediaStreamConstraints): Promise<MediaStream> {
    const defaultConstraints: MediaStreamConstraints = {
      audio: this.config.enableAudio ? this.config.audioConstraints : false,
      video: this.config.enableVideo ? this.config.videoConstraints : false
    };

    const finalConstraints = constraints || defaultConstraints;

    try {
      this.localStream = await navigator.mediaDevices.getUserMedia(finalConstraints);
      console.log('Got user media:', finalConstraints);
      return this.localStream;
    } catch (error) {
      console.error('Failed to get user media:', error);
      throw new Error(`Failed to access media devices: ${error}`);
    }
  }

  /**
   * Start a voice call
   */
  async startCall(remoteUserId: string, options?: { video?: boolean }): Promise<CallSession> {
    try {
      // Initialize peer connection
      await this.initializePeerConnection();

      // Get user media
      const mediaConstraints: MediaStreamConstraints = {
        audio: this.config.audioConstraints,
        video: options?.video || false
      };

      const stream = await this.getUserMedia(mediaConstraints);
      
      // Add tracks to peer connection
      stream.getTracks().forEach(track => {
        this.peerConnection?.addTrack(track, stream);
      });

      // Create offer
      const offer = await this.peerConnection!.createOffer();
      await this.peerConnection!.setLocalDescription(offer);

      // Create call session
      this.currentSession = {
        id: this.generateSessionId(),
        remoteUserId,
        status: 'connecting',
        startTime: new Date(),
        type: options?.video ? 'video' : 'audio'
      };

      this.emit('callStarted', this.currentSession);
      this.emit('localOffer', { offer, sessionId: this.currentSession.id });

      return this.currentSession;
    } catch (error) {
      console.error('Failed to start call:', error);
      throw error;
    }
  }

  /**
   * Answer an incoming call
   */
  async answerCall(offer: RTCSessionDescriptionInit, remoteUserId: string): Promise<CallSession> {
    try {
      // Initialize peer connection
      await this.initializePeerConnection();

      // Get user media
      const stream = await this.getUserMedia();
      
      // Add tracks to peer connection
      stream.getTracks().forEach(track => {
        this.peerConnection?.addTrack(track, stream);
      });

      // Set remote description (the offer)
      await this.peerConnection!.setRemoteDescription(offer);

      // Create and set local description (the answer)
      const answer = await this.peerConnection!.createAnswer();
      await this.peerConnection!.setLocalDescription(answer);

      // Create call session
      this.currentSession = {
        id: this.generateSessionId(),
        remoteUserId,
        status: 'connecting',
        startTime: new Date(),
        type: 'audio'
      };

      this.emit('callAnswered', this.currentSession);
      this.emit('localAnswer', { answer, sessionId: this.currentSession.id });

      return this.currentSession;
    } catch (error) {
      console.error('Failed to answer call:', error);
      throw error;
    }
  }

  /**
   * Handle remote answer
   */
  async handleRemoteAnswer(answer: RTCSessionDescriptionInit): Promise<void> {
    try {
      if (!this.peerConnection) {
        throw new Error('No peer connection available');
      }

      await this.peerConnection.setRemoteDescription(answer);
      console.log('Remote answer set successfully');
    } catch (error) {
      console.error('Failed to handle remote answer:', error);
      throw error;
    }
  }

  /**
   * Add ICE candidate
   */
  async addIceCandidate(candidate: RTCIceCandidateInit): Promise<void> {
    try {
      if (!this.peerConnection) {
        throw new Error('No peer connection available');
      }

      await this.peerConnection.addIceCandidate(candidate);
      console.log('ICE candidate added successfully');
    } catch (error) {
      console.error('Failed to add ICE candidate:', error);
      // Don't throw - ICE candidates can fail sometimes
    }
  }

  /**
   * End the current call
   */
  endCall(): void {
    if (this.currentSession) {
      this.currentSession.status = 'disconnected';
      this.currentSession.duration = Date.now() - this.currentSession.startTime.getTime();
      this.emit('callEnded', this.currentSession);
    }

    this.closePeerConnection();
    this.currentSession = null;
  }

  /**
   * Close peer connection and clean up resources
   */
  private closePeerConnection(): void {
    if (this.localStream) {
      this.localStream.getTracks().forEach(track => track.stop());
      this.localStream = null;
    }

    if (this.dataChannel) {
      this.dataChannel.close();
      this.dataChannel = null;
    }

    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
    }

    this.remoteStream = null;
  }

  /**
   * Send data through data channel
   */
  sendData(message: Omit<DataChannelMessage, 'timestamp' | 'senderId'>): boolean {
    if (!this.dataChannel || this.dataChannel.readyState !== 'open') {
      console.warn('Data channel not available or not open');
      return false;
    }

    try {
      const fullMessage: DataChannelMessage = {
        ...message,
        timestamp: Date.now(),
        senderId: 'local-user' // In real app, get from auth context
      };

      this.dataChannel.send(JSON.stringify(fullMessage));
      return true;
    } catch (error) {
      console.error('Failed to send data:', error);
      return false;
    }
  }

  /**
   * Send real-time transcription data
   */
  sendTranscription(transcription: string, isInterim: boolean = false): boolean {
    return this.sendData({
      type: 'transcription',
      payload: { text: transcription, isInterim, timestamp: Date.now() }
    });
  }

  /**
   * Send voice data chunks for real-time processing
   */
  sendVoiceData(audioData: ArrayBuffer): boolean {
    return this.sendData({
      type: 'voice-data',
      payload: { audioData: Array.from(new Uint8Array(audioData)), timestamp: Date.now() }
    });
  }

  /**
   * Get screen sharing stream
   */
  async getScreenShare(): Promise<MediaStream> {
    try {
      const stream = await navigator.mediaDevices.getDisplayMedia({
        video: true,
        audio: true
      });

      return stream;
    } catch (error) {
      console.error('Failed to get screen share:', error);
      throw error;
    }
  }

  /**
   * Start screen sharing
   */
  async startScreenShare(): Promise<void> {
    try {
      const screenStream = await this.getScreenShare();
      
      if (!this.peerConnection) {
        throw new Error('No active peer connection');
      }

      // Replace video track with screen share
      const sender = this.peerConnection.getSenders().find(s => 
        s.track && s.track.kind === 'video'
      );

      if (sender) {
        const screenTrack = screenStream.getVideoTracks()[0];
        await sender.replaceTrack(screenTrack);
        
        // Handle screen share ended
        screenTrack.onended = () => {
          this.emit('screenShareEnded');
        };
      }

      this.emit('screenShareStarted', screenStream);
    } catch (error) {
      console.error('Failed to start screen share:', error);
      throw error;
    }
  }

  /**
   * Get connection statistics
   */
  async getConnectionStats(): Promise<RTCStatsReport | null> {
    if (!this.peerConnection) return null;

    try {
      return await this.peerConnection.getStats();
    } catch (error) {
      console.error('Failed to get connection stats:', error);
      return null;
    }
  }

  /**
   * Get current session info
   */
  getCurrentSession(): CallSession | null {
    return this.currentSession;
  }

  /**
   * Get local stream
   */
  getLocalStream(): MediaStream | null {
    return this.localStream;
  }

  /**
   * Get remote stream
   */
  getRemoteStream(): MediaStream | null {
    return this.remoteStream;
  }

  /**
   * Check if WebRTC is supported
   */
  static isSupported(): boolean {
    return !!(
      window.RTCPeerConnection &&
      navigator.mediaDevices &&
      navigator.mediaDevices.getUserMedia
    );
  }

  /**
   * Event management
   */
  on(event: string, callback: Function): void {
    if (!this.eventListeners.has(event)) {
      this.eventListeners.set(event, []);
    }
    this.eventListeners.get(event)!.push(callback);
  }

  off(event: string, callback: Function): void {
    const listeners = this.eventListeners.get(event);
    if (listeners) {
      const index = listeners.indexOf(callback);
      if (index > -1) {
        listeners.splice(index, 1);
      }
    }
  }

  private emit(event: string, data?: any): void {
    const listeners = this.eventListeners.get(event);
    if (listeners) {
      listeners.forEach(callback => callback(data));
    }
  }

  /**
   * Generate unique session ID
   */
  private generateSessionId(): string {
    return `session-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Clean up resources
   */
  dispose(): void {
    this.endCall();
    this.eventListeners.clear();
  }
}

export default WebRTCService;