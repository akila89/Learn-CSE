# Frontend Setup Notes

## Environment configuration

The Angular app reads Supabase credentials from `environment.ts` files, replaced at build time.

**Local development** — `src/environments/environment.ts`:
```ts
export const environment = {
  production: false,
  supabaseUrl: 'https://<project>.supabase.co',
  supabaseAnonKey: '<anon-key>'
};
```

**Vercel deployment** — set these environment variables in the Vercel project settings:
- `SUPABASE_URL`
- `SUPABASE_ANON_KEY`

Wire them into the Angular build via `angular.json` file replacements or a custom `build:production` script that writes `environment.prod.ts` from `process.env` before the build runs. Never commit real keys to source — `environment.ts` with real values goes in `.gitignore`.
