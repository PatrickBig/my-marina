#!/bin/sh
set -e
envsubst '${API_BASE_URL} ${APP_URL} ${MARKETING_SITE_URL}' \
  < /usr/share/nginx/html/config.js \
  > /usr/share/nginx/html/config.js.tmp \
  && mv /usr/share/nginx/html/config.js.tmp /usr/share/nginx/html/config.js
exec nginx -g 'daemon off;'
