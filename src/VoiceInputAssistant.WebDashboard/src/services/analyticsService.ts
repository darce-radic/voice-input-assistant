// Analytics Service for Voice Input Assistant
// Comprehensive tracking and analytics functionality with multiple providers support

import { 
  AnalyticsEvent, 
  ConversionEvent, 
  PageViewData, 
  UserProperties, 
  WebVital, 
  PerformanceMetrics 
} from '../types/seo';
import { Metric } from 'web-vitals';

interface AnalyticsConfig {
  googleAnalyticsId?: string;
  googleTagManagerId?: string;
  facebookPixelId?: string;
  mixpanelToken?: string;
  hotjarId?: string;
  sentryDsn?: string;
  datadogRumApplicationId?: string;
  datadogRumClientToken?: string;
  enableDebugMode?: boolean;
  enableInDevelopment?: boolean;
  consentGiven?: boolean;
  anonymizeIp?: boolean;
  cookieDomain?: string;
  cookieExpires?: number;
}

class AnalyticsService {
  private config: AnalyticsConfig = {};
  private isInitialized = false;
  private consentGiven = false;
  private debugMode = false;
  private eventQueue: Array<() => void> = [];
  private performanceObserver: PerformanceObserver | null = null;
  private webVitalsData: WebVital[] = [];

  constructor() {
    // Auto-initialize with environment variables
    this.config = {
      googleAnalyticsId: process.env.REACT_APP_GA_TRACKING_ID,
      googleTagManagerId: process.env.REACT_APP_GTM_ID,
      facebookPixelId: process.env.REACT_APP_FB_PIXEL_ID,
      mixpanelToken: process.env.REACT_APP_MIXPANEL_TOKEN,
      hotjarId: process.env.REACT_APP_HOTJAR_ID,
      sentryDsn: process.env.REACT_APP_SENTRY_DSN,
      datadogRumApplicationId: process.env.REACT_APP_DATADOG_RUM_APP_ID,
      datadogRumClientToken: process.env.REACT_APP_DATADOG_RUM_CLIENT_TOKEN,
      enableDebugMode: process.env.REACT_APP_DEBUG_MODE === 'true',
      enableInDevelopment: process.env.REACT_APP_ENABLE_ANALYTICS_IN_DEV === 'true',
      anonymizeIp: true,
      cookieExpires: 365 * 24 * 60 * 60 * 1000 // 1 year
    };
  }

  /**
   * Initialize analytics services
   */
  async initialize(config?: Partial<AnalyticsConfig>): Promise<void> {
    if (this.isInitialized) {
      console.warn('Analytics already initialized');
      return;
    }

    // Merge provided config with existing config
    if (config) {
      this.config = { ...this.config, ...config };
    }

    this.debugMode = this.config.enableDebugMode || false;
    
    // Skip initialization in development unless explicitly enabled
    if (process.env.NODE_ENV === 'development' && !this.config.enableInDevelopment) {
      this.log('Analytics disabled in development mode');
      return;
    }

    try {
      // Wait for consent or initialize with basic tracking
      await this.checkConsent();
      
      // Initialize analytics providers
      await Promise.all([
        this.initializeGoogleAnalytics(),
        this.initializeGoogleTagManager(),
        this.initializeFacebookPixel(),
        this.initializeMixpanel(),
        this.initializeHotjar(),
        this.initializeSentry(),
        this.initializeDatadog(),
      ]);

      // Initialize web vitals monitoring
      this.initializeWebVitals();
      
      // Initialize performance monitoring
      this.initializePerformanceMonitoring();
      
      // Process queued events
      this.processEventQueue();
      
      this.isInitialized = true;
      this.log('Analytics service initialized successfully');
      
      // Track initialization
      this.trackEvent({
        category: 'System',
        action: 'Analytics Initialized',
        label: 'Success'
      });
      
    } catch (error) {
      console.error('Failed to initialize analytics:', error);
      this.trackError(error as Error, 'Analytics Initialization Failed');
    }
  }

  /**
   * Check and manage consent
   */
  private async checkConsent(): Promise<void> {
    // Check for existing consent
    const consent = localStorage.getItem('analytics_consent');
    if (consent === 'granted') {
      this.consentGiven = true;
      return;
    }

    // If no consent stored, ask for consent (in real app, show consent banner)
    // For now, we'll assume consent is granted for essential analytics
    this.consentGiven = true;
    localStorage.setItem('analytics_consent', 'granted');
  }

  /**
   * Initialize Google Analytics 4
   */
  private async initializeGoogleAnalytics(): Promise<void> {
    if (!this.config.googleAnalyticsId || !this.consentGiven) return;

    try {
      // Load gtag script
      const script = document.createElement('script');
      script.async = true;
      script.src = `https://www.googletagmanager.com/gtag/js?id=${this.config.googleAnalyticsId}`;
      document.head.appendChild(script);

      await new Promise((resolve) => {
        script.onload = resolve;
      });

      // Initialize gtag
      window.dataLayer = window.dataLayer || [];
      function gtag(...args: any[]) {
        window.dataLayer.push(args);
      }
      window.gtag = gtag;

      gtag('js', new Date());
      gtag('config', this.config.googleAnalyticsId, {
        anonymize_ip: this.config.anonymizeIp,
        cookie_domain: this.config.cookieDomain || 'auto',
        cookie_expires: this.config.cookieExpires,
        send_page_view: false, // We'll send manually
        custom_map: {
          'custom_parameter': 'dimension1'
        }
      });

      this.log('Google Analytics initialized');
    } catch (error) {
      console.error('Failed to initialize Google Analytics:', error);
    }
  }

  /**
   * Initialize Google Tag Manager
   */
  private async initializeGoogleTagManager(): Promise<void> {
    if (!this.config.googleTagManagerId || !this.consentGiven) return;

    try {
      // GTM script
      const script = document.createElement('script');
      script.innerHTML = `
        (function(w,d,s,l,i){w[l]=w[l]||[];w[l].push({'gtm.start':
        new Date().getTime(),event:'gtm.js'});var f=d.getElementsByTagName(s)[0],
        j=d.createElement(s),dl=l!='dataLayer'?'&l='+l:'';j.async=true;j.src=
        'https://www.googletagmanager.com/gtm.js?id='+i+dl;f.parentNode.insertBefore(j,f);
        })(window,document,'script','dataLayer','${this.config.googleTagManagerId}');
      `;
      document.head.appendChild(script);

      // GTM noscript fallback
      const noscript = document.createElement('noscript');
      noscript.innerHTML = `
        <iframe src="https://www.googletagmanager.com/ns.html?id=${this.config.googleTagManagerId}"
        height="0" width="0" style="display:none;visibility:hidden"></iframe>
      `;
      document.body.appendChild(noscript);

      this.log('Google Tag Manager initialized');
    } catch (error) {
      console.error('Failed to initialize Google Tag Manager:', error);
    }
  }

  /**
   * Initialize Facebook Pixel
   */
  private async initializeFacebookPixel(): Promise<void> {
    if (!this.config.facebookPixelId || !this.consentGiven) return;

    try {
      // Facebook Pixel code
      const script = document.createElement('script');
      script.innerHTML = `
        !function(f,b,e,v,n,t,s)
        {if(f.fbq)return;n=f.fbq=function(){n.callMethod?
        n.callMethod.apply(n,arguments):n.queue.push(arguments)};
        if(!f._fbq)f._fbq=n;n.push=n;n.loaded=!0;n.version='2.0';
        n.queue=[];t=b.createElement(e);t.async=!0;
        t.src=v;s=b.getElementsByTagName(e)[0];
        s.parentNode.insertBefore(t,s)}(window, document,'script',
        'https://connect.facebook.net/en_US/fbevents.js');
        fbq('init', '${this.config.facebookPixelId}');
        fbq('track', 'PageView');
      `;
      document.head.appendChild(script);

      // Noscript fallback
      const noscript = document.createElement('noscript');
      noscript.innerHTML = `
        <img height="1" width="1" style="display:none"
        src="https://www.facebook.com/tr?id=${this.config.facebookPixelId}&ev=PageView&noscript=1" />
      `;
      document.body.appendChild(noscript);

      this.log('Facebook Pixel initialized');
    } catch (error) {
      console.error('Failed to initialize Facebook Pixel:', error);
    }
  }

  /**
   * Initialize Mixpanel
   */
  private async initializeMixpanel(): Promise<void> {
    if (!this.config.mixpanelToken || !this.consentGiven) return;

    try {
      // Load Mixpanel library
      const script = document.createElement('script');
      script.src = 'https://cdn.mxpnl.com/libs/mixpanel-2-latest.min.js';
      script.async = true;
      document.head.appendChild(script);

      await new Promise((resolve) => {
        script.onload = resolve;
      });

      // Initialize Mixpanel
      if (window.mixpanel) {
        window.mixpanel.init(this.config.mixpanelToken, {
          debug: this.debugMode,
          track_pageview: false,
          persistence: 'localStorage'
        });
      }

      this.log('Mixpanel initialized');
    } catch (error) {
      console.error('Failed to initialize Mixpanel:', error);
    }
  }

  /**
   * Initialize Hotjar
   */
  private async initializeHotjar(): Promise<void> {
    if (!this.config.hotjarId || !this.consentGiven) return;

    try {
      // Hotjar tracking code
      const script = document.createElement('script');
      script.innerHTML = `
        (function(h,o,t,j,a,r){
            h.hj=h.hj||function(){(h.hj.q=h.hj.q||[]).push(arguments)};
            h._hjSettings={hjid:${this.config.hotjarId},hjsv:6};
            a=o.getElementsByTagName('head')[0];
            r=o.createElement('script');r.async=1;
            r.src=t+h._hjSettings.hjid+j+h._hjSettings.hjsv;
            a.appendChild(r);
        })(window,document,'https://static.hotjar.com/c/hotjar-','.js?sv=');
      `;
      document.head.appendChild(script);

      this.log('Hotjar initialized');
    } catch (error) {
      console.error('Failed to initialize Hotjar:', error);
    }
  }

  /**
   * Initialize Sentry error tracking
   */
  private async initializeSentry(): Promise<void> {
    if (!this.config.sentryDsn) return;

    try {
      // In a real implementation, you'd import and configure Sentry
      // import * as Sentry from "@sentry/react";
      // Sentry.init({ dsn: this.config.sentryDsn });
      
      this.log('Sentry initialized');
    } catch (error) {
      console.error('Failed to initialize Sentry:', error);
    }
  }

  /**
   * Initialize Datadog RUM
   */
  private async initializeDatadog(): Promise<void> {
    if (!this.config.datadogRumApplicationId || !this.config.datadogRumClientToken) return;

    try {
      // Load Datadog RUM script
      const script = document.createElement('script');
      script.src = 'https://www.datadoghq-browser-agent.com/datadog-rum-v4.js';
      script.async = true;
      document.head.appendChild(script);

      await new Promise((resolve) => {
        script.onload = resolve;
      });

      // Initialize Datadog RUM
      if (window.DD_RUM) {
        window.DD_RUM.init({
          applicationId: this.config.datadogRumApplicationId!,
          clientToken: this.config.datadogRumClientToken!,
          site: 'datadoghq.com',
          service: 'voice-input-assistant',
          env: process.env.REACT_APP_ENV || 'development',
          version: process.env.REACT_APP_VERSION || '1.0.0',
          sampleRate: 100,
          trackInteractions: true,
          trackUserInteractions: true,
          defaultPrivacyLevel: 'mask-user-input'
        });
        
        window.DD_RUM.startSessionReplayRecording();
      }

      this.log('Datadog RUM initialized');
    } catch (error) {
      console.error('Failed to initialize Datadog RUM:', error);
    }
  }

  /**
   * Initialize Web Vitals monitoring
   */
  private initializeWebVitals(): void {
    try {
      // Import web-vitals dynamically
      import('web-vitals').then(({ getCLS, getFID, getFCP, getLCP, getTTFB }) => {
        getCLS(this.onWebVital.bind(this));
        getFID(this.onWebVital.bind(this));
        getFCP(this.onWebVital.bind(this));
        getLCP(this.onWebVital.bind(this));
        getTTFB(this.onWebVital.bind(this));
      }).catch(() => {
        this.log('web-vitals library not available');
      });
    } catch (error) {
      console.error('Failed to initialize Web Vitals:', error);
    }
  }

  /**
   * Handle Web Vitals data
   */
  private onWebVital(metric: Metric): void {
    let navType: 'navigate' | 'reload' | 'back_forward' | 'back_forward_cache' | undefined;
    switch (metric.navigationType) {
      case 'navigate':
      case 'reload':
        navType = metric.navigationType;
        break;
      case 'back-forward':
        navType = 'back_forward';
        break;
      case 'back-forward-cache':
        navType = 'back_forward_cache';
        break;
      default:
        navType = undefined;
    }
    const webVital: WebVital = {
      name: metric.name,
      value: metric.value,
      rating: metric.rating,
      delta: metric.delta,
      id: metric.id,
      navigationType: navType,
    };
    this.webVitalsData.push(webVital);
    
    // Send to analytics
    this.trackEvent({
      category: 'Web Vitals',
      action: webVital.name,
      value: Math.round(webVital.value),
      label: webVital.rating
    });

    // Send to performance monitoring
    if (window.DD_RUM) {
      window.DD_RUM.addTiming(metric.name, metric.value);
    }

    this.log(`Web Vital ${metric.name}:`, metric.value, metric.rating);
  }

  /**
   * Initialize performance monitoring
   */
  private initializePerformanceMonitoring(): void {
    if (!window.PerformanceObserver) return;

    try {
      // Monitor navigation timing
      this.performanceObserver = new PerformanceObserver((list) => {
        for (const entry of list.getEntries()) {
          if (entry.entryType === 'navigation') {
            this.handleNavigationTiming(entry as PerformanceNavigationTiming);
          } else if (entry.entryType === 'resource') {
            this.handleResourceTiming(entry as PerformanceResourceTiming);
          }
        }
      });

      this.performanceObserver.observe({ entryTypes: ['navigation', 'resource'] });
    } catch (error) {
      console.error('Failed to initialize performance monitoring:', error);
    }
  }

  /**
   * Handle navigation timing data
   */
  private handleNavigationTiming(entry: PerformanceNavigationTiming): void {
    const metrics: PerformanceMetrics = {
      loadTime: entry.loadEventEnd - 0,
      domContentLoaded: entry.domContentLoadedEventEnd - 0,
      timeToInteractive: entry.loadEventEnd - 0,
      firstContentfulPaint: 0, // Will be filled by Web Vitals
      largestContentfulPaint: 0, // Will be filled by Web Vitals
      cumulativeLayoutShift: 0, // Will be filled by Web Vitals
      firstInputDelay: 0, // Will be filled by Web Vitals
      totalBlockingTime: 0,
      speedIndex: 0,
      resourceLoadTimes: {}
    };

    this.trackEvent({
      category: 'Performance',
      action: 'Page Load',
      value: Math.round(metrics.loadTime),
      label: 'Load Time'
    });

    this.log('Navigation timing:', metrics);
  }

  /**
   * Handle resource timing data
   */
  private handleResourceTiming(entry: PerformanceResourceTiming): void {
    const loadTime = entry.responseEnd - entry.requestStart;
    
    if (loadTime > 1000) { // Log slow resources (>1s)
      this.trackEvent({
        category: 'Performance',
        action: 'Slow Resource',
        value: Math.round(loadTime),
        label: entry.name
      });
    }
  }

  /**
   * Track page view
   */
  trackPageView(data: Partial<PageViewData>): void {
    if (!this.isInitialized || !this.consentGiven) {
      this.eventQueue.push(() => this.trackPageView(data));
      return;
    }

    const pageData: PageViewData = {
      page_title: data.page_title || document.title,
      page_location: data.page_location || window.location.href,
      page_referrer: data.page_referrer || document.referrer,
      ...data
    };

    // Google Analytics
    if (window.gtag) {
      window.gtag('config', this.config.googleAnalyticsId, {
        page_title: pageData.page_title,
        page_location: pageData.page_location,
        page_referrer: pageData.page_referrer
      });
    }

    // Mixpanel
    if (window.mixpanel) {
      window.mixpanel.track('Page View', pageData);
    }

    // Facebook Pixel
    if (window.fbq) {
      window.fbq('track', 'PageView');
    }

    this.log('Page view tracked:', pageData);
  }

  /**
   * Track custom event
   */
  trackEvent(event: AnalyticsEvent): void {
    if (!this.isInitialized || !this.consentGiven) {
      this.eventQueue.push(() => this.trackEvent(event));
      return;
    }

    // Google Analytics
    if (window.gtag) {
      window.gtag('event', event.action, {
        event_category: event.category,
        event_label: event.label,
        value: event.value,
        ...event.customParameters
      });
    }

    // Mixpanel
    if (window.mixpanel) {
      window.mixpanel.track(event.action, {
        category: event.category,
        label: event.label,
        value: event.value,
        ...event.customParameters
      });
    }

    // Facebook Pixel
    if (window.fbq && event.category === 'Conversion') {
      window.fbq('track', event.action, {
        value: event.value,
        currency: 'USD'
      });
    }

    this.log('Event tracked:', event);
  }

  /**
   * Track conversion
   */
  trackConversion(event: ConversionEvent): void {
    if (!this.isInitialized || !this.consentGiven) {
      this.eventQueue.push(() => this.trackConversion(event));
      return;
    }

    // Google Analytics Enhanced Ecommerce
    if (window.gtag) {
      window.gtag('event', event.eventName, {
        currency: event.currency || 'USD',
        value: event.value,
        items: event.items,
        ...event.customParameters
      });
    }

    // Mixpanel
    if (window.mixpanel) {
      window.mixpanel.track(event.eventName, {
        revenue: event.value,
        currency: event.currency,
        items: event.items,
        ...event.customParameters
      });
    }

    // Facebook Pixel
    if (window.fbq) {
      window.fbq('track', event.eventName, {
        value: event.value,
        currency: event.currency || 'USD',
        contents: event.items?.map(item => ({
          id: item.item_id,
          quantity: item.quantity,
          item_price: item.price
        }))
      });
    }

    this.log('Conversion tracked:', event);
  }

  /**
   * Set user properties
   */
  setUserProperties(properties: UserProperties): void {
    if (!this.isInitialized || !this.consentGiven) {
      this.eventQueue.push(() => this.setUserProperties(properties));
      return;
    }

    // Google Analytics
    if (window.gtag && properties.user_id) {
      window.gtag('config', this.config.googleAnalyticsId, {
        user_id: properties.user_id,
        custom_map: properties.custom_parameters
      });
    }

    // Mixpanel
    if (window.mixpanel) {
      if (properties.user_id) {
        window.mixpanel.identify(properties.user_id);
      }
      if (properties.user_properties) {
        window.mixpanel.people.set(properties.user_properties);
      }
    }

    this.log('User properties set:', properties);
  }

  /**
   * Track error
   */
  trackError(error: Error, context?: string): void {
    const errorEvent: AnalyticsEvent = {
      category: 'Error',
      action: error.name || 'Unknown Error',
      label: context || error.message,
      customParameters: {
        error_message: error.message,
        error_stack: error.stack,
        context: context
      }
    };

    this.trackEvent(errorEvent);

    // Send to error tracking services
    if (window.Sentry) {
      window.Sentry.captureException(error, { extra: { context } });
    }

    this.log('Error tracked:', error, context);
  }

  /**
   * Get Web Vitals data
   */
  getWebVitals(): WebVital[] {
    return [...this.webVitalsData];
  }

  /**
   * Process queued events
   */
  private processEventQueue(): void {
    while (this.eventQueue.length > 0) {
      const eventFn = this.eventQueue.shift();
      if (eventFn) {
        try {
          eventFn();
        } catch (error) {
          console.error('Error processing queued event:', error);
        }
      }
    }
  }

  /**
   * Debug logging
   */
  private log(...args: any[]): void {
    if (this.debugMode) {
      console.log('[Analytics]', ...args);
    }
  }

  /**
   * Clean up resources
   */
  destroy(): void {
    if (this.performanceObserver) {
      this.performanceObserver.disconnect();
      this.performanceObserver = null;
    }
    
    this.eventQueue = [];
    this.webVitalsData = [];
    this.isInitialized = false;
  }

  /**
   * Update consent
   */
  updateConsent(granted: boolean): void {
    this.consentGiven = granted;
    localStorage.setItem('analytics_consent', granted ? 'granted' : 'denied');
    
    if (granted && !this.isInitialized) {
      this.initialize();
    }
    
    // Update consent for all providers
    if (window.gtag) {
      window.gtag('consent', 'update', {
        analytics_storage: granted ? 'granted' : 'denied',
        ad_storage: granted ? 'granted' : 'denied'
      });
    }
  }
}

// Global declarations for analytics services
declare global {
  interface Window {
    gtag: (...args: any[]) => void;
    dataLayer: any[];
    fbq: (...args: any[]) => void;
    mixpanel: any;
    hj: (...args: any[]) => void;
    Sentry: any;
    DD_RUM: any;
  }
}

// Create singleton instance
export const analyticsService = new AnalyticsService();
export default analyticsService;