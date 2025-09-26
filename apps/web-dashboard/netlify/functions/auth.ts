import { Handler } from '@netlify/functions';
import * as jwt from 'jsonwebtoken';

const JWT_SECRET = process.env.JWT_SECRET || 'your-secret-key';

interface AuthUser {
  id: string;
  email: string;
  name: string;
  plan: string;
}

export const handler: Handler = async (event, context) => {
  // Enable CORS
  if (event.httpMethod === 'OPTIONS') {
    return {
      statusCode: 200,
      headers: {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Headers': 'Content-Type, Authorization',
        'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
      },
      body: '',
    };
  }

  try {
    switch (event.httpMethod) {
      case 'POST': {
        // Login/Authentication endpoint
        if (!event.body) {
          throw new Error('Missing request body');
        }

        const { email, password } = JSON.parse(event.body);

        // TODO: Replace with actual authentication logic
        // This is a mock authentication - in production, verify against your user database
        if (email === 'demo@example.com' && password === 'password') {
          const user: AuthUser = {
            id: '123',
            email: 'demo@example.com',
            name: 'John Doe',
            plan: 'pro',
          };

          // Generate JWT token
          const token = jwt.sign(user, JWT_SECRET, { expiresIn: '24h' });

          return {
            statusCode: 200,
            headers: {
              'Content-Type': 'application/json',
              'Access-Control-Allow-Origin': '*',
            },
            body: JSON.stringify({ token, user }),
          };
        }

        return {
          statusCode: 401,
          headers: {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*',
          },
          body: JSON.stringify({ error: 'Invalid credentials' }),
        };
      }

      case 'GET': {
        // Token verification endpoint
        const authHeader = event.headers.authorization;
        if (!authHeader || !authHeader.startsWith('Bearer ')) {
          throw new Error('Missing or invalid authorization header');
        }

        const token = authHeader.split(' ')[1];
        const decoded = jwt.verify(token, JWT_SECRET) as AuthUser;

        return {
          statusCode: 200,
          headers: {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*',
          },
          body: JSON.stringify({ user: decoded }),
        };
      }

      default:
        return {
          statusCode: 405,
          headers: {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*',
          },
          body: JSON.stringify({ error: 'Method not allowed' }),
        };
    }
  } catch (error) {
    console.error('Auth error:', error);
    return {
      statusCode: error.name === 'JsonWebTokenError' ? 401 : 500,
      headers: {
        'Content-Type': 'application/json',
        'Access-Control-Allow-Origin': '*',
      },
      body: JSON.stringify({ error: error.message }),
    };
  }
};