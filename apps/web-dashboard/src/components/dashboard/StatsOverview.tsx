'use client';

import React, { useState, useEffect } from 'react';
import { api } from '../../services/api';

interface StatsOverviewProps {
  stats?: {
    totalTranscriptions: number;
    accuracyRate: number;
    totalHours: number;
    activeEngines: number;
  };
}

const statCards = [
  {
    key: 'totalTranscriptions',
    title: 'Total Transcriptions',
    icon: (
      <svg className="w-6 h-6 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
      </svg>
    ),
    formatter: (value: number) => value.toLocaleString(),
  },
  {
    key: 'accuracyRate',
    title: 'Accuracy Rate',
    icon: (
      <svg className="w-6 h-6 text-success-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ),
    formatter: (value: number) => `${value}%`,
  },
  {
    key: 'totalHours',
    title: 'Total Hours',
    icon: (
      <svg className="w-6 h-6 text-warning-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ),
    formatter: (value: number) => `${value}h`,
  },
  {
    key: 'activeEngines',
    title: 'Active Engines',
    icon: (
      <svg className="w-6 h-6 text-error-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
      </svg>
    ),
    formatter: (value: number) => value.toString(),
  },
];

export function StatsOverview({ stats: propStats }: StatsOverviewProps) {
  const [stats, setStats] = useState(propStats || {
    totalTranscriptions: 0,
    accuracyRate: 0,
    totalHours: 0,
    activeEngines: 0,
  });

  useEffect(() => {
    // Fetch analytics data from API
    api.getAnalytics().then((data: any) => {
      setStats({
        totalTranscriptions: data.totalTranscriptions,
        accuracyRate: data.accuracyRate,
        totalHours: data.totalHours,
        activeEngines: data.activeEngines,
      });
    }).catch(console.error);
  }, []);

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
      {statCards.map((card) => {
        const value = stats[card.key as keyof typeof stats];
        return (
          <div key={card.key} className="card p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground mb-1">
                  {card.title}
                </p>
                <p className="text-3xl font-bold text-foreground">
                  {card.formatter(value)}
                </p>
                <div className="flex items-center mt-2">
                  <span className="text-xs text-success-600 font-medium">
                    ↑ 12.5%
                  </span>
                  <span className="text-xs text-muted-foreground ml-1">
                    from last month
                  </span>
                </div>
              </div>
              <div className="p-3 bg-accent/50 rounded-lg">
                {card.icon}
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}