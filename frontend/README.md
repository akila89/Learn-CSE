# Frontend

Angular 21 app for the CSE Stock Analyser.

## Prerequisites

- Node.js 20+
- A running local Supabase instance (`npx supabase start` from the project root)

## Setup

```bash
npm install
```

Create `src/environments/environment.development.ts` with your local Supabase credentials (printed by `supabase start`):

```ts
export const environment = {
  production: false,
  supabaseUrl: 'http://127.0.0.1:54321',
  supabaseAnonKey: '<anon key from supabase start output>',
};
```

This file is gitignored — never commit real keys.

## Run locally

```bash
npm run start
```

Open `http://localhost:4200`. The app reloads on file changes.

## Unit tests

```bash
npm test
```

## E2E tests (Playwright)

Copy `.env.example` to `.env` and replace the placeholder values with a real test user's credentials:

```bash
cp .env.example .env
```

```
E2E_EMAIL=test-user@example.com
E2E_PASSWORD=your-test-password
```

Then run:

```bash
npm run e2e
```

Use `npm run e2e:ui` to open the Playwright UI. The dev server starts automatically if not already running.

## Build

```bash
ng build
```

Artifacts are written to `dist/`.

## Deployment (Vercel)

Set these environment variables in the Vercel project settings:

- `SUPABASE_URL`
- `SUPABASE_ANON_KEY`

`scripts/set-env.js` generates `environment.prod.ts` from these at build time.
