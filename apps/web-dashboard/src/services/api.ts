// API Service Layer for Netlify Functions
const API_BASE = process.env.REACT_APP_API_URL || '/.netlify/functions';

export interface User {
  id: string;
  email: string;
  name: string;
  plan: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface TranscriptionResult {
  text: string;
  confidence: number;
  engine: string;
}

class ApiService {
  private token: string | null = null;

  constructor() {
    // Load token from localStorage if available
    if (typeof window !== 'undefined') {
      this.token = localStorage.getItem('auth_token');
    }
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      ...options.headers,
    };

    if (this.token) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }

    const response = await fetch(`${API_BASE}${endpoint}`, {
      ...options,
      headers,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Request failed' }));
      throw new Error(error.error || `HTTP ${response.status}`);
    }

    return response.json();
  }

  // Authentication
  async login(email: string, password: string): Promise<AuthResponse> {
    const response = await this.request<AuthResponse>('/auth', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });

    this.token = response.token;
    if (typeof window !== 'undefined') {
      localStorage.setItem('auth_token', response.token);
    }

    return response;
  }

  async verifyToken(): Promise<User> {
    const response = await this.request<{ user: User }>('/auth', {
      method: 'GET',
    });
    return response.user;
  }

  logout() {
    this.token = null;
    if (typeof window !== 'undefined') {
      localStorage.removeItem('auth_token');
    }
  }

  // Analytics
  async getAnalytics() {
    return this.request('/analytics', {
      method: 'GET',
    });
  }

  // Speech Recognition
  async transcribeAudio(audioBlob: Blob, engine: string): Promise<TranscriptionResult> {
    // Convert audio blob to base64
    const reader = new FileReader();
    const base64Audio = await new Promise<string>((resolve) => {
      reader.onloadend = () => resolve(reader.result as string);
      reader.readAsDataURL(audioBlob);
    });

    return this.request<TranscriptionResult>('/speech', {
      method: 'POST',
      body: JSON.stringify({ audio: base64Audio, engine }),
    });
  }
}

export const api = new ApiService();