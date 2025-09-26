'use client';

import React, { useState } from 'react';

interface AnalyticsChartProps {}

const mockData = {
  daily: [
    { date: '2024-01-15', transcriptions: 45, accuracy: 94.2 },
    { date: '2024-01-16', transcriptions: 67, accuracy: 95.1 },
    { date: '2024-01-17', transcriptions: 52, accuracy: 93.8 },
    { date: '2024-01-18', transcriptions: 78, accuracy: 96.4 },
    { date: '2024-01-19', transcriptions: 91, accuracy: 95.7 },
    { date: '2024-01-20', transcriptions: 63, accuracy: 97.1 },
    { date: '2024-01-21', transcriptions: 84, accuracy: 94.9 },
  ],
  engines: [
    { name: 'Whisper Local', usage: 45, accuracy: 96.2 },
    { name: 'Azure Speech', usage: 30, accuracy: 94.8 },
    { name: 'OpenAI Whisper', usage: 25, accuracy: 97.1 },
  ],
};

export function AnalyticsChart({}: AnalyticsChartProps) {
  const [activeTab, setActiveTab] = useState<'usage' | 'accuracy' | 'engines'>('usage');
  const [timeRange, setTimeRange] = useState<'7d' | '30d' | '90d'>('7d');

  const getMaxValue = (data: any[], key: string) => {
    return Math.max(...data.map(item => item[key]));
  };

  const renderUsageChart = () => {
    const maxTranscriptions = getMaxValue(mockData.daily, 'transcriptions');
    
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h4 className="text-sm font-medium">Daily Transcriptions</h4>
          <div className="text-sm text-muted-foreground">
            Avg: {Math.round(mockData.daily.reduce((sum, day) => sum + day.transcriptions, 0) / mockData.daily.length)}
          </div>
        </div>
        <div className="h-48 flex items-end justify-between space-x-2">
          {mockData.daily.map((day, index) => (
            <div key={day.date} className="flex flex-col items-center flex-1">
              <div
                className="bg-primary rounded-t-md w-full transition-all duration-300 hover:bg-primary/80 cursor-pointer"
                style={{
                  height: `${(day.transcriptions / maxTranscriptions) * 160}px`,
                  minHeight: '4px'
                }}
                title={`${day.date}: ${day.transcriptions} transcriptions`}
              />
              <div className="mt-2 text-xs text-muted-foreground">
                {new Date(day.date).toLocaleDateString('en-US', { weekday: 'short' })}
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  };

  const renderAccuracyChart = () => {
    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h4 className="text-sm font-medium">Recognition Accuracy</h4>
          <div className="text-sm text-muted-foreground">
            Avg: {(mockData.daily.reduce((sum, day) => sum + day.accuracy, 0) / mockData.daily.length).toFixed(1)}%
          </div>
        </div>
        <div className="h-48 relative">
          <svg viewBox="0 0 400 160" className="w-full h-full">
            <defs>
              <linearGradient id="accuracyGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                <stop offset="0%" stopColor="rgb(34, 197, 94)" stopOpacity="0.3"/>
                <stop offset="100%" stopColor="rgb(34, 197, 94)" stopOpacity="0.1"/>
              </linearGradient>
            </defs>
            
            {/* Grid lines */}
            {[0, 25, 50, 75, 100].map((y) => (
              <line
                key={y}
                x1="0"
                y1={160 - (y * 1.6)}
                x2="400"
                y2={160 - (y * 1.6)}
                stroke="currentColor"
                strokeOpacity="0.1"
                strokeWidth="1"
              />
            ))}
            
            {/* Accuracy line */}
            <polyline
              fill="none"
              stroke="rgb(34, 197, 94)"
              strokeWidth="2"
              points={mockData.daily.map((day, index) => 
                `${(index * 400) / (mockData.daily.length - 1)},${160 - (day.accuracy - 90) * 8}`
              ).join(' ')}
            />
            
            {/* Area fill */}
            <polygon
              fill="url(#accuracyGradient)"
              points={[
                `0,160`,
                ...mockData.daily.map((day, index) => 
                  `${(index * 400) / (mockData.daily.length - 1)},${160 - (day.accuracy - 90) * 8}`
                ),
                `400,160`
              ].join(' ')}
            />
            
            {/* Data points */}
            {mockData.daily.map((day, index) => (
              <circle
                key={index}
                cx={(index * 400) / (mockData.daily.length - 1)}
                cy={160 - (day.accuracy - 90) * 8}
                r="3"
                fill="rgb(34, 197, 94)"
                className="cursor-pointer"
              >
                <title>{`${day.date}: ${day.accuracy}%`}</title>
              </circle>
            ))}
          </svg>
        </div>
      </div>
    );
  };

  const renderEnginesChart = () => {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <h4 className="text-sm font-medium">Engine Performance</h4>
          <div className="text-sm text-muted-foreground">
            Last 7 days
          </div>
        </div>
        
        <div className="space-y-4">
          {mockData.engines.map((engine, index) => {
            const colors = ['bg-primary', 'bg-success-500', 'bg-warning-500'];
            const bgColors = ['bg-primary/10', 'bg-success-50', 'bg-warning-50'];
            
            return (
              <div key={engine.name} className="space-y-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-3">
                    <div className={`w-3 h-3 rounded-full ${colors[index]}`} />
                    <span className="text-sm font-medium">{engine.name}</span>
                  </div>
                  <div className="flex items-center space-x-4 text-sm text-muted-foreground">
                    <span>{engine.usage}% usage</span>
                    <span>{engine.accuracy}% accuracy</span>
                  </div>
                </div>
                
                <div className="grid grid-cols-2 gap-4">
                  {/* Usage bar */}
                  <div>
                    <div className="h-2 bg-muted rounded-full overflow-hidden">
                      <div
                        className={`h-full ${colors[index]} transition-all duration-500`}
                        style={{ width: `${engine.usage}%` }}
                      />
                    </div>
                  </div>
                  
                  {/* Accuracy bar */}
                  <div>
                    <div className="h-2 bg-muted rounded-full overflow-hidden">
                      <div
                        className={`h-full ${colors[index]} transition-all duration-500`}
                        style={{ width: `${engine.accuracy}%` }}
                      />
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    );
  };

  return (
    <div className="card p-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-6">
        <h3 className="text-lg font-semibold mb-4 sm:mb-0">Analytics</h3>
        
        <div className="flex items-center space-x-2">
          {/* Time Range Selector */}
          <select
            value={timeRange}
            onChange={(e) => setTimeRange(e.target.value as any)}
            className="px-3 py-1.5 border border-input bg-background rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent"
          >
            <option value="7d">Last 7 days</option>
            <option value="30d">Last 30 days</option>
            <option value="90d">Last 90 days</option>
          </select>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="flex space-x-1 mb-6 bg-muted p-1 rounded-lg">
        {[
          { id: 'usage', label: 'Usage', icon: '📊' },
          { id: 'accuracy', label: 'Accuracy', icon: '🎯' },
          { id: 'engines', label: 'Engines', icon: '⚙️' },
        ].map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id as any)}
            className={`flex-1 flex items-center justify-center space-x-2 px-3 py-2 rounded-md text-sm font-medium transition-colors ${
              activeTab === tab.id
                ? 'bg-background text-foreground shadow-sm'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            <span>{tab.icon}</span>
            <span>{tab.label}</span>
          </button>
        ))}
      </div>

      {/* Chart Content */}
      <div className="min-h-[240px]">
        {activeTab === 'usage' && renderUsageChart()}
        {activeTab === 'accuracy' && renderAccuracyChart()}
        {activeTab === 'engines' && renderEnginesChart()}
      </div>
    </div>
  );
}