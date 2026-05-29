---
name: ui-design
description: "Design UI screens for a web app end-to-end: read project docs, propose screens and nav structure, confirm layout, draw ASCII wireframes, generate self-contained Tailwind HTML mockups with dark/light mode, review for quality, make responsive, and document UX states and responsive strategy. Use when user says 'design UI', 'create mockups', 'show me screens', 'wireframes', or asks to design or mock up any screen."
---

# UI Design

Walk through the process phase by phase. Confirm with the user at the end of each phase before proceeding.

## Phase 1 — Information architecture

Read existing project documentation before asking anything — README, research notes, domain glossary, ADRs, or any design specs. Derive:

- Full list of screens needed
- What navigates to what
- Which screens can be grouped as tabs vs separate pages

Present the proposed screen list and nav structure. Resolve grouping questions explicitly (e.g. "Should Settings be a tab inside Profile, or a separate nav item?"). Confirm before proceeding.

## Phase 2 — Layout decisions

Propose and confirm:

- **Chrome**: decide between sidebar nav, top nav, or bottom nav — and where the theme toggle and user controls live. Keep controls in one consistent location across all screens.
- **Theme**: Tailwind `darkMode: 'class'`. Login / landing screens are typically light-only with no toggle.
- **Scroll reduction**: group related sections into horizontal tabs rather than long single-page scrolls wherever a screen risks excessive height.
- **Mobile nav**: if using a sidebar on desktop, hide it on mobile (`hidden lg:flex`) and replace with a fixed bottom nav bar (`lg:hidden`).

## Phase 3 — ASCII wireframes

For any screen with a non-obvious layout, draw an ASCII wireframe and confirm with the user before writing any HTML.

## Phase 4 — HTML mockups

Generate self-contained files in `docs/mockups/` (or wherever the project keeps design artifacts). Each file must:

- Use Tailwind CDN — no build step needed
- Include dark/light toggle (`toggleTheme()`) in the agreed location
- Link to every other screen it can navigate to
- Show realistic example data appropriate to the domain
- Include working vanilla JS micro-interactions where they communicate design intent (search filter, toggles, tab switching)
- Use `.tabular { font-variant-numeric: tabular-nums }` for any numeric data

See [MOCKUP-RULES.md](MOCKUP-RULES.md) for color hierarchy, status badge patterns, and the HTML layout shell.

## Phase 5 — Quality review

After all screens are generated, review for:

- **Dark mode contrast**: maintain a clear three-level hierarchy — page background, surface/sidebar, card. No two adjacent layers should be the same shade.
- **Data freshness**: if the app has potentially stale or cached data, never replace values with dashes. Show last-known values in muted color with a clear staleness indicator. Computed or AI-generated signals should remain visible even when raw data is stale.
- **Data consistency**: dates, counts, and statuses must be consistent across all screens. A status shown as successful on one screen must not contradict an error shown for the same event on another.
- **Nav active states**: each screen must highlight the correct nav item (sidebar and/or bottom nav).

## Phase 6 — Responsive

Apply to all authenticated screens (not login/landing):

- If sidebar pattern: `hidden lg:flex` on sidebar; `lg:hidden fixed bottom-0 inset-x-0` bottom nav
- Main content: `pb-24 lg:pb-0` to clear the bottom nav when scrolling
- Content padding: `px-4 sm:px-6`
- Card grids: `grid-cols-1 sm:grid-cols-2 lg:grid-cols-3` (adjust columns to content)
- Complex headers (title + metadata + status): `flex-col sm:flex-row sm:justify-between`
- Tab bars and tables: `overflow-x-auto`; tables add a `min-w-[Xpx]` to prevent column collapse

## Phase 7 — Documentation

Create a versioned spec file (e.g. `docs/ui-prompts-v1.md`) containing:

- **Global style notes**: font, number formatting, status badge colors, layout description
- **Screen specs**: one prompt block per screen describing layout and content (useful for generative UI tools or future re-generation)
- **UX States section**: table of loading / error / success states for every async operation
- **Responsive Strategy section**: breakpoints used, grid collapse table, layout rules, header stacking behaviour

Commit `docs/mockups/` and the spec file together with a descriptive message.
