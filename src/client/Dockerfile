# ─────────────────────────────────────────────────────────────────────────────
# Home MCP admin UI (Angular 19) — built with Bun, served by nginx.
# Internal-only admin panel; reverse-proxies /admin and /api to the backend.
# ─────────────────────────────────────────────────────────────────────────────

FROM oven/bun:1 AS build
WORKDIR /app

# Install deps from the committed lockfile (cached until package.json/bun.lock change)
COPY package.json bun.lock ./
RUN bun install --frozen-lockfile

# Build the production bundle → dist/home-mcp-admin/browser
COPY . .
RUN bun run build

# ─────────────────────────────────────────────────────────────────────────────
FROM nginx:1.27-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/home-mcp-admin/browser /usr/share/nginx/html

EXPOSE 8080
