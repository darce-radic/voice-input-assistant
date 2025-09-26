import { EventEmitter } from 'events';

// Cache strategy types
export enum CacheStrategy {
  CACHE_FIRST = 'cache-first',
  NETWORK_FIRST = 'network-first',
  CACHE_ONLY = 'cache-only',
  NETWORK_ONLY = 'network-only',
  STALE_WHILE_REVALIDATE = 'stale-while-revalidate'
}

// Cache configuration
export interface CacheConfig {
  name: string;
  version: string;
  strategy: CacheStrategy;
  maxAge: number; // in milliseconds
  maxEntries: number;
  networkTimeoutMs?: number;
  updateTriggers?: string[];
}

// Cached item metadata
export interface CacheItem {
  url: string;
  data: any;
  timestamp: number;
  etag?: string;
  contentType?: string;
  size: number;
  hits: number;
  strategy: CacheStrategy;
}

// Cache statistics
export interface CacheStats {
  totalSize: number;
  itemCount: number;
  hitRate: number;
  missRate: number;
  lastCleanup: number;
  cacheNames: string[];
}

// Sync queue item for offline operations
export interface SyncQueueItem {
  id: string;
  url: string;
  method: string;
  data: any;
  timestamp: number;
  retryCount: number;
  maxRetries: number;
  priority: number;
}

class OfflineCacheService extends EventEmitter {
  private cacheConfigs: Map<string, CacheConfig> = new Map();
  private stats: Map<string, { hits: number; misses: number }> = new Map();
  private syncQueue: SyncQueueItem[] = [];
  private isOnline: boolean = navigator.onLine;
  private cleanupTimer: NodeJS.Timeout | null = null;
  private syncTimer: NodeJS.Timeout | null = null;

  constructor() {
    super();
    this.initializeDefaultCaches();
    this.setupEventListeners();
    this.startPeriodicCleanup();
    this.startSyncProcessor();
  }

  /**
   * Initialize default cache configurations
   */
  private initializeDefaultCaches(): void {
    // API Response Cache
    this.registerCache({
      name: 'api-responses',
      version: '1.0',
      strategy: CacheStrategy.STALE_WHILE_REVALIDATE,
      maxAge: 5 * 60 * 1000, // 5 minutes
      maxEntries: 100,
      networkTimeoutMs: 3000
    });

    // Static Assets Cache
    this.registerCache({
      name: 'static-assets',
      version: '1.0',
      strategy: CacheStrategy.CACHE_FIRST,
      maxAge: 24 * 60 * 60 * 1000, // 24 hours
      maxEntries: 200,
      networkTimeoutMs: 5000
    });

    // User Data Cache
    this.registerCache({
      name: 'user-data',
      version: '1.0',
      strategy: CacheStrategy.NETWORK_FIRST,
      maxAge: 10 * 60 * 1000, // 10 minutes
      maxEntries: 50,
      networkTimeoutMs: 2000
    });

    // ML Models Cache
    this.registerCache({
      name: 'ml-models',
      version: '1.0',
      strategy: CacheStrategy.CACHE_FIRST,
      maxAge: 7 * 24 * 60 * 60 * 1000, // 7 days
      maxEntries: 20,
      networkTimeoutMs: 10000
    });

    // Audio Files Cache
    this.registerCache({
      name: 'audio-files',
      version: '1.0',
      strategy: CacheStrategy.CACHE_FIRST,
      maxAge: 60 * 60 * 1000, // 1 hour
      maxEntries: 100,
      networkTimeoutMs: 8000
    });
  }

  /**
   * Setup event listeners for online/offline detection
   */
  private setupEventListeners(): void {
    window.addEventListener('online', () => {
      this.isOnline = true;
      this.emit('online');
      this.processSyncQueue();
    });

    window.addEventListener('offline', () => {
      this.isOnline = false;
      this.emit('offline');
    });

    // Listen for visibility changes to sync when app becomes visible
    document.addEventListener('visibilitychange', () => {
      if (!document.hidden && this.isOnline) {
        this.processSyncQueue();
      }
    });
  }

  /**
   * Register a new cache configuration
   */
  public registerCache(config: CacheConfig): void {
    this.cacheConfigs.set(config.name, config);
    this.stats.set(config.name, { hits: 0, misses: 0 });
    this.emit('cache-registered', config);
  }

  /**
   * Make a cached request
   */
  public async request(url: string, options: RequestInit = {}, cacheName: string = 'api-responses'): Promise<Response> {
    const config = this.cacheConfigs.get(cacheName);
    if (!config) {
      throw new Error(`Cache configuration '${cacheName}' not found`);
    }

    const cacheKey = this.generateCacheKey(url, options);
    
    switch (config.strategy) {
      case CacheStrategy.CACHE_FIRST:
        return this.cacheFirstStrategy(url, options, config, cacheKey);
      case CacheStrategy.NETWORK_FIRST:
        return this.networkFirstStrategy(url, options, config, cacheKey);
      case CacheStrategy.CACHE_ONLY:
        return this.cacheOnlyStrategy(url, options, config, cacheKey);
      case CacheStrategy.NETWORK_ONLY:
        return this.networkOnlyStrategy(url, options, config);
      case CacheStrategy.STALE_WHILE_REVALIDATE:
        return this.staleWhileRevalidateStrategy(url, options, config, cacheKey);
      default:
        return this.networkFirstStrategy(url, options, config, cacheKey);
    }
  }

  /**
   * Cache-first strategy implementation
   */
  private async cacheFirstStrategy(url: string, options: RequestInit, config: CacheConfig, cacheKey: string): Promise<Response> {
    const cache = await caches.open(config.name);
    const cachedResponse = await cache.match(cacheKey);

    if (cachedResponse && this.isCacheValid(cachedResponse, config)) {
      this.recordCacheHit(config.name);
      return cachedResponse;
    }

    try {
      const networkResponse = await this.fetchWithTimeout(url, options, config.networkTimeoutMs);
      await this.cacheResponse(cache, cacheKey, networkResponse.clone(), config);
      this.recordCacheMiss(config.name);
      return networkResponse;
    } catch (error) {
      if (cachedResponse) {
        this.emit('stale-cache-served', { url, error });
        return cachedResponse;
      }
      throw error;
    }
  }

  /**
   * Network-first strategy implementation
   */
  private async networkFirstStrategy(url: string, options: RequestInit, config: CacheConfig, cacheKey: string): Promise<Response> {
    try {
      const networkResponse = await this.fetchWithTimeout(url, options, config.networkTimeoutMs);
      const cache = await caches.open(config.name);
      await this.cacheResponse(cache, cacheKey, networkResponse.clone(), config);
      this.recordCacheMiss(config.name);
      return networkResponse;
    } catch (error) {
      const cache = await caches.open(config.name);
      const cachedResponse = await cache.match(cacheKey);
      
      if (cachedResponse) {
        this.recordCacheHit(config.name);
        this.emit('network-failed-cache-served', { url, error });
        return cachedResponse;
      }
      
      // Queue for sync if this was a write operation
      if (options.method && ['POST', 'PUT', 'PATCH', 'DELETE'].includes(options.method.toUpperCase())) {
        this.queueForSync(url, options);
      }
      
      throw error;
    }
  }

  /**
   * Cache-only strategy implementation
   */
  private async cacheOnlyStrategy(url: string, options: RequestInit, config: CacheConfig, cacheKey: string): Promise<Response> {
    const cache = await caches.open(config.name);
    const cachedResponse = await cache.match(cacheKey);

    if (cachedResponse) {
      this.recordCacheHit(config.name);
      return cachedResponse;
    }

    this.recordCacheMiss(config.name);
    throw new Error(`No cached response found for ${url}`);
  }

  /**
   * Network-only strategy implementation
   */
  private async networkOnlyStrategy(url: string, options: RequestInit, config: CacheConfig): Promise<Response> {
    const response = await this.fetchWithTimeout(url, options, config.networkTimeoutMs);
    this.recordCacheMiss(config.name);
    return response;
  }

  /**
   * Stale-while-revalidate strategy implementation
   */
  private async staleWhileRevalidateStrategy(url: string, options: RequestInit, config: CacheConfig, cacheKey: string): Promise<Response> {
    const cache = await caches.open(config.name);
    const cachedResponse = await cache.match(cacheKey);

    // Always try to revalidate in the background
    const revalidate = async () => {
      try {
        const networkResponse = await this.fetchWithTimeout(url, options, config.networkTimeoutMs);
        await this.cacheResponse(cache, cacheKey, networkResponse.clone(), config);
        this.emit('cache-revalidated', { url, cacheKey });
      } catch (error) {
        this.emit('revalidation-failed', { url, error });
      }
    };

    if (cachedResponse && this.isCacheValid(cachedResponse, config)) {
      this.recordCacheHit(config.name);
      // Revalidate in background
      revalidate();
      return cachedResponse;
    }

    try {
      const networkResponse = await this.fetchWithTimeout(url, options, config.networkTimeoutMs);
      await this.cacheResponse(cache, cacheKey, networkResponse.clone(), config);
      this.recordCacheMiss(config.name);
      return networkResponse;
    } catch (error) {
      if (cachedResponse) {
        this.recordCacheHit(config.name);
        this.emit('stale-cache-served', { url, error });
        return cachedResponse;
      }
      throw error;
    }
  }

  /**
   * Fetch with timeout
   */
  private async fetchWithTimeout(url: string, options: RequestInit, timeoutMs?: number): Promise<Response> {
    if (!timeoutMs) {
      return fetch(url, options);
    }

    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

    try {
      const response = await fetch(url, {
        ...options,
        signal: controller.signal
      });
      clearTimeout(timeoutId);
      return response;
    } catch (error) {
      clearTimeout(timeoutId);
      throw error;
    }
  }

  /**
   * Cache a response
   */
  private async cacheResponse(cache: Cache, cacheKey: string, response: Response, config: CacheConfig): Promise<void> {
    if (!response.ok) {
      return; // Don't cache error responses
    }

    // Add metadata headers
    const responseToCache = new Response(response.body, {
      status: response.status,
      statusText: response.statusText,
      headers: {
        ...Object.fromEntries(response.headers.entries()),
        'x-cache-timestamp': Date.now().toString(),
        'x-cache-strategy': config.strategy
      }
    });

    await cache.put(cacheKey, responseToCache);
    
    // Enforce cache size limits
    await this.enforceMaxEntries(cache, config);
  }

  /**
   * Check if cached response is still valid
   */
  private isCacheValid(response: Response, config: CacheConfig): boolean {
    const cacheTimestamp = response.headers.get('x-cache-timestamp');
    if (!cacheTimestamp) {
      return false;
    }

    const age = Date.now() - parseInt(cacheTimestamp);
    return age < config.maxAge;
  }

  /**
   * Generate cache key from URL and options
   */
  private generateCacheKey(url: string, options: RequestInit): string {
    const method = options.method || 'GET';
    const headers = JSON.stringify(options.headers || {});
    const body = options.body ? JSON.stringify(options.body) : '';
    
    return `${method}:${url}:${btoa(headers + body)}`;
  }

  /**
   * Enforce maximum entries in cache
   */
  private async enforceMaxEntries(cache: Cache, config: CacheConfig): Promise<void> {
    const keys = await cache.keys();
    
    if (keys.length > config.maxEntries) {
      // Remove oldest entries
      const responses = await Promise.all(keys.map(key => cache.match(key)));
      const entries = keys.map((key, index) => ({
        key,
        response: responses[index],
        timestamp: parseInt(responses[index]?.headers.get('x-cache-timestamp') || '0')
      }));

      entries.sort((a, b) => a.timestamp - b.timestamp);
      
      const toDelete = entries.slice(0, keys.length - config.maxEntries);
      await Promise.all(toDelete.map(entry => cache.delete(entry.key)));
      
      this.emit('cache-cleanup', { 
        cacheName: config.name, 
        deletedCount: toDelete.length,
        remainingCount: config.maxEntries
      });
    }
  }

  /**
   * Queue operation for sync when back online
   */
  private queueForSync(url: string, options: RequestInit): void {
    const syncItem: SyncQueueItem = {
      id: this.generateSyncId(),
      url,
      method: options.method || 'GET',
      data: options.body,
      timestamp: Date.now(),
      retryCount: 0,
      maxRetries: 3,
      priority: this.getSyncPriority(options.method || 'GET')
    };

    this.syncQueue.push(syncItem);
    this.syncQueue.sort((a, b) => b.priority - a.priority); // Higher priority first
    
    this.saveSyncQueue();
    this.emit('sync-queued', syncItem);
  }

  /**
   * Process sync queue when back online
   */
  private async processSyncQueue(): Promise<void> {
    if (!this.isOnline || this.syncQueue.length === 0) {
      return;
    }

    this.emit('sync-started', { queueLength: this.syncQueue.length });

    const toSync = [...this.syncQueue];
    this.syncQueue = [];

    for (const item of toSync) {
      try {
        await this.executeSyncItem(item);
        this.emit('sync-success', item);
      } catch (error) {
        if (item.retryCount < item.maxRetries) {
          item.retryCount++;
          this.syncQueue.push(item);
          this.emit('sync-retry', { item, error });
        } else {
          this.emit('sync-failed', { item, error });
        }
      }
    }

    this.saveSyncQueue();
    this.emit('sync-completed', { 
      processed: toSync.length, 
      remaining: this.syncQueue.length 
    });
  }

  /**
   * Execute a sync queue item
   */
  private async executeSyncItem(item: SyncQueueItem): Promise<void> {
    const options: RequestInit = {
      method: item.method,
      body: item.data,
      headers: {
        'Content-Type': 'application/json',
      }
    };

    const response = await fetch(item.url, options);
    
    if (!response.ok) {
      throw new Error(`Sync failed: ${response.status} ${response.statusText}`);
    }
  }

  /**
   * Get sync priority based on HTTP method
   */
  private getSyncPriority(method: string): number {
    switch (method.toUpperCase()) {
      case 'DELETE': return 100;
      case 'POST': return 80;
      case 'PUT': return 70;
      case 'PATCH': return 60;
      default: return 50;
    }
  }

  /**
   * Generate unique sync ID
   */
  private generateSyncId(): string {
    return `sync_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Save sync queue to localStorage
   */
  private saveSyncQueue(): void {
    try {
      localStorage.setItem('offline_sync_queue', JSON.stringify(this.syncQueue));
    } catch (error) {
      console.warn('Failed to save sync queue:', error);
    }
  }

  /**
   * Load sync queue from localStorage
   */
  private loadSyncQueue(): void {
    try {
      const saved = localStorage.getItem('offline_sync_queue');
      if (saved) {
        this.syncQueue = JSON.parse(saved);
      }
    } catch (error) {
      console.warn('Failed to load sync queue:', error);
      this.syncQueue = [];
    }
  }

  /**
   * Start periodic cleanup
   */
  private startPeriodicCleanup(): void {
    this.cleanupTimer = setInterval(() => {
      this.performCleanup();
    }, 30 * 60 * 1000); // Every 30 minutes
  }

  /**
   * Start sync processor
   */
  private startSyncProcessor(): void {
    this.loadSyncQueue();
    
    this.syncTimer = setInterval(() => {
      if (this.isOnline) {
        this.processSyncQueue();
      }
    }, 10 * 1000); // Every 10 seconds
  }

  /**
   * Perform cache cleanup
   */
  private async performCleanup(): Promise<void> {
    for (const [cacheName, config] of this.cacheConfigs) {
      try {
        const cache = await caches.open(cacheName);
        const keys = await cache.keys();
        let deletedCount = 0;

        for (const key of keys) {
          const response = await cache.match(key);
          if (response && !this.isCacheValid(response, config)) {
            await cache.delete(key);
            deletedCount++;
          }
        }

        if (deletedCount > 0) {
          this.emit('cleanup-completed', { cacheName, deletedCount });
        }
      } catch (error) {
        this.emit('cleanup-failed', { cacheName, error });
      }
    }
  }

  /**
   * Record cache hit
   */
  private recordCacheHit(cacheName: string): void {
    const stats = this.stats.get(cacheName);
    if (stats) {
      stats.hits++;
    }
  }

  /**
   * Record cache miss
   */
  private recordCacheMiss(cacheName: string): void {
    const stats = this.stats.get(cacheName);
    if (stats) {
      stats.misses++;
    }
  }

  /**
   * Get cache statistics
   */
  public async getCacheStats(): Promise<CacheStats> {
    let totalSize = 0;
    let itemCount = 0;
    const cacheNames: string[] = [];

    for (const [cacheName] of this.cacheConfigs) {
      try {
        const cache = await caches.open(cacheName);
        const keys = await cache.keys();
        itemCount += keys.length;
        cacheNames.push(cacheName);

        // Estimate size (rough calculation)
        for (const key of keys.slice(0, 10)) { // Sample first 10 items
          const response = await cache.match(key);
          if (response) {
            const text = await response.clone().text();
            totalSize += text.length;
          }
        }
        totalSize = totalSize * (keys.length / Math.min(keys.length, 10));
      } catch (error) {
        console.warn(`Failed to get stats for cache ${cacheName}:`, error);
      }
    }

    let totalHits = 0;
    let totalMisses = 0;

    for (const stats of this.stats.values()) {
      totalHits += stats.hits;
      totalMisses += stats.misses;
    }

    const total = totalHits + totalMisses;
    const hitRate = total > 0 ? totalHits / total : 0;
    const missRate = total > 0 ? totalMisses / total : 0;

    return {
      totalSize,
      itemCount,
      hitRate,
      missRate,
      lastCleanup: Date.now(),
      cacheNames
    };
  }

  /**
   * Clear specific cache
   */
  public async clearCache(cacheName: string): Promise<void> {
    try {
      await caches.delete(cacheName);
      this.stats.set(cacheName, { hits: 0, misses: 0 });
      this.emit('cache-cleared', { cacheName });
    } catch (error) {
      this.emit('cache-clear-failed', { cacheName, error });
      throw error;
    }
  }

  /**
   * Clear all caches
   */
  public async clearAllCaches(): Promise<void> {
    const cacheNames = await caches.keys();
    await Promise.all(cacheNames.map(name => caches.delete(name)));
    
    this.stats.clear();
    for (const cacheName of this.cacheConfigs.keys()) {
      this.stats.set(cacheName, { hits: 0, misses: 0 });
    }
    
    this.emit('all-caches-cleared');
  }

  /**
   * Get sync queue status
   */
  public getSyncQueueStatus(): { pending: number; items: SyncQueueItem[] } {
    return {
      pending: this.syncQueue.length,
      items: [...this.syncQueue]
    };
  }

  /**
   * Force sync queue processing
   */
  public async forceSyncQueue(): Promise<void> {
    if (this.isOnline) {
      await this.processSyncQueue();
    } else {
      throw new Error('Cannot sync while offline');
    }
  }

  /**
   * Check if online
   */
  public isOnlineStatus(): boolean {
    return this.isOnline;
  }

  /**
   * Preload critical resources
   */
  public async preloadResources(urls: string[], cacheName: string = 'static-assets'): Promise<void> {
    const cache = await caches.open(cacheName);
    const config = this.cacheConfigs.get(cacheName);
    
    if (!config) {
      throw new Error(`Cache configuration '${cacheName}' not found`);
    }

    const preloadPromises = urls.map(async (url) => {
      try {
        const response = await fetch(url);
        if (response.ok) {
          await this.cacheResponse(cache, url, response.clone(), config);
        }
      } catch (error) {
        console.warn(`Failed to preload ${url}:`, error);
      }
    });

    await Promise.all(preloadPromises);
    this.emit('preload-completed', { urls, cacheName });
  }

  /**
   * Cleanup resources
   */
  public destroy(): void {
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
      this.cleanupTimer = null;
    }
    
    if (this.syncTimer) {
      clearInterval(this.syncTimer);
      this.syncTimer = null;
    }

    this.removeAllListeners();
  }
}

// Export singleton instance
export const offlineCacheService = new OfflineCacheService();
export default offlineCacheService;