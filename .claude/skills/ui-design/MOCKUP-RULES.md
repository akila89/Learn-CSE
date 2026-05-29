# Mockup Rules

## HTML boilerplate (every authenticated screen)

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

Login screen: omit `tailwind.config` dark mode config and the toggle entirely — login is always light.

---

## Dark mode color hierarchy

| Layer | Light | Dark |
|---|---|---|
| Page background | `bg-slate-50` | `dark:bg-slate-950` |
| Sidebar | `bg-slate-900` (always dark) | — |
| Top bar / cards | `bg-white` | `dark:bg-slate-800` |
| Card borders | `border-slate-200` | `dark:border-slate-700` |
| Body text | `text-slate-900` | `dark:text-white` |
| Muted text | `text-slate-500` | `dark:text-slate-400` |
| Sidebar border | `border-r border-slate-700` (always) | — |

The sidebar is always `bg-slate-900` regardless of theme. In dark mode: page `slate-950` is darker than sidebar `slate-900`, which is darker than cards `slate-800`. This three-level contrast must be preserved.

---

## Signal badge colors

| Signal | Classes |
|---|---|
| BUY | `bg-green-500 text-white` |
| ACCUMULATE | `bg-teal-500 text-white` |
| HOLD | `bg-amber-500 text-white` |
| REDUCE | `bg-orange-500 text-white` |
| SELL | `bg-red-500 text-white` |

All badges: `font-semibold px-2.5 py-1 rounded-full text-xs`

Badge colors are identical in light and dark mode — do not apply dark: variants.

---

## Stale data pattern

When a stock's price data is stale (scraper failed to update it):

**Never** replace data with `—` dashes. Show last known values in muted color.

Price and indicator values: `text-slate-400 dark:text-slate-500` (muted grey)  
Add `last known` label next to the price in small muted text.

Stale badge:
```html
<span class="flex items-center gap-1 bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 text-xs font-semibold px-2 py-0.5 rounded-full">⚠ Stale · [date]</span>
```

Stale notice banner (detail screens, below tab bar):
```html
<div class="bg-amber-50 dark:bg-amber-900/20 border-b border-amber-200 dark:border-amber-800 px-6 py-2.5 flex items-center gap-2">
  <span class="text-amber-500 text-sm">⚠</span>
  <span class="text-xs font-medium text-amber-700 dark:text-amber-400">Price data as of [date] — last scrape partially failed. Showing last known values.</span>
</div>
```

Recommendation signal badge: **always full color** — the AI signal is not stale even when the price feed is.

---

## Outer layout shell (all authenticated screens)

```html
<body class="bg-slate-50 dark:bg-slate-950">
<div class="flex h-screen overflow-hidden">

  <!-- Sidebar (desktop only) -->
  <aside class="hidden lg:flex w-48 bg-slate-900 border-r border-slate-700 flex-col flex-shrink-0">
    <!-- logo + nav items -->
  </aside>

  <div class="flex-1 flex flex-col overflow-hidden">

    <!-- Top bar -->
    <header class="h-14 bg-white dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700 flex items-center justify-between px-6 flex-shrink-0">
      <span class="font-bold text-slate-900 dark:text-white text-sm">App Name</span>
      <div class="flex items-center gap-3">
        <button onclick="toggleTheme()" ...><span id="theme-icon">🌙</span></button>
        <!-- user avatar -->
      </div>
    </header>

    <!-- Main content -->
    <main class="flex-1 overflow-auto bg-slate-50 dark:bg-slate-950 pb-24 lg:pb-0">
      <!-- screen content with px-4 sm:px-6 padding -->
    </main>

  </div>
</div>

<!-- Mobile bottom nav -->
<nav class="lg:hidden fixed bottom-0 inset-x-0 bg-slate-900 border-t border-slate-700 flex z-50">
  <!-- 3 items: active = text-teal-400, inactive = text-slate-500 -->
</nav>
</body>
```
