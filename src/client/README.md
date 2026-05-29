# Home MCP Admin Dashboard

Modern Angular 19 admin dashboard for Home MCP server. Built with:

- **Angular 19** with standalone components & signals
- **Angular Material 3** with light/dark themes
- **Reactive forms** for flexible plugin configuration
- **i18n** (English + Russian) via ngx-translate
- **Bun** package manager for speed & security
- **Responsive design** that works on any device

## Quick Start

> **Location:** `src/client/` in the home-mcp-server repo.
> The Angular admin client lives here; voice/chat clients are in separate repositories.

Install dependencies with Bun:

```bash
cd src/client
bun install
bun start
```

The app opens at `http://localhost:4200` and proxies `/admin/*` API calls to `http://localhost:5201/admin`.

Lint with auto-fix:

```bash
bun run lint          # eslint src --fix
bun run lint:check    # eslint src (read-only)
```

## Build

Production build:

```bash
bun run build:prod
```

Output goes to `dist/home-mcp-admin/`.

## Architecture

```
src/
├── app/
│   ├── core/
│   │   ├── api/               # API service + types
│   │   ├── services/          # Theme, locale services
│   │   └── guards/            # Setup completion guard
│   ├── layout/
│   │   └── shell/             # Main shell with nav + sidenav
│   ├── features/
│   │   ├── setup/             # Setup wizard (multi-step form)
│   │   ├── dashboard/         # Overview + quick stats
│   │   ├── plugins/           # Plugin discovery & configuration
│   │   └── logs/              # Log viewer with filtering
│   ├── app.component.ts       # Root component
│   ├── app.routes.ts          # Route definitions + guards
│   └── app.config.ts          # DI config + i18n setup
├── assets/
│   └── i18n/                  # Translation files (en.json, ru.json)
└── styles.scss                # Global Material 3 themes + utilities
```

## Features

### Setup Wizard
First-time user experience with multi-step form:
- Admin account creation (password validation)
- Server name & default language
- Email confirmation before completion

### Dashboard
Overview of system health:
- Server uptime, version
- Active plugin count
- Recent log entries with source filtering
- Quick navigation to detailed views

### Plugin Management
Browse and configure plugins:
- Plugin list with metadata (version, status, tools)
- Detailed plugin view with tool documentation
- Configuration forms auto-generated from `ConfigFieldDescriptor`
- Field types: text, password, URL, integer, boolean, select
- Multilingual labels & hints from backend schema

### Logs
Real-time log viewer:
- Filter by source (application, plugin, LLM)
- Adjust result limit (50-200)
- Timestamp, level, source ID, message
- Color-coded severity levels

## Theme System

Automatic light/dark mode with localStorage persistence:

- Light: Bright surfaces with primary blues
- Dark: High-contrast dark background

User can toggle via theme button in header. Theme preference persists across sessions.

## Internationalization

Full i18n support via ngx-translate:
- English (en) and Russian (ru)
- Automatic browser language detection
- Manual language switcher in header
- All UI strings in `assets/i18n/*.json`
- Backend labels are resolved dynamically per user locale

## Configuration

### API Proxy

Dev server proxies `/admin/*` to backend. Configure in `proxy.conf.json`:

```json
{
  "/admin": {
    "target": "http://localhost:5201",
    "secure": false,
    "changeOrigin": true
  }
}
```

### Environment Variables

Create `.env.local` for backend overrides:

```
NG_APP_API_URL=http://my-server:5201
NG_APP_DEFAULT_LOCALE=ru
```

(Not currently used by the app, but available for future expansion)

## Performance

- Change detection: `OnPush` by default on all components
- Lazy loading: Feature routes load on demand
- Signals: Reactive state management without RxJS overload
- Bundle size: Material modules tree-shaken for production

## Browser Support

- Chrome/Edge 90+
- Firefox 88+
- Safari 14+
- Mobile browsers (iOS Safari 14+, Chrome Mobile)

## Contributing

Code style enforced by tsconfig strict mode:
- No implicit any
- No null/undefined without proper typing
- Proper control flow analysis
- Exhaustive pattern matching in templates

Components use standalone + signals pattern — minimal boilerplate.

---

Powered by Bun ⚡ and Angular 🚀
