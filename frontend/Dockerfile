FROM node:22-alpine AS build
WORKDIR /src
# Sem baixar o Chrome do puppeteer: ele só serve aos testes, não ao build.
ENV PUPPETEER_SKIP_DOWNLOAD=true
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/ciee-curriculos/browser /usr/share/nginx/html
