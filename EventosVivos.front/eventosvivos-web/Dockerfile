# Etapa 1: build de la app Angular con Node.
FROM node:24-alpine AS build
WORKDIR /app

# Capa de dependencias cacheable: solo se reinstala si cambian package*.json.
COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# Etapa 2: servir los estáticos con nginx (imagen final liviana, sin Node).
FROM nginx:alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/eventosvivos-web/browser /usr/share/nginx/html
EXPOSE 80
