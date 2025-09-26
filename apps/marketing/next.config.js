/** @type {import('next').NextConfig} */
const nextConfig = {
  // Use static export for Netlify
  output: 'export',

  // Ensure images from your domain are optimized
  images: {
    domains: ['jocular-youtiao-7bff13.netlify.app'],
    unoptimized: true, // Required for static export
  },

  // Enable React strict mode for better development practices
  reactStrictMode: true,

  // Disable server-side image optimization as it's not supported in static exports
  images: {
    unoptimized: true,
  },

  // Static export configuration
  trailingSlash: true,

  // Environment variables that will be available at build time
  env: {
    NEXT_PUBLIC_SITE_URL: process.env.NEXT_PUBLIC_SITE_URL || 'https://jocular-youtiao-7bff13.netlify.app',
  },

  // Configure headers for proper MIME types
  async headers() {
    return [
      {
        source: '/_next/static/:path*',
        headers: [
          {
            key: 'Content-Type',
            value: 'application/javascript',
          },
        ],
      },
    ];
  },

  // Ensure proper static file serving
  poweredByHeader: false,
  compress: true,

  // Experimental features
  experimental: {
    // Add any experimental features here if needed
  },
}

module.exports = nextConfig