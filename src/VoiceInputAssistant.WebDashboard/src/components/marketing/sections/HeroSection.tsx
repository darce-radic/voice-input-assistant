import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import './HeroSection.css';

const HeroSection: React.FC = () => {
  const [currentFeature, setCurrentFeature] = useState(0);
  const [isVisible, setIsVisible] = useState(false);

  const features = [
    'Real-time Voice Transcription',
    'AI-Powered Emotion Detection', 
    'Multi-Language Support',
    'Privacy-First Processing',
    'Offline Functionality'
  ];

  useEffect(() => {
    setIsVisible(true);
    const interval = setInterval(() => {
      setCurrentFeature((prev) => (prev + 1) % features.length);
    }, 3000);

    return () => clearInterval(interval);
  }, [features.length]);

  const handleGetStarted = () => {
    // Track conversion event
    if (typeof window.gtag !== 'undefined') {
      window.gtag('event', 'click', {
        event_category: 'CTA',
        event_label: 'Hero Get Started',
        value: 1
      });
    }
  };

  const handleWatchDemo = () => {
    // Track demo view event
    if (typeof window.gtag !== 'undefined') {
      window.gtag('event', 'click', {
        event_category: 'Engagement',
        event_label: 'Hero Watch Demo',
        value: 1
      });
    }
  };

  return (
    <section className={`hero-section ${isVisible ? 'visible' : ''}`} aria-label="Hero section">
      <div className="hero-container">
        <div className="hero-content">
          <div className="hero-text">
            {/* Badge */}
            <div className="hero-badge">
              <span className="badge-text">
                🚀 Now with Advanced AI Processing
              </span>
            </div>

            {/* Main Headlines */}
            <h1 className="hero-title">
              Transform Your Voice Into 
              <span className="gradient-text"> Intelligent Text</span>
            </h1>
            
            <div className="hero-subtitle-container">
              <h2 className="hero-subtitle">
                Experience the future of voice input with{' '}
                <span className="rotating-feature" key={currentFeature}>
                  {features[currentFeature]}
                </span>
              </h2>
            </div>

            <p className="hero-description">
              Our cutting-edge AI-powered voice assistant delivers real-time transcription, 
              emotion detection, and advanced audio processing. Perfect for professionals, 
              content creators, and businesses who value accuracy, privacy, and efficiency.
            </p>

            {/* Key Benefits */}
            <div className="hero-benefits">
              <div className="benefit-item">
                <span className="benefit-icon">⚡</span>
                <span>99.5% Accuracy</span>
              </div>
              <div className="benefit-item">
                <span className="benefit-icon">🔒</span>
                <span>Privacy First</span>
              </div>
              <div className="benefit-item">
                <span className="benefit-icon">🌐</span>
                <span>Works Offline</span>
              </div>
              <div className="benefit-item">
                <span className="benefit-icon">📱</span>
                <span>Any Device</span>
              </div>
            </div>

            {/* CTAs */}
            <div className="hero-ctas">
              <Link 
                to="/app" 
                className="cta-primary"
                onClick={handleGetStarted}
                aria-label="Start using Voice Input Assistant for free"
              >
                Get Started Free
                <span className="cta-arrow">→</span>
              </Link>
              
              <button 
                className="cta-secondary"
                onClick={handleWatchDemo}
                aria-label="Watch product demo video"
              >
                <span className="play-icon">▶</span>
                Watch Demo
              </button>
            </div>

            {/* Social Proof */}
            <div className="hero-social-proof">
              <div className="social-proof-stats">
                <div className="stat">
                  <span className="stat-number">10K+</span>
                  <span className="stat-label">Active Users</span>
                </div>
                <div className="stat">
                  <span className="stat-number">99.5%</span>
                  <span className="stat-label">Accuracy Rate</span>
                </div>
                <div className="stat">
                  <span className="stat-number">24/7</span>
                  <span className="stat-label">Availability</span>
                </div>
              </div>
              
              <div className="social-proof-reviews">
                <div className="stars">
                  {'★'.repeat(5)}
                </div>
                <span>Rated 4.8/5 by 500+ professionals</span>
              </div>
            </div>
          </div>

          {/* Hero Visual */}
          <div className="hero-visual">
            <div className="hero-image-container">
              <img 
                src="/images/hero-dashboard.png" 
                alt="Voice Input Assistant Dashboard - showing real-time transcription interface with waveform visualization and controls"
                className="hero-image"
                loading="eager"
                width="600"
                height="400"
              />
              
              {/* Floating UI Elements */}
              <div className="floating-elements">
                <div className="floating-card transcription-card">
                  <div className="card-header">
                    <span className="status-dot recording"></span>
                    <span>Recording...</span>
                  </div>
                  <div className="card-content">
                    <p>"Hello, this is a real-time transcription example..."</p>
                  </div>
                </div>

                <div className="floating-card emotion-card">
                  <div className="card-header">
                    <span className="emotion-icon">😊</span>
                    <span>Emotion: Positive</span>
                  </div>
                  <div className="confidence">
                    <span>Confidence: 94%</span>
                  </div>
                </div>

                <div className="floating-card waveform-card">
                  <div className="waveform">
                    <div className="wave-bar" style={{height: '20px'}}></div>
                    <div className="wave-bar" style={{height: '35px'}}></div>
                    <div className="wave-bar" style={{height: '15px'}}></div>
                    <div className="wave-bar" style={{height: '40px'}}></div>
                    <div className="wave-bar" style={{height: '25px'}}></div>
                    <div className="wave-bar active" style={{height: '50px'}}></div>
                  </div>
                </div>
              </div>
            </div>

            {/* Background Decorations */}
            <div className="hero-decorations">
              <div className="decoration-circle circle-1"></div>
              <div className="decoration-circle circle-2"></div>
              <div className="decoration-gradient gradient-1"></div>
              <div className="decoration-gradient gradient-2"></div>
            </div>
          </div>
        </div>

        {/* Scroll Indicator */}
        <div className="scroll-indicator">
          <span className="scroll-text">Discover More</span>
          <div className="scroll-arrow">↓</div>
        </div>
      </div>

      {/* Background Pattern */}
      <div className="hero-background">
        <div className="bg-pattern"></div>
        <div className="bg-grid"></div>
      </div>
    </section>
  );
};

export default HeroSection;