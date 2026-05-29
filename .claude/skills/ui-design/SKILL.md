---
name: ui-design
description: Design UI screens for a web app end-to-end: read project docs, propose screens and nav structure, confirm layout, draw ASCII wireframes, generate self-contained Tailwind HTML mockups with dark/light mode, review for quality, make responsive, and document UX states and responsive strategy. Use when user says "design UI", "create mockups", "show me screens", "wireframes", or asks to design or mock up any screen.
---

# UI Design

Walk through the process phase by phase. Confirm with the user at the end of each phase before proceeding.

## Phase 1 — Information architecture

Read existing project docs before asking anything: RESEARCH.md, CONTEXT.md, any ADRs in `docs/adr/`. Derive:

- Full list of screens needed
- What navigates to what
- Which screens can be grouped as tabs vs separate pages

Present the proposed screen list and nav structure. Resolve grouping questions explicitly (e.g. "Should Stock Manager be a tab inside Stocks, or a separate nav item?"). Confirm before proceeding.

## Phase 2 — Layout decisions

Propose and confirm:

- **Chrome**: narrow dark sidebar for nav + full-width top bar. Theme toggle and user avatar grouped on the top bar right — single location, never in the sidebar.
- **Theme**: Tailwind `darkMode: 'class'`, toggle in top bar only. Login screen is light-only, no toggle.
- **Scroll reduction**: group related sections into horizontal tabs rather than long single-page scrolls.
- **Mobile nav**: sidebar hidden on mobile (`hidden lg:flex`), replaced by a fixed bottom nav bar (`lg:hidden`) with the same nav items.

## Phase 3 — ASCII wireframes

For any screen with a non-obvious layout, draw an ASCII wireframe and confirm with the user before writing any HTML.

## Phase 4 — HTML mockups

Generate self-contained files in `docs/mockups/`. Each file must:

- Use Tailwind CDN — no build step needed
- Include dark/light toggle (`toggleTheme()`) in the top bar
- Link to every other screen it can navigate to
- Show realistic example data (names, prices, dates, signal badges)
- Include working vanilla JS micro-interactions where they communicate design intent (search filter, annotation toggle)
- Use `.tabular { font-variant-numeric: tabular-nums }` for all numbers

See [MOCKUP-RULES.md](MOCKUP-RULES.md) for the full color palette, signal badge colors, stale indicator markup, and HTML boilerplate.

## Phase 5 — Quality review

After all screens are generated, review for:

- **Dark mode contrast**: three-level hierarchy — page `slate-950`, sidebar `slate-900`, cards `slate-800`. Sidebar needs `border-r border-slate-700`.
- **Stale data**: never show `—` dashes. Show last known values in muted color with `⚠ Stale · [date]` badge. Recommendation signal stays full color.
- **Data consistency**: dates, counts, and statuses must be consistent across all screens (e.g. a run showing "28/28 success" must not coexist with stale stocks from that same run).
- **Nav active states**: each screen must highlight the correct sidebar item and bottom nav item.

## Phase 6 — Responsive

Apply to all authenticated screens (not login):

- Sidebar: `hidden lg:flex`
- Bottom nav: `lg:hidden fixed bottom-0 inset-x-0` with active item `teal-400`, inactive `slate-500`
- Main content: `pb-24 lg:pb-0` to clear the bottom nav when scrolling
- Content padding: `px-4 sm:px-6`
- Card grids: `grid-cols-1 sm:grid-cols-2 lg:grid-cols-3`
- Complex headers (name + price + signal): `flex-col sm:flex-row sm:justify-between`
- Tab bars and tables: `overflow-x-auto`; tables add `min-w-[480px]` on the `<table>`

## Phase 7 — Documentation

Create `docs/ui-prompts-v{n}.md` containing:

- **Global style notes**: font, number formatting, signal badge colors, layout description
- **Screen specs**: one prompt block per screen describing layout and content
- **UX States section**: table of loading / error / success states for every async operation
- **Responsive Strategy section**: breakpoints, grid collapse table, layout rules, header stacking

Commit `docs/mockups/` and `docs/ui-prompts-v{n}.md` together.
