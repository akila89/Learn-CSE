# Mockup Rules

## HTML boilerplate

```html
<script src="https://cdn.tailwindcss.com"></script>
<script>tailwind.config = { darkMode: 'class' }</script>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
<style>body { font-family: 'Inter', sans-serif; } .tabular { font-variant-numeric: tabular-nums; }</style>
```

```html
<script>
  function toggleTheme() {
    document.documentElement.classList.toggle('dark');
    document.getElementById('theme-icon').textContent =
      document.documentElement.classList.contains('dark') ? '☀️' : '🌙';
  }
</script>
```

Login / landing screens: omit the `tailwind.config` dark mode config and the toggle entirely.

---

## Dark mode color hierarchy

The key rule: no two adjacent layers should share the same shade. Three levels minimum.

| Layer | Light | Dark |
|---|---|---|
| Page background | `bg-slate-50` | `dark:bg-slate-950` |
| Sidebar / raised surface | `bg-slate-900` (always dark) | — |
| Top bar / cards | `bg-white` | `dark:bg-slate-800` |
| Card borders | `border-slate-200` | `dark:border-slate-700` |
| Body text | `text-slate-900` | `dark:text-white` |
| Muted text | `text-slate-500` | `dark:text-slate-400` |

If using a dark sidebar: always add `border-r border-slate-700` to create a visible edge in dark mode.

Adapt the palette to the app's brand — the hierarchy principle (dark bg → mid surface → light card) holds regardless of specific shades.

---

## Status / signal badge colors

Define badge colors based on the app's status vocabulary. Common pattern:

| Sentiment | Badge classes |
|---|---|
| Positive / success | `bg-green-500 text-white` |
| Cautious / accumulate | `bg-teal-500 text-white` |
| Neutral / hold | `bg-amber-500 text-white` |
| Warning / reduce | `bg-orange-500 text-white` |
| Negative / danger | `bg-red-500 text-white` |
| Inactive / muted | `bg-slate-400 text-white` |

All badges: `font-semibold px-2.5 py-1 rounded-full text-xs`

Badge colors should be identical in light and dark mode — avoid `dark:` variants on badges.

---

## Data freshness pattern

When the app shows potentially stale or cached data (e.g. background sync hasn't run, API call failed):

- **Never** replace values with `—` dashes — show last known values in muted color instead
- Apply `text-slate-400 dark:text-slate-500` to stale numeric values
- Add a staleness indicator near the value:

```html
<span class="flex items-center gap-1 bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 text-xs font-semibold px-2 py-0.5 rounded-full">
  ⚠ Stale · [date or "cached"]
</span>
```

- For detail screens, add a banner below the tab bar:

```html
<div class="bg-amber-50 dark:bg-amber-900/20 border-b border-amber-200 dark:border-amber-800 px-6 py-2.5 flex items-center gap-2">
  <span class="text-amber-500 text-sm">⚠</span>
  <span class="text-xs font-medium text-amber-700 dark:text-amber-400">Showing data as of [date] — update failed. Last known values displayed.</span>
</div>
```

- Computed or AI-generated signals derived from that data: keep at **full color** — the signal is not stale even if the raw data feed is.

---

## Layout shell (sidebar pattern)

```html
<body class="bg-slate-50 dark:bg-slate-950">
<div class="flex h-screen overflow-hidden">

  <!-- Sidebar: desktop only -->
  <aside class="hidden lg:flex w-48 bg-slate-900 border-r border-slate-700 flex-col flex-shrink-0">
    <!-- logo block -->
    <div class="p-4 border-b border-slate-800">...</div>
    <!-- nav items -->
    <nav class="flex-1 px-2 py-4 space-y-1">
      <!-- active item: bg-slate-700 text-white -->
      <!-- inactive item: text-slate-400 hover:text-white hover:bg-slate-800 -->
    </nav>
  </aside>

  <div class="flex-1 flex flex-col overflow-hidden">

    <!-- Top bar -->
    <header class="h-14 bg-white dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700 flex items-center justify-between px-6 flex-shrink-0">
      <span class="font-bold text-slate-900 dark:text-white text-sm">App Name</span>
      <div class="flex items-center gap-3">
        <button onclick="toggleTheme()" class="p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 text-base leading-none">
          <span id="theme-icon">🌙</span>
        </button>
        <!-- user avatar / controls -->
      </div>
    </header>

    <!-- Main content -->
    <main class="flex-1 overflow-auto bg-slate-50 dark:bg-slate-950 pb-24 lg:pb-0">
      <div class="px-4 sm:px-6 py-6">
        <!-- screen content -->
      </div>
    </main>

  </div>
</div>

<!-- Mobile bottom nav -->
<nav class="lg:hidden fixed bottom-0 inset-x-0 bg-slate-900 border-t border-slate-700 flex z-50">
  <!-- each item: flex-1 flex flex-col items-center py-3 gap-0.5 -->
  <!-- active: text-teal-400 (or brand color) | inactive: text-slate-500 -->
</nav>
</body>
```
