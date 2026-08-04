FROM node:24-alpine AS build
WORKDIR /app

COPY arcpay-fe/package.json arcpay-fe/package-lock.json ./
RUN npm ci

COPY arcpay-fe/ ./
ARG VITE_API_BASE_URL=/
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:1.29-alpine AS final
COPY arcpay-fe/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 8080
