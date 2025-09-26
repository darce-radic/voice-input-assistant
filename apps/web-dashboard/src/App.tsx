import React, { useState } from 'react';
import { AnalyticsChart } from './components/dashboard/AnalyticsChart';
import { RecentActivity } from './components/dashboard/RecentActivity';
import { StatsOverview } from './components/dashboard/StatsOverview';
import { VoiceRecognitionWidget } from './components/dashboard/VoiceRecognitionWidget';
import { DashboardLayout } from './components/layout/DashboardLayout';

function App() {
  const [currentView, setCurrentView] = useState<'home' | 'dashboard' | 'settings'>('dashboard');

  if (currentView === 'dashboard') {
    return (
      <DashboardLayout>
        <div className="p-6 space-y-6">
          <h1 className="text-3xl font-bold">Voice Input Dashboard</h1>
          
          {/* Stats Overview - Key metrics */}
          <StatsOverview />
          
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Voice Recognition Widget - Live transcription */}
            <VoiceRecognitionWidget />
            
            {/* Analytics Chart - Usage and accuracy data */}
            <AnalyticsChart />
          </div>
          
          {/* Recent Activity - History and logs */}
          <RecentActivity />
        </div>
      </DashboardLayout>
    );
  }

  // Home/Landing view
  return (
    <div className="flex min-h-screen flex-col items-center justify-center py-2">
      <main className="flex w-full flex-1 flex-col items-center justify-center px-20 text-center">
        <h1 className="text-6xl font-bold">
          Welcome to{' '}
          <span className="text-blue-600">
            Voice Input Assistant
          </span>
        </h1>

        <p className="mt-3 text-2xl">
          Your AI-powered voice recognition dashboard
        </p>

        <div className="mt-6 flex max-w-4xl flex-wrap items-center justify-around sm:w-full">
          <button
            onClick={() => setCurrentView('dashboard')}
            className="mt-6 w-96 rounded-xl border p-6 text-left hover:text-blue-600 focus:text-blue-600 cursor-pointer"
          >
            <h3 className="text-2xl font-bold">Dashboard &rarr;</h3>
            <p className="mt-4 text-xl">
              View your voice input history and analytics
            </p>
          </button>

          <button
            onClick={() => setCurrentView('settings')}
            className="mt-6 w-96 rounded-xl border p-6 text-left hover:text-blue-600 focus:text-blue-600 cursor-pointer"
          >
            <h3 className="text-2xl font-bold">Settings &rarr;</h3>
            <p className="mt-4 text-xl">
              Configure your voice recognition preferences
            </p>
          </button>
        </div>
      </main>
    </div>
  );
}

export default App;