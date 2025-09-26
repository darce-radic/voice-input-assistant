const path = require('path');
const webpack = require('webpack');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const CssMinimizerPlugin = require('css-minimizer-webpack-plugin');
const TerserPlugin = require('terser-webpack-plugin');
const { BundleAnalyzerPlugin } = require('webpack-bundle-analyzer');
const CompressionPlugin = require('compression-webpack-plugin');
const { InjectManifest } = require('workbox-webpack-plugin');
const CopyPlugin = require('copy-webpack-plugin');

const isProduction = process.env.NODE_ENV === 'production';
const isAnalyze = process.env.ANALYZE === 'true';

module.exports = {
  mode: isProduction ? 'production' : 'development',
  
  entry: {
    main: './src/index.tsx',
    // Separate entry for service worker utilities
    'sw-utils': './src/utils/pwaUtils.ts'
  },

  output: {
    path: path.resolve(__dirname, 'build'),
    filename: isProduction 
      ? 'static/js/[name].[contenthash:8].js'
      : 'static/js/[name].js',
    chunkFilename: isProduction
      ? 'static/js/[name].[contenthash:8].chunk.js'
      : 'static/js/[name].chunk.js',
    publicPath: '/',
    clean: true
  },

  resolve: {
    extensions: ['.tsx', '.ts', '.jsx', '.js'],
    alias: {
      '@': path.resolve(__dirname, 'src'),
      '@components': path.resolve(__dirname, 'src/components'),
      '@utils': path.resolve(__dirname, 'src/utils'),
      '@hooks': path.resolve(__dirname, 'src/hooks'),
      '@services': path.resolve(__dirname, 'src/services'),
    }
  },

  module: {
    rules: [
      {
        test: /\.(ts|tsx)$/,
        use: [
          {
            loader: 'ts-loader',
            options: {
              transpileOnly: true,
              configFile: path.resolve(__dirname, 'tsconfig.json')
            }
          }
        ],
        exclude: /node_modules/
      },
      {
        test: /\.css$/,
        use: [
          isProduction ? MiniCssExtractPlugin.loader : 'style-loader',
          {
            loader: 'css-loader',
            options: {
              importLoaders: 1,
              modules: {
                auto: true,
                localIdentName: isProduction 
                  ? '[hash:base64:5]' 
                  : '[name]__[local]__[hash:base64:5]'
              }
            }
          },
          'postcss-loader'
        ]
      },
      {
        test: /\.(png|jpe?g|gif|svg|webp|ico)$/,
        type: 'asset',
        parser: {
          dataUrlCondition: {
            maxSize: 8192 // 8kb
          }
        },
        generator: {
          filename: 'static/media/[name].[contenthash:8][ext]'
        }
      },
      {
        test: /\.(woff|woff2|eot|ttf|otf)$/,
        type: 'asset/resource',
        generator: {
          filename: 'static/fonts/[name].[contenthash:8][ext]'
        }
      },
      {
        test: /\.(mp3|wav|ogg|m4a)$/,
        type: 'asset/resource',
        generator: {
          filename: 'static/audio/[name].[contenthash:8][ext]'
        }
      }
    ]
  },

  optimization: {
    minimize: isProduction,
    minimizer: [
      new TerserPlugin({
        terserOptions: {
          compress: {
            drop_console: isProduction,
            drop_debugger: isProduction,
            pure_funcs: isProduction ? ['console.log', 'console.info'] : []
          },
          mangle: true,
          format: {
            comments: false
          }
        },
        extractComments: false
      }),
      new CssMinimizerPlugin()
    ],
    
    splitChunks: {
      chunks: 'all',
      cacheGroups: {
        // Vendor chunks
        vendor: {
          test: /[\\/]node_modules[\\/]/,
          name: 'vendors',
          chunks: 'all',
          priority: 10
        },
        
        // React and related libraries
        react: {
          test: /[\\/]node_modules[\\/](react|react-dom|react-router)[\\/]/,
          name: 'react',
          chunks: 'all',
          priority: 20
        },
        
        // UI libraries and utilities
        ui: {
          test: /[\\/]node_modules[\\/](styled-components|emotion|@mui|antd)[\\/]/,
          name: 'ui',
          chunks: 'all',
          priority: 15
        },
        
        // Analytics and charting libraries
        analytics: {
          test: /[\\/]node_modules[\\/](chart\.js|recharts|d3|plotly\.js)[\\/]/,
          name: 'analytics',
          chunks: 'all',
          priority: 15
        },
        
        // Common utilities
        utils: {
          test: /[\\/]src[\\/](utils|hooks|services)[\\/]/,
          name: 'utils',
          chunks: 'all',
          minChunks: 2,
          priority: 5
        },
        
        // Default chunk
        default: {
          minChunks: 2,
          priority: 1,
          reuseExistingChunk: true
        }
      }
    },

    // Runtime chunk for better caching
    runtimeChunk: {
      name: entrypoint => `runtime-${entrypoint.name}`
    },

    // Module concatenation for better tree shaking
    concatenateModules: true,
    
    // Better module IDs for caching
    moduleIds: isProduction ? 'deterministic' : 'named',
    chunkIds: isProduction ? 'deterministic' : 'named'
  },

  plugins: [
    new HtmlWebpackPlugin({
      template: './public/index.html',
      minify: isProduction ? {
        removeComments: true,
        collapseWhitespace: true,
        removeRedundantAttributes: true,
        useShortDoctype: true,
        removeEmptyAttributes: true,
        removeStyleLinkTypeAttributes: true,
        keepClosingSlash: true,
        minifyJS: true,
        minifyCSS: true,
        minifyURLs: true
      } : false
    }),

    // Extract CSS in production
    ...(isProduction ? [
      new MiniCssExtractPlugin({
        filename: 'static/css/[name].[contenthash:8].css',
        chunkFilename: 'static/css/[name].[contenthash:8].chunk.css'
      })
    ] : []),

    // Copy static assets
    new CopyPlugin({
      patterns: [
        {
          from: 'public',
          to: '',
          globOptions: {
            ignore: ['**/index.html']
          }
        }
      ]
    }),

    // Service Worker with Workbox
    ...(isProduction ? [
      new InjectManifest({
        swSrc: './public/sw-enhanced.js',
        swDest: 'sw-enhanced.js',
        exclude: [/\.map$/, /manifest$/, /\.htaccess$/],
        maximumFileSizeToCacheInBytes: 5 * 1024 * 1024, // 5MB
        mode: 'production'
      })
    ] : []),

    // Compression in production
    ...(isProduction ? [
      new CompressionPlugin({
        algorithm: 'gzip',
        test: /\.(js|css|html|svg)$/,
        threshold: 8192,
        minRatio: 0.8
      })
    ] : []),

    // Environment variables
    new webpack.DefinePlugin({
      'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV || 'development'),
      'process.env.REACT_APP_VERSION': JSON.stringify(process.env.npm_package_version || '1.0.0'),
      'process.env.REACT_APP_BUILD_TIME': JSON.stringify(new Date().toISOString())
    }),

    // Hot Module Replacement in development
    ...(isProduction ? [] : [
      new webpack.HotModuleReplacementPlugin()
    ]),

    // Bundle analyzer
    ...(isAnalyze ? [
      new BundleAnalyzerPlugin({
        analyzerMode: 'server',
        openAnalyzer: true
      })
    ] : []),

    // Progress plugin for better build feedback
    new webpack.ProgressPlugin({
      activeModules: true
    }),

    // Ignore moment.js locales to reduce bundle size
    new webpack.IgnorePlugin({
      resourceRegExp: /^\.\/locale$/,
      contextRegExp: /moment$/
    })
  ],

  devServer: {
    contentBase: path.join(__dirname, 'public'),
    port: 3000,
    hot: true,
    open: true,
    historyApiFallback: true,
    compress: true,
    overlay: {
      warnings: false,
      errors: true
    },
    headers: {
      'Service-Worker-Allowed': '/'
    },
    https: false, // Set to true for PWA testing with HTTPS
    host: '0.0.0.0', // Allow external connections for mobile testing
    
    // Mock service worker in development
    setupMiddlewares: (middlewares, devServer) => {
      devServer.app.get('/sw-enhanced.js', (req, res) => {
        res.setHeader('Content-Type', 'application/javascript');
        res.setHeader('Service-Worker-Allowed', '/');
        res.sendFile(path.resolve(__dirname, 'public/sw-enhanced.js'));
      });
      
      return middlewares;
    }
  },

  // Source maps
  devtool: isProduction 
    ? 'source-map' 
    : 'eval-cheap-module-source-map',

  // Performance hints
  performance: {
    hints: isProduction ? 'warning' : false,
    maxEntrypointSize: 512000, // 500kb
    maxAssetSize: 512000,
    assetFilter: (assetFilename) => {
      return !assetFilename.endsWith('.map');
    }
  },

  // Stats configuration
  stats: {
    colors: true,
    modules: false,
    chunks: false,
    chunkModules: false,
    entrypoints: false,
    assets: isProduction,
    version: false,
    timings: true,
    builtAt: true
  },

  // Resolve symlinks for better compatibility
  resolve: {
    ...module.exports.resolve,
    symlinks: false
  },

  // External dependencies (for CDN usage if needed)
  externals: isProduction ? {
    // Uncomment if you want to use CDN for these libraries
    // 'react': 'React',
    // 'react-dom': 'ReactDOM'
  } : {},

  // Experiments for future webpack features
  experiments: {
    // Enable async WebAssembly
    asyncWebAssembly: true,
    
    // Enable top-level await
    topLevelAwait: true
  }
};

// Development-specific optimizations
if (!isProduction) {
  // Faster builds in development
  module.exports.optimization.splitChunks = {
    chunks: 'async',
    cacheGroups: {
      default: false,
      vendors: false
    }
  };

  // Disable runtime chunk in development
  module.exports.optimization.runtimeChunk = false;
}

// Production-specific optimizations
if (isProduction) {
  // Additional production optimizations
  module.exports.optimization.usedExports = true;
  module.exports.optimization.sideEffects = false;
  
  // Module concatenation
  module.exports.optimization.concatenateModules = true;
}

module.exports.module.rules.push(
  // Preload critical resources
  {
    test: /\.(woff2)$/,
    use: [
      {
        loader: 'preload-webpack-plugin',
        options: {
          rel: 'preload',
          as: 'font',
          crossorigin: true
        }
      }
    ]
  }
);