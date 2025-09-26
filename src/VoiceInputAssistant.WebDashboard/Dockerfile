# Multi-stage Dockerfile for Voice Input Assistant
# Stage 1: Build the React application
FROM node:18-alpine AS builder

# Set working directory
WORKDIR /app

# Add labels for better organization
LABEL maintainer="Voice Input Assistant Team"
LABEL description="Voice Input Assistant - Progressive Web App"
LABEL version="1.0.0"

# Install dependencies for node-gyp and native modules
RUN apk add --no-cache python3 make g++ git

# Copy package files
COPY package*.json ./
COPY yarn.lock* ./

# Install dependencies
# Use npm ci for faster, reliable, reproducible builds
RUN npm ci --only=production && npm cache clean --force

# Copy source code
COPY . .

# Set build environment variables
ENV NODE_ENV=production
ENV REACT_APP_ENV=production
ENV GENERATE_SOURCEMAP=false

# Build the application
RUN npm run build

# Stage 2: Serve the application with Nginx
FROM nginx:1.25-alpine AS production

# Install additional packages for security and monitoring
RUN apk add --no-cache curl

# Remove default nginx website
RUN rm -rf /usr/share/nginx/html/*

# Copy built application from builder stage
COPY --from=builder /app/build /usr/share/nginx/html

# Copy custom nginx configuration
COPY docker/nginx.conf /etc/nginx/nginx.conf
COPY docker/default.conf /etc/nginx/conf.d/default.conf

# Copy SSL certificates (optional)
# COPY docker/ssl/ /etc/nginx/ssl/

# Create nginx user and set permissions
RUN addgroup -g 1001 -S nginx && \
    adduser -S -D -H -u 1001 -h /var/cache/nginx -s /sbin/nologin -G nginx -g nginx nginx

# Set proper permissions
RUN chown -R nginx:nginx /usr/share/nginx/html && \
    chown -R nginx:nginx /var/cache/nginx && \
    chown -R nginx:nginx /var/log/nginx && \
    chown -R nginx:nginx /etc/nginx/conf.d

# Create directory for nginx pid
RUN mkdir -p /var/run/nginx && \
    chown -R nginx:nginx /var/run/nginx

# Expose ports
EXPOSE 80 443

# Add health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

# Use nginx user
USER nginx

# Start nginx
CMD ["nginx", "-g", "daemon off;"]

# Development stage
FROM node:18-alpine AS development

WORKDIR /app

# Install development dependencies
RUN apk add --no-cache python3 make g++ git

# Copy package files
COPY package*.json ./
COPY yarn.lock* ./

# Install all dependencies including devDependencies
RUN npm install

# Copy source code
COPY . .

# Set development environment variables
ENV NODE_ENV=development
ENV REACT_APP_ENV=development
ENV GENERATE_SOURCEMAP=true

# Expose development port
EXPOSE 3000 3443

# Create non-root user for security
RUN addgroup -g 1001 -S appuser && \
    adduser -S -D -H -u 1001 -h /app -s /bin/sh -G appuser -g appuser appuser

# Set ownership
RUN chown -R appuser:appuser /app

USER appuser

# Start development server
CMD ["npm", "start"]

# Testing stage
FROM builder AS testing

# Install test dependencies
RUN npm install

# Copy test files
COPY src/ ./src/
COPY public/ ./public/

# Set test environment
ENV NODE_ENV=test
ENV CI=true

# Run tests
RUN npm test -- --coverage --watchAll=false

# Build and test production build
RUN npm run build && npm run pwa-test &

# Keep container running for test inspection
CMD ["tail", "-f", "/dev/null"]