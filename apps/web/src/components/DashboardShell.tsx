import React from 'react'
import { Outlet } from 'react-router-dom'
import { AppSidebar } from './AppSidebar'

/**
 * DashboardShell — simple two-panel flex layout.
 *
 * Structure:
 *   <div class="flex h-screen">
 *     <AppSidebar />   ← document-flow sidebar (shrink-0, pushes content)
 *     <main>           ← takes remaining flex space (flex-1)
 *       <header>       ← top bar with page title
 *       <div>          ← scrollable page content
 *     </main>
 *   </div>
 *
 * Because AppSidebar uses document flow (no fixed/absolute), the <main> area
 * automatically adjusts:
 *   • Sidebar expanded  → main area is narrower (data visible, just less wide)
 *   • Sidebar collapsed → main area is wider   (all data fully visible)
 */
export function DashboardShell() {
  return (
    <div className="flex h-screen w-full overflow-hidden bg-[var(--bg)]">
      {/* Sidebar — document-flow; never overlaps main content */}
      <AppSidebar />

      {/* Main content area */}
      <main className="flex flex-1 flex-col min-w-0 overflow-hidden">
        {/* ── Top header ──────────────────────────────────────────────── */}
        <header className="flex h-12 shrink-0 items-center gap-2 border-b border-[var(--border)] px-4 lg:px-6">
          <h1 className="text-sm font-medium text-[var(--text-h)]">
            Operations Dashboard
          </h1>
        </header>

        {/* ── Page content (nested route renders here) ────────────────── */}
        <div className="flex flex-1 flex-col gap-4 p-4 md:p-6 overflow-auto min-h-0">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
