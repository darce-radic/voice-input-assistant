import { useEffect, useRef, useState, useMemo, useCallback } from 'react';
import { performanceMonitor, connectionMonitor } from '../utils/pwaUtils';

// Hook for lazy loading components with Intersection Observer
export const useLazyLoading = <T extends HTMLElement>(threshold = 0.1) => {
  const [isVisible, setIsVisible] = useState(false);
  const ref = useRef<T>(null);

  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setIsVisible(true);
          observer.disconnect();
        }
      },
      { threshold }
    );

    if (ref.current) {
      observer.observe(ref.current);
    }

    return () => observer.disconnect();
  }, [threshold]);

  return { ref, isVisible };
};

// Hook for image lazy loading with placeholder
export const useImageLazyLoading = (src: string, placeholder?: string) => {
  const [imageSrc, setImageSrc] = useState(placeholder || '');
  const [isLoaded, setIsLoaded] = useState(false);
  const [isError, setIsError] = useState(false);
  const { ref, isVisible } = useLazyLoading();

  useEffect(() => {
    if (isVisible && src) {
      const img = new Image();
      img.onload = () => {
        setImageSrc(src);
        setIsLoaded(true);
      };
      img.onerror = () => {
        setIsError(true);
      };
      img.src = src;
    }
  }, [isVisible, src]);

  return { ref, imageSrc, isLoaded, isError };
};

// Hook for performance monitoring
export const usePerformanceMonitor = (componentName: string) => {
  const startTimeRef = useRef<number>();

  useEffect(() => {
    startTimeRef.current = performance.now();
    performanceMonitor.startMeasure(`${componentName}-mount`);

    return () => {
      if (startTimeRef.current) {
        const duration = performanceMonitor.endMeasure(`${componentName}-mount`);
        console.log(`${componentName} mount duration: ${duration.toFixed(2)}ms`);
      }
    };
  }, [componentName]);

  const measureRender = useCallback((renderName: string) => {
    performanceMonitor.startMeasure(`${componentName}-${renderName}`);
    
    return () => {
      const duration = performanceMonitor.endMeasure(`${componentName}-${renderName}`);
      console.log(`${componentName} ${renderName} duration: ${duration.toFixed(2)}ms`);
    };
  }, [componentName]);

  return { measureRender };
};

// Hook for connection-aware data loading
export const useConnectionAwareLoading = () => {
  const [isOnline, setIsOnline] = useState(connectionMonitor.isOnline());
  const [connectionSpeed, setConnectionSpeed] = useState(connectionMonitor.getConnectionSpeed());

  useEffect(() => {
    const unsubscribe = connectionMonitor.onStatusChange((online) => {
      setIsOnline(online);
      setConnectionSpeed(connectionMonitor.getConnectionSpeed());
    });

    return unsubscribe;
  }, []);

  const shouldPreload = useMemo(() => {
    if (!isOnline) return false;
    
    // Only preload on fast connections
    return ['4g', '3g'].includes(connectionSpeed);
  }, [isOnline, connectionSpeed]);

  const getLoadingStrategy = useCallback((priority: 'high' | 'medium' | 'low') => {
    if (!isOnline) return 'cache-only';
    
    switch (connectionSpeed) {
      case 'slow-2g':
      case '2g':
        return priority === 'high' ? 'network-first' : 'cache-first';
      case '3g':
        return priority === 'low' ? 'cache-first' : 'network-first';
      case '4g':
      default:
        return 'network-first';
    }
  }, [isOnline, connectionSpeed]);

  return { isOnline, connectionSpeed, shouldPreload, getLoadingStrategy };
};

// Hook for debounced values
export const useDebounce = <T>(value: T, delay: number): T => {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
};

// Hook for throttled callbacks
export const useThrottle = <T extends (...args: any[]) => any>(
  callback: T,
  delay: number
): T => {
  const lastRun = useRef(Date.now());

  return useCallback(
    ((...args) => {
      if (Date.now() - lastRun.current >= delay) {
        callback(...args);
        lastRun.current = Date.now();
      }
    }) as T,
    [callback, delay]
  );
};

// Hook for virtual scrolling
export const useVirtualScrolling = <T>(
  items: T[],
  itemHeight: number,
  containerHeight: number
) => {
  const [scrollTop, setScrollTop] = useState(0);
  
  const startIndex = Math.floor(scrollTop / itemHeight);
  const endIndex = Math.min(
    startIndex + Math.ceil(containerHeight / itemHeight) + 1,
    items.length - 1
  );

  const visibleItems = useMemo(() => {
    return items.slice(startIndex, endIndex + 1).map((item, index) => ({
      item,
      index: startIndex + index,
      style: {
        position: 'absolute' as const,
        top: (startIndex + index) * itemHeight,
        height: itemHeight,
        width: '100%',
      },
    }));
  }, [items, startIndex, endIndex, itemHeight]);

  const totalHeight = items.length * itemHeight;

  const handleScroll = useThrottle((e: React.UIEvent<HTMLDivElement>) => {
    setScrollTop(e.currentTarget.scrollTop);
  }, 16); // ~60fps

  return {
    visibleItems,
    totalHeight,
    handleScroll,
    containerStyle: {
      height: containerHeight,
      overflow: 'auto' as const,
      position: 'relative' as const,
    },
  };
};

// Hook for code splitting with retry mechanism
export const useCodeSplitting = () => {
  const [retryCount, setRetryCount] = useState(0);
  const maxRetries = 3;

  const loadComponent = useCallback(
    async (importFunc: () => Promise<any>) => {
      try {
        return await importFunc();
      } catch (error) {
        if (retryCount < maxRetries) {
          console.warn(`Code splitting failed, retry ${retryCount + 1}/${maxRetries}:`, error);
          setRetryCount(prev => prev + 1);
          
          // Wait before retry with exponential backoff
          await new Promise(resolve => setTimeout(resolve, Math.pow(2, retryCount) * 1000));
          
          return loadComponent(importFunc);
        }
        
        throw error;
      }
    },
    [retryCount, maxRetries]
  );

  return { loadComponent, retryCount, isRetrying: retryCount > 0 };
};

// Hook for memory usage monitoring
export const useMemoryMonitor = () => {
  const [memoryInfo, setMemoryInfo] = useState<any>(null);

  useEffect(() => {
    const updateMemoryInfo = () => {
      if ('memory' in performance) {
        const memory = (performance as any).memory;
        setMemoryInfo({
          usedJSHeapSize: memory.usedJSHeapSize,
          totalJSHeapSize: memory.totalJSHeapSize,
          jsHeapSizeLimit: memory.jsHeapSizeLimit,
          usagePercentage: (memory.usedJSHeapSize / memory.jsHeapSizeLimit) * 100,
        });
      }
    };

    updateMemoryInfo();
    const interval = setInterval(updateMemoryInfo, 5000); // Update every 5 seconds

    return () => clearInterval(interval);
  }, []);

  const formatBytes = useCallback((bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }, []);

  return {
    memoryInfo,
    formatBytes,
    isHighUsage: memoryInfo ? memoryInfo.usagePercentage > 80 : false,
  };
};

// Hook for adaptive loading based on device capabilities
export const useAdaptiveLoading = () => {
  const [deviceCapabilities, setDeviceCapabilities] = useState({
    hardwareConcurrency: navigator.hardwareConcurrency || 2,
    deviceMemory: (navigator as any).deviceMemory || 4,
    connection: connectionMonitor.getNetworkInfo(),
  });

  const getLoadingPriority = useCallback(() => {
    const { hardwareConcurrency, deviceMemory, connection } = deviceCapabilities;
    
    // Score based on device capabilities
    let score = 0;
    
    // CPU cores
    if (hardwareConcurrency >= 8) score += 3;
    else if (hardwareConcurrency >= 4) score += 2;
    else score += 1;
    
    // RAM
    if (deviceMemory >= 8) score += 3;
    else if (deviceMemory >= 4) score += 2;
    else score += 1;
    
    // Network
    if (connection?.effectiveType === '4g') score += 3;
    else if (connection?.effectiveType === '3g') score += 2;
    else score += 1;
    
    // Determine loading strategy based on total score
    if (score >= 7) return 'aggressive'; // High-end device
    if (score >= 5) return 'balanced';   // Mid-range device
    return 'conservative';               // Low-end device
  }, [deviceCapabilities]);

  const getOptimalChunkSize = useCallback(() => {
    const priority = getLoadingPriority();
    
    switch (priority) {
      case 'aggressive':
        return { images: 10, components: 5, data: 100 };
      case 'balanced':
        return { images: 5, components: 3, data: 50 };
      case 'conservative':
        return { images: 2, components: 1, data: 20 };
      default:
        return { images: 5, components: 3, data: 50 };
    }
  }, [getLoadingPriority]);

  return {
    deviceCapabilities,
    loadingPriority: getLoadingPriority(),
    optimalChunkSize: getOptimalChunkSize(),
  };
};

// Hook for resource preloading
export const useResourcePreloader = () => {
  const preloadedResources = useRef(new Set<string>());

  const preloadImage = useCallback((src: string, priority: 'high' | 'low' = 'low') => {
    if (preloadedResources.current.has(src)) return;

    const link = document.createElement('link');
    link.rel = 'preload';
    link.as = 'image';
    link.href = src;
    if (priority === 'high') {
      link.fetchPriority = 'high';
    }
    
    document.head.appendChild(link);
    preloadedResources.current.add(src);
  }, []);

  const preloadScript = useCallback((src: string) => {
    if (preloadedResources.current.has(src)) return;

    const link = document.createElement('link');
    link.rel = 'preload';
    link.as = 'script';
    link.href = src;
    
    document.head.appendChild(link);
    preloadedResources.current.add(src);
  }, []);

  const preloadData = useCallback(async (url: string) => {
    if (preloadedResources.current.has(url)) return;

    try {
      const response = await fetch(url);
      if (response.ok) {
        preloadedResources.current.add(url);
      }
    } catch (error) {
      console.warn('Failed to preload data:', url, error);
    }
  }, []);

  return { preloadImage, preloadScript, preloadData };
};

export default {
  useLazyLoading,
  useImageLazyLoading,
  usePerformanceMonitor,
  useConnectionAwareLoading,
  useDebounce,
  useThrottle,
  useVirtualScrolling,
  useCodeSplitting,
  useMemoryMonitor,
  useAdaptiveLoading,
  useResourcePreloader,
};