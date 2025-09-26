/** @type {import('next').NextConfig} */
const withPWA = require("next-pwa")({
  dest: "public",
  disable: process.env.NODE_ENV === "development",
  register: true,
  skipWaiting: true,
});

const nextConfig = {
  output: "export",
  images: {
    unoptimized: true,
  },
  // PWA configuration
  experimental: {
    webpackBuildWorker: true,
  },
};

module.exports = withPWA(nextConfig);
