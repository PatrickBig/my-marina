# syntax=docker/dockerfile:1
FROM node:22-alpine AS build
WORKDIR /app

# Install deps
COPY package.json package-lock.json ./
RUN npm ci --legacy-peer-deps

# Build — VITE_API_BASE_URL is injected at build time via ARG
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
COPY . .
RUN npm run build

# Runtime — nginx serving the static files
FROM nginx:1.27-alpine AS runtime
EXPOSE 8080

# Use non-root port (8080) in nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html

# nginx runs as nginx user by default; adjust to allow binding port 8080
RUN chown -R nginx:nginx /usr/share/nginx/html \
 && chown -R nginx:nginx /var/cache/nginx \
 && chown -R nginx:nginx /var/log/nginx \
 && touch /var/run/nginx.pid \
 && chown nginx:nginx /var/run/nginx.pid

USER nginx
CMD ["nginx", "-g", "daemon off;"]
