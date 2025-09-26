const development = {
  apiBaseUrl: 'http://localhost:5000',
  vapidPublicKey: 'BJ8tVLWDNfZ6yrPq5NiTyS9HdHGKEKvLjKYz8nSH6vJFMfQdmxL9LNGCCvUnXY6UlNDG9qOQ1P2ZyJ7Fj8RqvKE',
};

const production = {
  apiBaseUrl: 'https://your_production_api_domain.com',
  vapidPublicKey: 'YOUR_PRODUCTION_VAPID_PUBLIC_KEY',
};

const config = process.env.NODE_ENV === 'production' ? production : development;

export default config;
