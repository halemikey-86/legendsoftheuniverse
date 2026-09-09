FROM node:22-alpine

WORKDIR /app

COPY package.json ./
RUN npm install

COPY api ./api
COPY web/src/lib ./web/src/lib
COPY scripts ./scripts

ENV NODE_ENV=production
ENV UPLOAD_DIR=/data/uploads
RUN mkdir -p /data/uploads

EXPOSE 8787

CMD ["npx", "tsx", "api/server.ts"]
