# Build stage
FROM node:20-alpine AS builder

WORKDIR /app

# Copy package files
COPY package*.json ./
RUN npm ci

# Copy source
COPY . .

# Build
RUN npm run build

# Test stage (dev deps + source for running jest / ts-node)
FROM node:20-alpine AS test

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

EXPOSE 3000 3001

CMD ["npm", "run", "dev"]

# Production stage
FROM node:20-alpine AS production

WORKDIR /app

# Copy package files
COPY package*.json ./
RUN npm ci --only=production

# Copy built files
COPY --from=builder /app/dist ./dist

# Create non-root user
RUN addgroup -g 1001 -S nodejs && \
    adduser -S nodejs -u 1001

USER nodejs

EXPOSE 3000 3001

CMD ["node", "dist/index.js"]