# syntax=docker/dockerfile:1
FROM node:22-alpine AS build
WORKDIR /app

# Install deps
COPY package.json package-lock.json ./
RUN npm ci --legacy-peer-deps

COPY . .
RUN npm run build

# Runtime — nginx serving the static files
FROM nginx:1.27-alpine AS runtime
EXPOSE 8080

# envsubst (from gettext) stamps API_BASE_URL into config.js at container start
RUN apk add --no-cache gettext

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

# config.js starts as the template; entrypoint replaces ${API_BASE_URL} before nginx starts
COPY config.js.template /usr/share/nginx/html/config.js
COPY entrypoint.sh /entrypoint.sh

RUN chmod +x /entrypoint.sh \
 && chown -R nginx:nginx /usr/share/nginx/html \
 && chown -R nginx:nginx /var/cache/nginx \
 && chown -R nginx:nginx /var/log/nginx \
 && touch /var/run/nginx.pid \
 && chown nginx:nginx /var/run/nginx.pid

USER nginx
ENTRYPOINT ["/entrypoint.sh"]
