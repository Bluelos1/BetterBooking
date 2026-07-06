FROM node:22-alpine
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=8080

COPY infra/local/oidc/server.mjs ./server.mjs

USER node
EXPOSE 8080
CMD ["node", "server.mjs"]
