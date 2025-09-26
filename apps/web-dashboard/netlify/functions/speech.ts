import { Handler } from '@netlify/functions';

interface TranscriptionResult {
  text: string;
  confidence: number;
  engine: string;
}

const mockTranscribe = async (audioBlob: Buffer, engine: string): Promise<TranscriptionResult> => {
  // Simulate processing delay
  await new Promise(resolve => setTimeout(resolve, 1500));
  
  // In production, replace this with actual transcription logic using your chosen engine
  const responses = [
    'This is a transcribed text from the audio file using AI-powered recognition.',
    'The voice recognition system has successfully processed your audio input.',
    'Your speech has been converted to text with high accuracy.',
  ];
  
  return {
    text: responses[Math.floor(Math.random() * responses.length)],
    confidence: Math.random() * 15 + 85, // 85-100% confidence
    engine,
  };
};

export const handler: Handler = async (event, context) => {
  if (event.httpMethod === 'OPTIONS') {
    return {
      statusCode: 200,
      headers: {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Headers': 'Content-Type',
        'Access-Control-Allow-Methods': 'POST, OPTIONS',
      },
      body: '',
    };
  }

  if (event.httpMethod !== 'POST') {
    return {
      statusCode: 405,
      body: JSON.stringify({ error: 'Method not allowed' }),
    };
  }

  try {
    if (!event.body) {
      throw new Error('Missing request body');
    }

    const { audio, engine = 'whisper-local' } = JSON.parse(event.body);
    
    if (!audio) {
      throw new Error('Missing audio data');
    }

    // Convert base64 audio to buffer
    const audioBuffer = Buffer.from(audio.split(',')[1], 'base64');
    
    // Process the audio
    const result = await mockTranscribe(audioBuffer, engine);

    return {
      statusCode: 200,
      headers: {
        'Content-Type': 'application/json',
        'Access-Control-Allow-Origin': '*',
      },
      body: JSON.stringify(result),
    };

  } catch (error) {
    console.error('Speech recognition error:', error);
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