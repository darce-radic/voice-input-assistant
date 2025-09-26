const { GenerateSW } = require('workbox-webpack-plugin');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const CompressionPlugin = require('compression-webpack-plugin');
const { BundleAnalyzerPlugin } = require('webpack-bundle-analyzer');
const path = require('path');

module.exports = {
  webpack: {
    configure: (webpackConfig, { env, paths }) => {
      // Add workbox service worker generation
      if (env === 'production') {
        webpackConfig.plugins.push(
          new GenerateSW({
            clientsClaim: true,
            skipWaiting: true,
            navigateFallback: '/index.html',
            navigateFallbackDenylist: [/^\/_/, /\/[^/?]+\.[^/]+$/],
            maximumFileSizeToCacheInBytes: 5 * 1024 * 1024, // 5MB
            runtimeCaching: [
              {
                urlPattern: /^https:\/\/fonts\.googleapis\.com/,
                handler: 'StaleWhileRevalidate',
                options: {
                  cacheName: 'google-fonts-stylesheets',
                  expiration: {
                    maxAgeSeconds: 60 * 60 * 24 * 365, // 1 year
                    maxEntries: 30,
                  },
                },
              },
              {
                urlPattern: /^https:\/\/fonts\.gstatic\.com/,
                handler: 'CacheFirst',
                options: {
                  cacheName: 'google-fonts-webfonts',
                  expiration: {
                    maxAgeSeconds: 60 * 60 * 24 * 365, // 1 year
                    maxEntries: 30,
                  },
                },
              },
              {
                urlPattern: /\.(?:png|jpg|jpeg|svg|gif|webp)$/,
                handler: 'CacheFirst',
                options: {
                  cacheName: 'images',
                  expiration: {
                    maxAgeSeconds: 60 * 60 * 24 * 30, // 30 days
                    maxEntries: 100,
                  },
                },
              },
              {
                urlPattern: /\.(?:mp3|wav|ogg|m4a|webm)$/,
                handler: 'CacheFirst',
                options: {
                  cacheName: 'audio-files',
                  expiration: {
                    maxAgeSeconds: 60 * 60 * 24 * 7, // 7 days
                    maxEntries: 50,
                  },
                },
              },
              {
                urlPattern: /^https:\/\/speech\.googleapis\.com/,
                handler: 'NetworkFirst',
                options: {
                  cacheName: 'speech-api',
                  networkTimeoutSeconds: 5,
                  expiration: {
                    maxAgeSeconds: 60 * 5, // 5 minutes
                    maxEntries: 20,
                  },
                },
              },
              {
                urlPattern: /^https:\/\/.*\.cognitive\.microsofttranslator\.com/,
                handler: 'NetworkFirst',
                options: {
                  cacheName: 'azure-api',
                  networkTimeoutSeconds: 5,
                  expiration: {
                    maxAgeSeconds: 60 * 5, // 5 minutes
                    maxEntries: 20,
                  },
                },
              },
              {
                urlPattern: /^https:\/\/transcribe.*\.amazonaws\.com/,
                handler: 'NetworkFirst',
                options: {
                  cacheName: 'aws-api',
                  networkTimeoutSeconds: 5,
                  expiration: {
                    maxAgeSeconds: 60 * 5, // 5 minutes
                    maxEntries: 20,
                  },
                },
              },
              {
                urlPattern: /^https:\/\/api\.openai\.com/,
                handler: 'NetworkFirst',
                options: {
                  cacheName: 'openai-api',
                  networkTimeoutSeconds: 10,
                  expiration: {
                    maxAgeSeconds: 60 * 5, // 5 minutes
                    maxEntries: 20,
                  },
                },
              },
            ],
            // Include our custom service worker
            importScripts: ['/workbox-sw.js'],
          })
        );

        // Copy additional files
        webpackConfig.plugins.push(
          new CopyWebpackPlugin({
            patterns: [
              {
                from: 'public/workbox-sw.js',
                to: 'workbox-sw.js',
              },
              {
                from: 'public/icons',
                to: 'icons',
              },
              {
                from: 'public/screenshots',
                to: 'screenshots',
                noErrorOnMissing: true,
              },
            ],
          })
        );

        // Enable gzip compression
        webpackConfig.plugins.push(
          new CompressionPlugin({
            filename: '[path][base].gz',
            algorithm: 'gzip',
            test: /\.(js|css|html|svg)$/,
            threshold: 8192,
            minRatio: 0.8,
          })
        );

        // Bundle analyzer in analyze mode
        if (process.env.ANALYZE) {
          webpackConfig.plugins.push(
            new BundleAnalyzerPlugin({
              analyzerMode: 'server',
              openAnalyzer: true,
            })
          );
        }
      }

      // Code splitting optimization
      webpackConfig.optimization = {
        ...webpackConfig.optimization,
        splitChunks: {
          chunks: 'all',
          cacheGroups: {
            vendor: {
              test: /[\\/]node_modules[\\/]/,
              name: 'vendors',
              priority: 10,
              chunks: 'all',
            },
            mui: {
              test: /[\\/]node_modules[\\/]@mui[\\/]/,
              name: 'mui',
              priority: 20,
              chunks: 'all',
            },
            tensorflow: {
              test: /[\\/]node_modules[\\/]@tensorflow[\\/]/,
              name: 'tensorflow',
              priority: 20,
              chunks: 'all',
            },
            common: {
              minChunks: 2,
              priority: 5,
              reuseExistingChunk: true,
            },
          },
        },
      };

      // Performance hints
      webpackConfig.performance = {
        maxAssetSize: 512000,
        maxEntrypointSize: 512000,
        hints: env === 'production' ? 'warning' : false,
      };

      return webpackConfig;
    },
  },
  devServer: {
    // Enable HTTPS for PWA testing
    https: process.env.HTTPS === 'true',
    host: 'localhost',
    port: 3000,
    headers: {
      'Service-Worker-Allowed': '/',
    },
  },
  plugins: [
    {
      plugin: require('craco-workbox'),
    },
  ],
  eslint: {
    enable: true,
    mode: 'extends',
    configure: {
      extends: [
        'react-app',
        'react-app/jest',
        '@typescript-eslint/recommended',
      ],
      rules: {
        '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_' }],
        '@typescript-eslint/explicit-function-return-type': 'off',
        '@typescript-eslint/explicit-module-boundary-types': 'off',
        'no-console': ['warn', { allow: ['warn', 'error'] }],
      },
    },
  },
  typescript: {
    enableTypeChecking: true,
  },
};