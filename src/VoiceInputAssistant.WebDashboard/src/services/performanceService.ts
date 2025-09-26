import { EventEmitter } from 'events';

// Performance metrics
export interface PerformanceMetrics {
  // Web Vitals
  FCP: number; // First Contentful Paint
  LCP: number; // Largest Contentful Paint
  FID: number; // First Input Delay
  CLS: number; // Cumulative Layout Shift
  TTFB: number; // Time to First Byte

  // Custom metrics
  TTI: number; // Time to Interactive
  domContentLoaded: number;
  loadComplete: number;
  memoryUsage?: MemoryInfo;
  bundleSize: number;
  cacheHitRate: number;
}

export interface MemoryInfo {
  jsHeapSizeLimit: number;
  totalJSHeapSize: number;
  usedJSHeapSize: number;
}

// Resource timing
export interface ResourceTiming {
  name: string;
  entryType: string;
  startTime: number;
  duration: number;
  transferSize: number;
  encodedBodySize: number;
  decodedBodySize: number;
}

// Bundle information
export interface BundleInfo {
  name: string;
  size: number;
  compressed: number;
  modules: string[];
  dependencies: string[];
  loadTime: number;
  cached: boolean;
}

// Lazy load config
export interface LazyLoadConfig {
  rootMargin: string;
  threshold: number;
  enableIntersectionObserver: boolean;
}

// Code split point
export interface CodeSplitPoint {
  name: string;
  loader: () => Promise<any>;
  preload: boolean;
  priority: 'high' | 'normal' | 'low';
  dependencies: string[];
}

class PerformanceService extends EventEmitter {
  private metrics: Partial<PerformanceMetrics> = {};
  private observers: Map<string, any> = new Map();
  private bundles: Map<string, BundleInfo> = new Map();
  private codeSplitPoints: Map<string, CodeSplitPoint> = new Map();
  private loadedChunks: Set<string> = new Set();
  private lazyLoadConfig: LazyLoadConfig = {
    rootMargin: '50px',
    threshold: 0.1,
    enableIntersectionObserver: true
  };

  constructor() {
    super();
    this.initialize();
  }

  /**
   * Initialize performance monitoring
   */
  private initialize(): void {
    this.setupPerformanceObserver();
    this.measureWebVitals();
    this.setupResourceTimingObserver();
    this.monitorMemoryUsage();
    this.setupNavigationTiming();
  }

  /**
   * Setup performance observer for various metrics
   */
  private setupPerformanceObserver(): void {
    if ('PerformanceObserver' in window) {
      // Long tasks observer
      try {
        const longTaskObserver = new PerformanceObserver((list) => {
          list.getEntries().forEach((entry: any) => {
            this.emit('long-task', {
              name: entry.name,
              duration: entry.duration,
              startTime: entry.startTime
            });
          });
        });
        longTaskObserver.observe({ entryTypes: ['longtask'] });
        this.observers.set('longtask', longTaskObserver);
      } catch (error) {
        console.warn('Long tasks observer not supported');
      }

      // Layout shift observer
      try {
        const clsObserver = new PerformanceObserver((list) => {
          let clsValue = 0;
          list.getEntries().forEach((entry: any) => {
            if (!entry.hadRecentInput) {
              clsValue += entry.value;
            }
          });
          this.metrics.CLS = (this.metrics.CLS || 0) + clsValue;
          this.emit('cls-updated', this.metrics.CLS);
        });
        clsObserver.observe({ entryTypes: ['layout-shift'] });
        this.observers.set('layout-shift', clsObserver);
      } catch (error) {
        console.warn('Layout shift observer not supported');
      }

      // Paint observer
      try {
        const paintObserver = new PerformanceObserver((list) => {
          list.getEntries().forEach((entry: any) => {
            if (entry.name === 'first-contentful-paint') {
              this.metrics.FCP = entry.startTime;
              this.emit('fcp-measured', this.metrics.FCP);
            }
          });
        });
        paintObserver.observe({ entryTypes: ['paint'] });
        this.observers.set('paint', paintObserver);
      } catch (error) {
        console.warn('Paint observer not supported');
      }
    }
  }

  /**
   * Measure Web Vitals
   */
  private measureWebVitals(): void {
    // Largest Contentful Paint
    this.measureLCP();
    
    // First Input Delay
    this.measureFID();
    
    // Time to First Byte
    this.measureTTFB();
  }

  /**
   * Measure Largest Contentful Paint
   */
  private measureLCP(): void {
    if ('PerformanceObserver' in window) {
      try {
        const lcpObserver = new PerformanceObserver((list) => {
          const entries = list.getEntries();
          const lastEntry = entries[entries.length - 1] as any;
          this.metrics.LCP = lastEntry.startTime;
          this.emit('lcp-measured', this.metrics.LCP);
        });
        lcpObserver.observe({ entryTypes: ['largest-contentful-paint'] });
        this.observers.set('lcp', lcpObserver);
      } catch (error) {
        console.warn('LCP observer not supported');
      }
    }
  }

  /**
   * Measure First Input Delay
   */
  private measureFID(): void {
    if ('PerformanceObserver' in window) {
      try {
        const fidObserver = new PerformanceObserver((list) => {
          list.getEntries().forEach((entry: any) => {
            this.metrics.FID = entry.processingStart - entry.startTime;
            this.emit('fid-measured', this.metrics.FID);
          });
        });
        fidObserver.observe({ entryTypes: ['first-input'] });
        this.observers.set('fid', fidObserver);
      } catch (error) {
        console.warn('FID observer not supported');
      }
    }
  }

  /**
   * Measure Time to First Byte
   */
  private measureTTFB(): void {
    const navEntry = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
    if (navEntry) {
      this.metrics.TTFB = navEntry.responseStart - navEntry.requestStart;
      this.emit('ttfb-measured', this.metrics.TTFB);
    }
  }

  /**
   * Setup resource timing observer
   */
  private setupResourceTimingObserver(): void {
    if ('PerformanceObserver' in window) {
      try {
        const resourceObserver = new PerformanceObserver((list) => {
          list.getEntries().forEach((entry: any) => {
            const resourceTiming: ResourceTiming = {
              name: entry.name,
              entryType: entry.entryType,
              startTime: entry.startTime,
              duration: entry.duration,
              transferSize: entry.transferSize || 0,
              encodedBodySize: entry.encodedBodySize || 0,
              decodedBodySize: entry.decodedBodySize || 0
            };
            this.emit('resource-loaded', resourceTiming);
          });
        });
        resourceObserver.observe({ entryTypes: ['resource'] });
        this.observers.set('resource', resourceObserver);
      } catch (error) {
        console.warn('Resource observer not supported');
      }
    }
  }

  /**
   * Monitor memory usage
   */
  private monitorMemoryUsage(): void {
    if ('memory' in performance) {
      const updateMemory = () => {
        this.metrics.memoryUsage = (performance as any).memory;
        this.emit('memory-updated', this.metrics.memoryUsage);
      };

      updateMemory();
      setInterval(updateMemory, 30000); // Every 30 seconds
    }
  }

  /**
   * Setup navigation timing
   */
  private setupNavigationTiming(): void {
    window.addEventListener('load', () => {
      setTimeout(() => {
        const navEntry = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
        if (navEntry) {
          this.metrics.domContentLoaded = navEntry.domContentLoadedEventEnd - 0;
          this.metrics.loadComplete = navEntry.loadEventEnd - 0;
          
          // Calculate TTI (simplified)
          this.calculateTTI();
          
          this.emit('navigation-timing-measured', {
            domContentLoaded: this.metrics.domContentLoaded,
            loadComplete: this.metrics.loadComplete,
            tti: this.metrics.TTI
          });
        }
      }, 0);
    });
  }

  /**
   * Calculate Time to Interactive (simplified)
   */
  private calculateTTI(): void {
    const navEntry = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
    if (navEntry) {
      // Simplified TTI calculation
      this.metrics.TTI = Math.max(
        this.metrics.FCP || 0,
        this.metrics.domContentLoaded || 0
      );
    }
  }

  /**
   * Register code split point
   */
  public registerCodeSplitPoint(name: string, config: Omit<CodeSplitPoint, 'name'>): void {
    this.codeSplitPoints.set(name, { name, ...config });
    
    if (config.preload) {
      this.preloadChunk(name);
    }
    
    this.emit('code-split-registered', { name, config });
  }

  /**
   * Load chunk dynamically
   */
  public async loadChunk(name: string): Promise<any> {
    const splitPoint = this.codeSplitPoints.get(name);
    if (!splitPoint) {
      throw new Error(`Code split point '${name}' not found`);
    }

    const startTime = performance.now();

    try {
      // Load dependencies first
      for (const dep of splitPoint.dependencies) {
        if (!this.loadedChunks.has(dep)) {
          await this.loadChunk(dep);
        }
      }

      // Load the chunk
      const module = await splitPoint.loader();
      const endTime = performance.now();
      const loadTime = endTime - startTime;

      this.loadedChunks.add(name);
      
      // Record bundle info
      this.bundles.set(name, {
        name,
        size: 0, // Would be set by build system
        compressed: 0,
        modules: [],
        dependencies: splitPoint.dependencies,
        loadTime,
        cached: false
      });

      this.emit('chunk-loaded', { name, loadTime, module });
      return module;
    } catch (error) {
      this.emit('chunk-load-error', { name, error });
      throw error;
    }
  }

  /**
   * Preload chunk
   */
  private async preloadChunk(name: string): Promise<void> {
    try {
      await this.loadChunk(name);
      this.emit('chunk-preloaded', { name });
    } catch (error) {
      this.emit('chunk-preload-error', { name, error });
    }
  }

  /**
   * Setup lazy loading for images
   */
  public setupLazyLoading(config?: Partial<LazyLoadConfig>): void {
    this.lazyLoadConfig = { ...this.lazyLoadConfig, ...config };

    if (!this.lazyLoadConfig.enableIntersectionObserver || !('IntersectionObserver' in window)) {
      // Fallback to scroll-based lazy loading
      this.setupScrollBasedLazyLoading();
      return;
    }

    const imageObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            const img = entry.target as HTMLImageElement;
            this.loadLazyImage(img);
            imageObserver.unobserve(img);
          }
        });
      },
      {
        rootMargin: this.lazyLoadConfig.rootMargin,
        threshold: this.lazyLoadConfig.threshold
      }
    );

    // Observe all images with data-src
    document.querySelectorAll('img[data-src]').forEach((img) => {
      imageObserver.observe(img);
    });

    this.observers.set('lazy-images', imageObserver);
    this.emit('lazy-loading-setup', this.lazyLoadConfig);
  }

  /**
   * Fallback scroll-based lazy loading
   */
  private setupScrollBasedLazyLoading(): void {
    const loadImagesInViewport = () => {
      const images = document.querySelectorAll('img[data-src]');
      images.forEach((img) => {
        if (this.isInViewport(img as HTMLElement)) {
          this.loadLazyImage(img as HTMLImageElement);
        }
      });
    };

    window.addEventListener('scroll', this.throttle(loadImagesInViewport, 100));
    window.addEventListener('resize', this.throttle(loadImagesInViewport, 100));
    
    // Initial load
    loadImagesInViewport();
  }

  /**
   * Load lazy image
   */
  private loadLazyImage(img: HTMLImageElement): void {
    const src = img.getAttribute('data-src');
    if (!src) return;

    const startTime = performance.now();
    
    img.onload = () => {
      const loadTime = performance.now() - startTime;
      img.removeAttribute('data-src');
      img.classList.add('loaded');
      this.emit('lazy-image-loaded', { src, loadTime });
    };

    img.onerror = () => {
      this.emit('lazy-image-error', { src });
    };

    img.src = src;
  }

  /**
   * Check if element is in viewport
   */
  private isInViewport(element: HTMLElement): boolean {
    const rect = element.getBoundingClientRect();
    return (
      rect.top >= 0 &&
      rect.left >= 0 &&
      rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
      rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
  }

  /**
   * Throttle function
   */
  private throttle(func: Function, delay: number): () => void {
    let timeoutId: NodeJS.Timeout;
    let lastExecTime = 0;
    
    return function (...args: any[]) {
      const currentTime = Date.now();
      
      if (currentTime - lastExecTime > delay) {
        func(...args);
        lastExecTime = currentTime;
      } else {
        clearTimeout(timeoutId);
        timeoutId = setTimeout(() => {
          func(...args);
          lastExecTime = Date.now();
        }, delay - (currentTime - lastExecTime));
      }
    };
  }

  /**
   * Optimize font loading
   */
  public optimizeFontLoading(fonts: string[]): void {
    fonts.forEach((font) => {
      const link = document.createElement('link');
      link.rel = 'preload';
      link.as = 'font';
      link.type = 'font/woff2';
      link.crossOrigin = 'anonymous';
      link.href = font;
      document.head.appendChild(link);
    });

    this.emit('fonts-preloaded', fonts);
  }

  /**
   * Implement service worker caching
   */
  public async setupServiceWorkerCaching(): Promise<void> {
    if ('serviceWorker' in navigator) {
      try {
        const registration = await navigator.serviceWorker.register('/sw.js');
        this.emit('service-worker-registered', registration);
        
        // Update cache hit rate
        navigator.serviceWorker.addEventListener('message', (event) => {
          if (event.data.type === 'CACHE_STATS') {
            this.metrics.cacheHitRate = event.data.hitRate;
            this.emit('cache-stats-updated', event.data);
          }
        });
      } catch (error) {
        this.emit('service-worker-error', error);
      }
    }
  }

  /**
   * Compress and optimize images
   */
  public optimizeImages(): void {
    const images = document.querySelectorAll('img');
    images.forEach((img) => {
      // Use WebP if supported
      if (this.supportsWebP()) {
        const src = img.src;
        if (src && !src.includes('.webp')) {
          img.src = src.replace(/\.(jpg|jpeg|png)$/, '.webp');
        }
      }

      // Add loading="lazy" for native lazy loading
      if (!img.hasAttribute('loading')) {
        img.setAttribute('loading', 'lazy');
      }
    });

    this.emit('images-optimized');
  }

  /**
   * Check WebP support
   */
  private supportsWebP(): boolean {
    return document.createElement('canvas')
      .toDataURL('image/webp')
      .indexOf('data:image/webp') === 0;
  }

  /**
   * Implement critical CSS
   */
  public loadCriticalCSS(css: string): void {
    const style = document.createElement('style');
    style.textContent = css;
    document.head.insertBefore(style, document.head.firstChild);
    
    this.emit('critical-css-loaded', { size: css.length });
  }

  /**
   * Preload critical resources
   */
  public preloadCriticalResources(resources: Array<{ url: string; as: string; type?: string }>): void {
    resources.forEach((resource) => {
      const link = document.createElement('link');
      link.rel = 'preload';
      link.href = resource.url;
      link.as = resource.as;
      if (resource.type) {
        link.type = resource.type;
      }
      if (resource.as === 'font') {
        link.crossOrigin = 'anonymous';
      }
      document.head.appendChild(link);
    });

    this.emit('critical-resources-preloaded', resources);
  }

  /**
   * Monitor Core Web Vitals
   */
  public startWebVitalsMonitoring(): void {
    // Setup automatic reporting
    const reportVitals = () => {
      const vitals = {
        FCP: this.metrics.FCP,
        LCP: this.metrics.LCP,
        FID: this.metrics.FID,
        CLS: this.metrics.CLS,
        TTFB: this.metrics.TTFB
      };
      
      this.emit('web-vitals-report', vitals);
    };

    // Report on page unload
    window.addEventListener('beforeunload', reportVitals);
    
    // Report periodically
    setInterval(reportVitals, 30000);
  }

  /**
   * Get performance score
   */
  public getPerformanceScore(): number {
    let score = 100;

    // Deduct points based on metrics
    if (this.metrics.FCP && this.metrics.FCP > 1800) score -= 10;
    if (this.metrics.LCP && this.metrics.LCP > 2500) score -= 15;
    if (this.metrics.FID && this.metrics.FID > 100) score -= 10;
    if (this.metrics.CLS && this.metrics.CLS > 0.1) score -= 15;
    if (this.metrics.TTFB && this.metrics.TTFB > 800) score -= 10;

    return Math.max(0, score);
  }

  /**
   * Get current metrics
   */
  public getMetrics(): PerformanceMetrics {
    return {
      FCP: this.metrics.FCP || 0,
      LCP: this.metrics.LCP || 0,
      FID: this.metrics.FID || 0,
      CLS: this.metrics.CLS || 0,
      TTFB: this.metrics.TTFB || 0,
      TTI: this.metrics.TTI || 0,
      domContentLoaded: this.metrics.domContentLoaded || 0,
      loadComplete: this.metrics.loadComplete || 0,
      memoryUsage: this.metrics.memoryUsage,
      bundleSize: Array.from(this.bundles.values()).reduce((sum, bundle) => sum + bundle.size, 0),
      cacheHitRate: this.metrics.cacheHitRate || 0
    };
  }

  /**
   * Get bundle information
   */
  public getBundles(): BundleInfo[] {
    return Array.from(this.bundles.values());
  }

  /**
   * Generate performance report
   */
  public generateReport(): any {
    const metrics = this.getMetrics();
    const score = this.getPerformanceScore();
    const bundles = this.getBundles();

    return {
      timestamp: new Date().toISOString(),
      score,
      metrics,
      bundles,
      recommendations: this.getRecommendations(metrics)
    };
  }

  /**
   * Get performance recommendations
   */
  private getRecommendations(metrics: PerformanceMetrics): string[] {
    const recommendations: string[] = [];

    if (metrics.FCP > 1800) {
      recommendations.push('Optimize First Contentful Paint - consider reducing CSS and JS bundle sizes');
    }
    if (metrics.LCP > 2500) {
      recommendations.push('Improve Largest Contentful Paint - optimize images and fonts loading');
    }
    if (metrics.FID > 100) {
      recommendations.push('Reduce First Input Delay - minimize main thread blocking');
    }
    if (metrics.CLS > 0.1) {
      recommendations.push('Fix Cumulative Layout Shift - ensure proper sizing for dynamic content');
    }
    if (metrics.TTFB > 800) {
      recommendations.push('Optimize Time to First Byte - improve server response times');
    }
    if (metrics.bundleSize > 1000000) {
      recommendations.push('Reduce bundle size - implement code splitting and tree shaking');
    }
    if (metrics.cacheHitRate < 0.8) {
      recommendations.push('Improve caching strategy - increase cache hit rate');
    }

    return recommendations;
  }

  /**
   * Clear all observers
   */
  public destroy(): void {
    this.observers.forEach((observer) => {
      observer.disconnect();
    });
    this.observers.clear();
    this.removeAllListeners();
  }
}

// Export singleton instance
export const performanceService = new PerformanceService();
export default performanceService;