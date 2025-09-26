'use client';

import React, { useState } from 'react';

interface Activity {
  id: string;
  type: 'transcription' | 'error' | 'engine_switch' | 'config_change';
  title: string;
  details: string;
  timestamp: string;
  engine?: string;
  confidence?: number;
  status?: 'success' | 'warning' | 'error';
}

const mockActivities: Activity[] = [
  {
    id: '1',
    type: 'transcription',
    title: 'Transcription completed',
    details: 'Successfully transcribed 2 minute audio clip.',
    timestamp: '2024-01-21T14:32:00Z',
    engine: 'Whisper Local',
    confidence: 96.5,
    status: 'success',
  },
  {
    id: '2',
    type: 'engine_switch',
    title: 'Engine switched',
    details: 'Switched from Azure Speech to OpenAI Whisper for better accuracy.',
    timestamp: '2024-01-21T14:15:00Z',
    engine: 'OpenAI Whisper',
    status: 'warning',
  },
  {
    id: '3',
    type: 'error',
    title: 'Recognition failed',
    details: 'Network connection error. Retrying with local engine.',
    timestamp: '2024-01-21T13:45:00Z',
    engine: 'Azure Speech',
    status: 'error',
  },
  {
    id: '4',
    type: 'config_change',
    title: 'Settings updated',
    details: 'Changed language model to enhance technical term recognition.',
    timestamp: '2024-01-21T13:30:00Z',
    status: 'success',
  },
  {
    id: '5',
    type: 'transcription',
    title: 'Transcription completed',
    details: 'Transcribed 5 minute meeting recording with speaker diarization.',
    timestamp: '2024-01-21T13:15:00Z',
    engine: 'OpenAI Whisper',
    confidence: 94.8,
    status: 'success',
  },
];

interface RecentActivityProps {}

export function RecentActivity({}: RecentActivityProps) {
  const [filter, setFilter] = useState<'all' | 'transcription' | 'error'>('all');

  const getActivityIcon = (type: Activity['type']) => {
    switch (type) {
      case 'transcription':
        return (
          <div className="p-2 bg-primary/10 rounded-lg">
            <svg className="w-4 h-4 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 11a7 7 0 01-7 7m0 0a7 7 0 01-7-7m7 7v4m0 0H8m4 0h4m-4-8a3 3 0 01-3-3V5a3 3 0 016 0v6a3 3 0 01-3 3z" />
            </svg>
          </div>
        );
      case 'error':
        return (
          <div className="p-2 bg-error-50 rounded-lg">
            <svg className="w-4 h-4 text-error-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
        );
      case 'engine_switch':
        return (
          <div className="p-2 bg-warning-50 rounded-lg">
            <svg className="w-4 h-4 text-warning-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
            </svg>
          </div>
        );
      case 'config_change':
        return (
          <div className="p-2 bg-success-50 rounded-lg">
            <svg className="w-4 h-4 text-success-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
          </div>
        );
    }
  };

  const formatTime = (timestamp: string) => {
    const date = new Date(timestamp);
    return new Intl.DateTimeFormat('en-US', {
      hour: 'numeric',
      minute: 'numeric',
      hour12: true
    }).format(date);
  };

  const filteredActivities = mockActivities.filter(activity => {
    if (filter === 'all') return true;
    return activity.type === filter;
  });

  return (
    <div className="card p-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-6">
        <h3 className="text-lg font-semibold mb-4 sm:mb-0">Recent Activity</h3>
        
        <div className="flex items-center space-x-2">
          <select
            value={filter}
            onChange={(e) => setFilter(e.target.value as any)}
            className="px-3 py-1.5 border border-input bg-background rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent"
          >
            <option value="all">All Activities</option>
            <option value="transcription">Transcriptions</option>
            <option value="error">Errors</option>
          </select>
        </div>
      </div>

      {/* Activity List */}
      <div className="space-y-4">
        {filteredActivities.map((activity) => (
          <div key={activity.id} className="flex items-start space-x-4 p-3 rounded-lg hover:bg-muted/50 transition-colors">
            {getActivityIcon(activity.type)}
            
            <div className="flex-1 min-w-0">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-sm font-medium">{activity.title}</p>
                  <p className="text-xs text-muted-foreground mt-0.5">{activity.details}</p>
                </div>
                <span className="text-xs text-muted-foreground whitespace-nowrap ml-4">
                  {formatTime(activity.timestamp)}
                </span>
              </div>

              {/* Additional Info */}
              {(activity.engine || activity.confidence) && (
                <div className="flex items-center space-x-3 mt-2">
                  {activity.engine && (
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-accent text-accent-foreground">
                      {activity.engine}
                    </span>
                  )}
                  {activity.confidence && (
                    <span className="inline-flex items-center text-xs text-muted-foreground">
                      {activity.confidence}% confidence
                    </span>
                  )}
                </div>
              )}
            </div>

            {/* Status Indicator */}
            {activity.status && (
              <div className={`
                w-2 h-2 rounded-full mt-1.5
                ${activity.status === 'success' ? 'bg-success-500' : ''}
                ${activity.status === 'warning' ? 'bg-warning-500' : ''}
                ${activity.status === 'error' ? 'bg-error-500' : ''}
              `} />
            )}
          </div>
        ))}
      </div>

      {/* View All Link */}
      <div className="mt-6 pt-4 border-t">
        <button className="btn btn-ghost px-4 py-2 text-sm w-full">
          View All Activity
        </button>
      </div>
    </div>
  );
}