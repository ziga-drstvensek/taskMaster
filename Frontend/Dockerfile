# Frontend Dockerfile
# Step 1: Build the Vue application
FROM node:20-alpine AS build-stage
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build:prod

# Step 2: Serve the application with Nginx
FROM nginx:stable-alpine AS production-stage
COPY --from=build-stage /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
RUN apk add --no-cache openssl && \
  mkdir -p /etc/ssl/certs /etc/ssl/private && \
  openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -subj "/C=SI/ST=Slovenija/L=Ljubljana/O=TaskMaster/CN=localhost" \
    -keyout /etc/ssl/private/nginx-selfsigned.key \
    -out /etc/ssl/certs/nginx-selfsigned.crt
EXPOSE 443
CMD ["nginx", "-g", "daemon off;"]
