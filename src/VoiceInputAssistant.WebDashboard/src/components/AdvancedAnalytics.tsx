import React, { useState, useEffect, useMemo } from 'react';
import { useLazyLoading, useConnectionAwareLoading, useVirtualScrolling } from '../hooks/usePerformanceOptimization';
import { offlineManager, DeviceFeatures } from '../utils/pwaUtils';
import config from '../services/config';
import api from '../services/api';

interface AnalyticsData {
  id: string;
  timestamp: number;
  recognitionAccuracy: number;
  processingTime: number;
  engineType: string;
  confidence: number;
  wordCount: number;
  errorCount: number;
  userId: string;
}

interface MetricCardProps {
  title: string;
  value: string | number;
  trend: 'up' | 'down' | 'stable';
  percentage?: number;
  icon: string;
  loading?: boolean;
}

interface ChartData {
  labels: string[];
  datasets: {
    label: string;
    data: number[];
    borderColor: string;
    backgroundColor: string;
    fill?: boolean;
  }[];
}

const MetricCard: React.FC<MetricCardProps> = ({ title, value, trend, percentage, icon, loading = false }) => {
  const { ref, isVisible } = useLazyLoading<HTMLDivElement>();

  const getTrendColor = () => {
    switch (trend) {
      case 'up': return '#10b981';
      case 'down': return '#ef4444';
      default: return '#6b7280';
    }
  };

  const getTrendIcon = () => {
    switch (trend) {
      case 'up': return '📈';
      case 'down': return '📉';
      default: return '➡️';
    }
  };

  if (loading) {
    return (
      <div ref={ref} className="metric-card loading">
        <div className="skeleton-icon"></div>
        <div className="skeleton-content">
          <div className="skeleton-text"></div>
          <div className="skeleton-value"></div>
        </div>
      </div>
    );
  }

  return (
    <div ref={ref} className={`metric-card ${isVisible ? 'visible' : ''}`}>
      <div className="metric-icon">{icon}</div>
      <div className="metric-content">
        <h3 className="metric-title">{title}</h3>
        <div className="metric-value">{value}</div>
        {percentage !== undefined && (
          <div className="metric-trend" style={{ color: getTrendColor() }}>
            <span className="trend-icon">{getTrendIcon()}</span>
            <span className="trend-percentage">{percentage}%</span>
          </div>
        )}
      </div>

    </div>
  );
};

const VirtualizedDataTable: React.FC<{ data: AnalyticsData[] }> = ({ data }) => {
  const containerHeight = 400;
  const itemHeight = 60;
  const { visibleItems, totalHeight, handleScroll, containerStyle } = useVirtualScrolling(
    data,
    itemHeight,
    containerHeight
  );

  const formatTimestamp = (timestamp: number) => {
    return new Date(timestamp).toLocaleString();
  };

  const getAccuracyColor = (accuracy: number) => {
    if (accuracy >= 90) return '#10b981';
    if (accuracy >= 75) return '#f59e0b';
    return '#ef4444';
  };

  return (
    <div className="data-table-container">
      <div className="table-header">
        <div className="header-cell">Timestamp</div>
        <div className="header-cell">Engine</div>
        <div className="header-cell">Accuracy</div>
        <div className="header-cell">Processing Time</div>
        <div className="header-cell">Confidence</div>
      </div>
      <div style={containerStyle} onScroll={handleScroll}>
        <div style={{ height: totalHeight, position: 'relative' }}>
          {visibleItems.map(({ item, index, style }) => (
            <div key={item.id} style={style} className="table-row">
              <div className="table-cell">{formatTimestamp(item.timestamp)}</div>
              <div className="table-cell">
                <span className="engine-badge">{item.engineType}</span>
              </div>
              <div className="table-cell">
                <span
                  className="accuracy-badge"
                  style={{ color: getAccuracyColor(item.recognitionAccuracy) }}
                >
                  {item.recognitionAccuracy}%
                </span>
              </div>
              <div className="table-cell">{item.processingTime}ms</div>
              <div className="table-cell">{Math.round(item.confidence * 100)}%</div>
            </div>
          ))}
        </div>
      </div>

    </div>
  );
};

const AdvancedAnalytics: React.FC = () => {
  const [analyticsData, setAnalyticsData] = useState<AnalyticsData[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedTimeRange, setSelectedTimeRange] = useState('7d');
  const [selectedEngine, setSelectedEngine] = useState('all');
  const [exportFormat, setExportFormat] = useState('csv');
  
  const { isOnline, getLoadingStrategy } = useConnectionAwareLoading();
  const { ref: headerRef, isVisible: headerVisible } = useLazyLoading<HTMLElement>();

  useEffect(() => {
    loadAnalyticsData();
  }, [selectedTimeRange, selectedEngine]);

  const loadAnalyticsData = async () => {
    setLoading(true);
    try {
      let data: AnalyticsData[] = [];

      if (isOnline) {
        // Try to fetch from API first
        const strategy = getLoadingStrategy('high');
        const response = await api.get(`/api/analytics/detailed?range=${selectedTimeRange}&engine=${selectedEngine}`);
        
        if (response.status === 200) {
          data = response.data;
          // Cache the data for offline use
          await offlineManager.storeData('analytics', data);
        } else {
          throw new Error('API request failed');
        }
      } else {
        // Load from offline cache
        data = await offlineManager.getData('analytics') || [];
      }

      setAnalyticsData(data);
    } catch (error) {
      console.error('Failed to load analytics data:', error);
      
      // Fallback to cached data
      const cachedData = await offlineManager.getData('analytics') || [];
      setAnalyticsData(cachedData);
      
      if (!isOnline) {
        await DeviceFeatures.showNotification('Offline Mode', {
          body: 'Showing cached analytics data. Some data may be outdated.',
          icon: '/icons/icon-192x192.png'
        });
      }
    } finally {
      setLoading(false);
    }
  };

  const metrics = useMemo(() => {
    if (!analyticsData.length) return null;

    const totalRecognitions = analyticsData.length;
    const avgAccuracy = analyticsData.reduce((sum, item) => sum + item.recognitionAccuracy, 0) / totalRecognitions;
    const avgProcessingTime = analyticsData.reduce((sum, item) => sum + item.processingTime, 0) / totalRecognitions;
    const totalWords = analyticsData.reduce((sum, item) => sum + item.wordCount, 0);
    const totalErrors = analyticsData.reduce((sum, item) => sum + item.errorCount, 0);

    // Calculate trends (mock calculation for demo)
    const recentData = analyticsData.slice(-Math.floor(totalRecognitions / 2));
    const oldData = analyticsData.slice(0, Math.floor(totalRecognitions / 2));

    const recentAvgAccuracy = recentData.reduce((sum, item) => sum + item.recognitionAccuracy, 0) / recentData.length;
    const oldAvgAccuracy = oldData.reduce((sum, item) => sum + item.recognitionAccuracy, 0) / oldData.length;
    const accuracyTrend = recentAvgAccuracy > oldAvgAccuracy ? 'up' : recentAvgAccuracy < oldAvgAccuracy ? 'down' : 'stable';
    const accuracyPercentage = Math.abs(((recentAvgAccuracy - oldAvgAccuracy) / oldAvgAccuracy) * 100);

    return {
      totalRecognitions,
      avgAccuracy: Math.round(avgAccuracy * 100) / 100,
      avgProcessingTime: Math.round(avgProcessingTime),
      totalWords,
      errorRate: Math.round((totalErrors / totalWords) * 10000) / 100,
      accuracyTrend: accuracyTrend as 'up' | 'down' | 'stable',
      accuracyPercentage: Math.round(accuracyPercentage * 100) / 100
    };
  }, [analyticsData]);

  const handleExport = async () => {
    try {
      let content: string;
      let filename: string;
      let mimeType: string;

      switch (exportFormat) {
        case 'json':
          content = JSON.stringify(analyticsData, null, 2);
          filename = `analytics-${Date.now()}.json`;
          mimeType = 'application/json';
          break;
        case 'csv':
        default:
          const headers = ['Timestamp', 'Engine', 'Accuracy', 'Processing Time', 'Confidence', 'Word Count', 'Errors'];
          const csvRows = [
            headers.join(','),
            ...analyticsData.map(item => [
              new Date(item.timestamp).toISOString(),
              item.engineType,
              item.recognitionAccuracy,
              item.processingTime,
              item.confidence,
              item.wordCount,
              item.errorCount
            ].join(','))
          ];
          content = csvRows.join('\n');
          filename = `analytics-${Date.now()}.csv`;
          mimeType = 'text/csv';
          break;
      }

      // Try to use the Web Share API first
      if ('share' in navigator) {
        const file = new File([content], filename, { type: mimeType });
        await (navigator as any).share({
          title: 'Voice Assistant Analytics',
          files: [file]
        });
      } else {
        // Fallback to download
        const blob = new Blob([content], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
      }

      await DeviceFeatures.showNotification('Export Complete', {
        body: `Analytics data exported as ${filename}`,
        icon: '/icons/icon-192x192.png'
      });

    } catch (error) {
      console.error('Export failed:', error);
      await DeviceFeatures.showNotification('Export Failed', {
        body: 'Could not export analytics data',
        icon: '/icons/icon-192x192.png'
      });
    }
  };

  const timeRangeOptions = [
    { value: '24h', label: 'Last 24 Hours' },
    { value: '7d', label: 'Last 7 Days' },
    { value: '30d', label: 'Last 30 Days' },
    { value: '90d', label: 'Last 90 Days' }
  ];

  const engineOptions = [
    { value: 'all', label: 'All Engines' },
    { value: 'whisper', label: 'Whisper' },
    { value: 'google', label: 'Google Speech' },
    { value: 'azure', label: 'Azure Speech' },
    { value: 'aws', label: 'AWS Transcribe' }
  ];

  return (
    <div className="advanced-analytics">
      <header ref={headerRef} className={`analytics-header ${headerVisible ? 'visible' : ''}`}>
        <div className="header-content">
          <h1>Advanced Analytics</h1>
          <p>Detailed insights into voice recognition performance</p>
          {!isOnline && (
            <div className="offline-notice">
              📡 Offline Mode - Showing cached data
            </div>
          )}
        </div>
        
        <div className="analytics-controls">
          <div className="control-group">
            <label>Time Range:</label>
            <select 
              value={selectedTimeRange} 
              onChange={(e) => setSelectedTimeRange(e.target.value)}
            >
              {timeRangeOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          <div className="control-group">
            <label>Engine:</label>
            <select 
              value={selectedEngine} 
              onChange={(e) => setSelectedEngine(e.target.value)}
            >
              {engineOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          <div className="control-group">
            <label>Export:</label>
            <select 
              value={exportFormat} 
              onChange={(e) => setExportFormat(e.target.value)}
            >
              <option value="csv">CSV</option>
              <option value="json">JSON</option>
            </select>
            <button onClick={handleExport} className="export-btn">
              📤 Export
            </button>
          </div>
        </div>
      </header>

      <div className="metrics-grid">
        <MetricCard
          title="Total Recognitions"
          value={metrics?.totalRecognitions || 0}
          trend="stable"
          icon="🎯"
          loading={loading}
        />
        <MetricCard
          title="Average Accuracy"
          value={`${metrics?.avgAccuracy || 0}%`}
          trend={metrics?.accuracyTrend || 'stable'}
          percentage={metrics?.accuracyPercentage}
          icon="🎯"
          loading={loading}
        />
        <MetricCard
          title="Avg Processing Time"
          value={`${metrics?.avgProcessingTime || 0}ms`}
          trend="down"
          percentage={12.5}
          icon="⚡"
          loading={loading}
        />
        <MetricCard
          title="Total Words"
          value={metrics?.totalWords?.toLocaleString() || 0}
          trend="up"
          percentage={8.3}
          icon="📝"
          loading={loading}
        />
        <MetricCard
          title="Error Rate"
          value={`${metrics?.errorRate || 0}%`}
          trend="down"
          percentage={15.2}
          icon="🚫"
          loading={loading}
        />
      </div>

      <div className="data-section">
        <div className="section-header">
          <h2>Detailed Recognition Data</h2>
          <div className="data-info">
            {analyticsData.length} records • Last updated: {new Date().toLocaleTimeString()}
          </div>
        </div>
        <VirtualizedDataTable data={analyticsData} />
      </div>

    </div>
  );
};

export default AdvancedAnalytics;