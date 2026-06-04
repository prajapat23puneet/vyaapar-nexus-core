import * as React from 'react'
import { NavLink } from 'react-router-dom'
import {
  LayoutDashboardIcon,
  ListIcon,
  PackageIcon,
  UsersIcon,
  ArrowUpCircleIcon,
  PanelLeftCloseIcon,
  PanelLeftOpenIcon,
} from 'lucide-react'
import { ConnectionBadge } from './ConnectionBadge'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'

const navItems = [
  { title: 'Dashboard', url: '/',          icon: LayoutDashboardIcon },
  { title: 'Orders',    url: '/orders',    icon: ListIcon },
  { title: 'Products',  url: '/products',  icon: PackageIcon },
  { title: 'Customers', url: '/customers', icon: UsersIcon },
]

/**
 * AppSidebar — custom document-flow sidebar (no fixed/absolute positioning).
 *
 * Layout contract:
 *   • Expanded  (w-56 / 224px): brand + text labels visible; content area narrows.
 *   • Collapsed (w-14 /  56px): icons only; content area is FULLY visible; hovering
 *     an icon shows a Tooltip with the label (rendered via portal, never clipped).
 *
 * The sidebar lives in the normal flex row alongside the main <main> element, so
 * it NEVER overlaps the content — it only pushes it.
 */
export function AppSidebar() {
  const [expanded, setExpanded] = React.useState(false)

  return (
    <TooltipProvider delayDuration={120}>
      <aside
        onClick={(e) => {
          const t = e.target as HTMLElement
          if (t.closest('button') || t.closest('a')) return
          setExpanded(v => !v)
        }}
        className={`
          relative flex flex-col shrink-0 h-screen
          bg-[var(--sidebar)] text-[var(--sidebar-foreground)]
          border-r border-[var(--sidebar-border)]
          transition-[width] duration-[280ms] ease-in-out
          overflow-hidden
          ${expanded ? 'w-56' : 'w-14'}
        `}
      >
        {/* ── Brand header + toggle ──────────────────────────────────────── */}
        {expanded ? (
          /* Expanded: brand left, collapse button right */
          <div className="flex items-center h-12 shrink-0 px-2 gap-2 border-b border-[var(--sidebar-border)]">
            <a href="/" className="flex items-center gap-2 flex-1 min-w-0 overflow-hidden">
              <ArrowUpCircleIcon className="h-5 w-5 text-[var(--sidebar-primary)] shrink-0" />
              <div className="flex flex-col leading-none overflow-hidden whitespace-nowrap">
                <span className="text-sm font-semibold text-[var(--text-h)]">VyaaparNexus</span>
                <span className="text-[10px] text-[var(--text)]/60">v2</span>
              </div>
            </a>
            <button
              onClick={() => setExpanded(false)}
              className="shrink-0 p-1.5 rounded-md hover:bg-[var(--sidebar-accent)] text-[var(--sidebar-foreground)] transition-colors"
              title="Collapse sidebar"
              aria-label="Collapse sidebar"
            >
              <PanelLeftCloseIcon className="h-4 w-4" />
            </button>
          </div>
        ) : (
          /* Collapsed: whole header is one button — brand icon by default,
             toggle icon revealed on hover via opacity cross-fade            */
          <button
            onClick={() => setExpanded(true)}
            className="group relative flex items-center justify-center h-12 w-full shrink-0 border-b border-[var(--sidebar-border)] hover:bg-[var(--sidebar-accent)] transition-colors"
            title="Expand sidebar"
            aria-label="Expand sidebar"
          >
            {/* Brand icon — shown by default, fades out on hover */}
            <ArrowUpCircleIcon
              className="h-5 w-5 text-[var(--sidebar-primary)] transition-opacity duration-150 group-hover:opacity-0"
            />
            {/* Expand icon — hidden by default, fades in on hover */}
            <PanelLeftOpenIcon
              className="absolute h-4 w-4 text-[var(--sidebar-foreground)] opacity-0 transition-opacity duration-150 group-hover:opacity-100"
            />
          </button>
        )}

        {/* ── Connection badge ───────────────────────────────────────────── */}
        <div
          className={`
            px-2 py-1.5 border-b border-[var(--sidebar-border)]
            ${expanded ? 'flex' : 'flex justify-center'}
          `}
        >
          {/* In collapsed mode, wrap in a Tooltip so users know what the dot means */}
          {expanded ? (
            <ConnectionBadge />
          ) : (
            <Tooltip>
              <TooltipTrigger asChild>
                {/* Render just the pulsing dot when collapsed */}
                <span className="relative flex h-2.5 w-2.5 mt-1 cursor-default">
                  <span className="absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75 animate-ping" />
                  <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-emerald-500" />
                </span>
              </TooltipTrigger>
              <TooltipContent side="right">Live — SSE connected</TooltipContent>
            </Tooltip>
          )}
        </div>

        {/* ── Nav links ─────────────────────────────────────────────────── */}
        <nav className="flex flex-col gap-0.5 p-2 flex-1 overflow-y-auto">
          {navItems.map((item) => (
            <Tooltip key={item.title}>
              <TooltipTrigger asChild>
                <NavLink to={item.url} end={item.url === '/'} className="outline-none">
                  {() =>
                    expanded ? (
                      /* ── Expanded: full-width row with icon + label ── */
                      <div className="flex items-center gap-3 px-3 py-2 rounded-md text-sm w-full transition-colors duration-150 text-[var(--sidebar-foreground)] hover:bg-[var(--sidebar-accent)] hover:text-[var(--sidebar-accent-foreground)]">
                        <item.icon className="h-4 w-4 shrink-0" />
                        <span className="whitespace-nowrap text-sm">{item.title}</span>
                      </div>
                    ) : (
                      /* ── Collapsed: centered circle icon only ── */
                      <div className="w-9 h-9 mx-auto rounded-full flex items-center justify-center transition-colors duration-150 text-[var(--sidebar-foreground)] hover:bg-[var(--sidebar-accent)] hover:text-[var(--sidebar-accent-foreground)]">
                        <item.icon className="h-4 w-4" />
                      </div>
                    )
                  }
                </NavLink>
              </TooltipTrigger>

              {/* Tooltip only shown when collapsed */}
              {!expanded && (
                <TooltipContent side="right" className="text-xs font-medium">
                  {item.title}
                </TooltipContent>
              )}
            </Tooltip>
          ))}
        </nav>
      </aside>
    </TooltipProvider>
  )
}
