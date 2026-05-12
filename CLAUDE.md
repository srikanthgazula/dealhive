# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**DealHive** is a Groupon-style deals platform. The stack is:
- **Backend**: .NET 10 Web API (Clean Architecture) — `backend/`
- **Frontend**: Next.js 14 App Router + TypeScript + Tailwind CSS — `frontend/`
- **Database**: PostgreSQL 16 (via Docker) — container `dealhive_postgres`
- **Cache**: Redis 7 (via Docker) — container `dealhive_redis`
- **Logging**: Seq (structured logs at `http://localhost:5341`)

## Running the Stack

```bash
# Start all infrastructure (Postgres, Redis, Seq, API, Frontend)
docker compose up -d

# Start just databases (for local dev)
docker compose up -d postgres redis seq
```

**Local dev (without Docker for app code):**
```bash
# Backend — from backend/
dotnet run --project src/GrouponClone.API

# Frontend — from frontend/
npm run dev        # dev server at http://localhost:3000
npm run build      # production build
npm run type-check # TypeScript check without emit
npm run lint       # ESLint
npm run format     # Prettier
```

**Tests:**
```bash
# Frontend unit tests
npm test           # watch mode
npm run test:ci    # CI mode with coverage

# Frontend E2E tests
npm run test:e2e   # Playwright
```

**Database:**
```bash
# Seed data
docker exec -i dealhive_postgres psql -U dealhive -d dealhive < backend/seed.sql

# Run EF migrations (also runs automatically in Development on startup)
dotnet ef database update --project src/GrouponClone.Infrastructure --startup-project src/GrouponClone.API
```

## Key Config

| Variable | Default | Where set |
|---|---|---|
| `NEXT_PUBLIC_API_URL` | `http://localhost:5000/api/v1` | `frontend/next.config.mjs` |
| `NEXT_PUBLIC_SIGNALR_URL` | `http://localhost:5000` | `frontend/next.config.mjs` |
| `ConnectionStrings__DefaultConnection` | postgres://dealhive | `docker-compose.yml` |
| `Jwt__Secret` | `super_secret_jwt_key_at_least_32_chars_long` | `docker-compose.yml` |

Swagger UI is available at `http://localhost:5000/swagger` in Development mode.

**Important**: The API URL must include `/api/v1` — the backend route prefix is `api/v1/[controller]`. Using `/api` (without `/v1`) causes 404s on all server-side fetches.

## Backend Architecture

Clean Architecture with 4 projects:

```
GrouponClone.Domain        — Entities, domain events, value objects (no dependencies)
GrouponClone.Application   — CQRS handlers (MediatR), interfaces, validators (FluentValidation)
GrouponClone.Infrastructure — EF Core DbContext, repositories, Redis, Stripe, SendGrid, Azure Blob
GrouponClone.API           — Controllers, middleware, SignalR hub, DI wiring (Program.cs)
```

**CQRS pattern**: All business logic lives in `Application/Features/*/` as MediatR `IRequest`/`IRequestHandler` pairs. Controllers only call `_mediator.Send(query/command)`. Pipeline behaviors (in order): `LoggingBehavior → ValidationBehavior → PerformanceBehavior`.

**Auth**: ASP.NET Core Identity + JWT bearer tokens. Access token = 15 min, refresh token = 30 days (HttpOnly cookie). Two authorization policies: `"AdminOnly"` (Admin/SuperAdmin roles) and `"VendorOnly"`.

**Key API endpoints:**
- `GET /api/v1/deals?category={slug}&page=1&pageSize=20` — paginated deals
- `GET /api/v1/deals/featured` — featured deals (used by homepage)
- `GET /api/v1/deals/trending` — trending deals (used by homepage)
- `GET /api/v1/deals/{slug}` — deal detail
- `POST /api/v1/auth/login`, `POST /api/v1/auth/register`, `POST /api/v1/auth/refresh`
- `WS /hubs/notifications` — SignalR hub

## Frontend Architecture

Next.js 14 App Router. Server Components for data fetching (homepage, listing pages). Client Components for interactive UI (cart, auth, forms).

**State management**: Redux Toolkit slices in `src/store/slices/`:
- `auth` — user session, access token, login/logout thunks
- `cart` — cart items
- `deals` — client-side deal filter state
- `notifications` — SignalR notifications

**Data fetching pattern**:
- Server Components fetch directly with `fetch()` + ISR (`next: { revalidate: 300 }`)
- Client Components use `@tanstack/react-query` + the Axios `api` client from `src/lib/api.ts`
- The `api` client auto-refreshes the JWT on 401 by calling `/auth/refresh` and retrying

**Route structure** (Groupon-style URLs):

| Route | Purpose |
|---|---|
| `/` | Homepage (featured + trending deals) |
| `/local` | All local deals listing |
| `/local/beauty-and-spas` | Beauty & Spas category |
| `/local/things-to-do` | Things To Do category |
| `/local/food-and-drink` | Food & Drink category |
| `/local/automotive` | Auto & Home category |
| `/travel` | Travel deals |
| `/goods` | Physical goods |
| `/gift` | Gift cards |
| `/search?q=` | Search results |
| `/deals/[slug]` | Deal detail page |
| `/login`, `/signup` | Auth pages |
| `/account/orders` | Order history |
| `/account/orders/[id]` | Order detail |
| `/account/groupons` | Vouchers ("My Groupons") |
| `/account/profile` | Profile settings |
| `/wishlist` | Wishlist |
| `/checkout`, `/cart` | Checkout flow |

Old routes (`/auth/login`, `/deals/health-beauty`, `/account/vouchers`, etc.) are redirected via `next.config.mjs` `redirects()`.

**Shared components:**
- `src/components/deals/DealCard.tsx` — deal card (no CTA button, Groupon-style layout)
- `src/components/deals/DealsListingPage.tsx` — shared server component for all category listing pages
- `src/components/layout/Navbar.tsx` — full Groupon-style navbar with mega-menu dropdowns
- `src/components/providers/Providers.tsx` — wraps QueryClientProvider + Redux Provider

**Styling**: Tailwind CSS v3. Primary green `#53A318`. Custom tokens in `tailwind.config.ts`:
- `primary.DEFAULT` = `#53A318`, `primary.dark` = `#438F10`, `primary.light` = `#EFF7E6`
- `accent.DEFAULT` = `#E31837`
- Font: Inter (Google Fonts via `next/font`)

**Date handling**: The API returns dates in `"DD-MM-YYYY HH:mm:ss"` format. `new Date()` cannot parse this. Use the `parseDate()` helper in `DealCard.tsx` or `parseDateStr()` in `CountdownTimer.tsx` — never call `new Date(dateStr)` directly on API dates.

## Database

PostgreSQL schema is managed by EF Core migrations (`GrouponClone.Infrastructure/Migrations/`). A seed file is at `backend/seed.sql`.

**Key schema facts:**
- `Categories` table uses integer IDs (not UUID). `beauty-spas` slug = ID `8`.
- `Deals`, `Vendors`, `Users` use UUIDs.
- Deal `Status` column is an enum int: `2` = Active. Deal `Type`: `0` = Service, `1` = Travel, `3` = Experience.
- Category slug used in `GET /api/v1/deals?category={slug}` must match the `Slug` column exactly (e.g., `beauty-spas`, not `health-beauty`).
- When seeding new deals: check the highest existing UUID suffix in `seed.sql` for Deals (`000000000014`), Vendors (`000000000013`), Users (`000000000014`).

## Known Gotchas

1. **Category slug mismatch**: `src/app/local/beauty-and-spas/page.tsx` passes `category="health-beauty"` but the DB slug is `beauty-spas`. This returns no results and needs fixing.

2. **Stale `.next` cache**: After changing `tailwind.config.ts` or font config, delete the entire `.next/` directory before restarting. Webpack chunk resolution errors are a symptom.

3. **ISR cache poisoning**: If the server fetches an empty result and caches it, clear `.next/cache/fetch-cache`. In dev, consider adding `cache: 'no-store'` to server fetches.

4. **`lib/utils.ts` `formatDate()`** uses plain `new Date()` — only safe for ISO 8601 strings. Do not pass DD-MM-YYYY API dates to it.

5. **`api.ts` 401 redirect**: On token refresh failure, the client redirects to `/auth/login` (old path). If the new path `/login` is needed, update `src/lib/api.ts` line `window.location.href = '/auth/login'`.
