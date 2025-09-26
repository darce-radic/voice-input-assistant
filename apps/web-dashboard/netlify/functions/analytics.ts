import { Handler } from '@netlify/functions';

interface AnalyticsData {
  totalTranscriptions: number;
  accuracyRate: number;
  totalHours: number;
  activeEngines: number;
  daily: Array<{
    date: string;
    transcriptions: number;
    accuracy: number;
  }>;
  engines: Array<{
    name: string;
    usage: number;
    accuracy: number;
  }>;
}

const generateMockData = (): AnalyticsData => {
  const daily = [];
  const now = new Date();
  
  for (let i = 6; i >= 0; i--) {
    const date = new Date(now);
    date.setDate(date.getDate() - i);
    daily.push({
      date: date.toISOString().split('T')[0],
      transcriptions: Math.floor(Math.random() * 50) + 40,
      accuracy: Math.random() * 5 + 93,
    });
  }
  
  return {
    totalTranscriptions: 1247,
    accuracyRate: 95.3,
    totalHours: 42.5,
    activeEngines: 3,
    daily,
    engines: [
      { name: 'Whisper Local', usage: 45, accuracy: 96.2 },
      { name: 'Azure Speech', usage: 30, accuracy: 94.8 },
      { name: 'OpenAI Whisper', usage: 25, accuracy: 97.1 },
    ],
  };
};

export const handler: Handler = async (event, context) => {
  // Enable CORS
  if (event.httpMethod === 'OPTIONS') {
    return {
      statusCode: 200,
      headers: {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Headers': 'Content-Type, Authorization',
        'Access-Control-Allow-Methods': 'GET, OPTIONS',
      },
      body: '',
    };
  }

  if (event.httpMethod !== 'GET') {
    return {
      statusCode: 405,
      headers: {
        'Content-Type': 'application/json',
        'Access-Control-Allow-Origin': '*',
      },
      body: JSON.stringify({ error: 'Method not allowed' }),
    };
  }

  try {
    // In production, fetch real data from your database
    const analyticsData = generateMockData();

    return {
      statusCode: 200,
      headers: {
        'Content-Type': 'application/json',
        'Access-Control-Allow-Origin': '*',
      },
      body: JSON.stringify(analyticsData),
    };
  } catch (error) {
    console.error('Analytics error:', error);
    return {
      statusCode: 500,
      headers: {
        'Content-Type': 'application/json',
        'Access-Control-Allow-Origin': '*',
      },
      body: JSON.stringify({ error: error.message }),
    };
  }
};