import React, { useEffect, useRef } from 'react'
import { useSelector } from 'react-redux'
import type { RootState } from '../store'
import type { LogEntry } from '@vyaapar-nexus/shared-types'

// ─── Color coding by log level ────────────────────────────────────────────────
function levelColor(level: string): string {
  const l = (level ?? '').toLowerCase()
  if (l === 'error' || l === 'critical' || l === 'fatal') return '#f87171' // red-400
  if (l === 'warning' || l === 'warn') return '#fbbf24'                     // amber-400
  if (l === 'information' || l === 'info') return '#38bdf8'                 // sky-400
  if (l === 'debug') return '#a3a3a3'                                        // neutral-400
  return '#94a3b8'                                                           // slate-400
}

function levelBadge(level: string): string {
  const l = (level ?? '').toLowerCase()
  if (l === 'error' || l === 'critical' || l === 'fatal') return 'ERR'
  if (l === 'warning' || l === 'warn') return 'WRN'
  if (l === 'information' || l === 'info') return 'INF'
  if (l === 'debug') return 'DBG'
  return l.slice(0, 3).toUpperCase()
}

function formatTimestamp(ts: string): string {
  try {
    const d = new Date(ts)
    return d.toLocaleTimeString('en-IN', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    })
  } catch {
    return ts ?? ''
  }
}

// ─── Single log line ──────────────────────────────────────────────────────────
function LogLine({ entry, index }: { entry: LogEntry; index: number }) {
  const color = levelColor(entry.level)
  const badge = levelBadge(entry.level)

  return (
    <div
      className="flex items-start gap-2 py-0.5 px-2 hover:bg-white/5 rounded"
      style={{ fontFamily: 'ui-monospace, Consolas, "Cascadia Code", monospace' }}
    >
      {/* Line number */}
      <span style={{ color: '#4b5563', minWidth: '28px', textAlign: 'right', fontSize: '10px', userSelect: 'none' }}>
        {index + 1}
      </span>

      {/* Timestamp */}
      <span style={{ color: '#6b7280', fontSize: '10px', whiteSpace: 'nowrap', minWidth: '60px' }}>
        {formatTimestamp(entry.timestamp)}
      </span>

      {/* Level badge */}
      <span
        style={{
          color,
          fontSize: '10px',
          fontWeight: 700,
          minWidth: '28px',
          letterSpacing: '0.05em',
        }}
      >
        {badge}
      </span>

      {/* Service */}
      {entry.service && (
        <span style={{ color: '#a855f7', fontSize: '10px', minWidth: '80px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
          [{entry.service}]
        </span>
      )}

      {/* Message */}
      <span style={{ color, fontSize: '11px', wordBreak: 'break-word', flex: 1 }}>
        {entry.message}
      </span>

      {/* Correlation ID */}
      {entry.correlationId && (
        <span style={{ color: '#6b7280', fontSize: '9px', opacity: 0.6, whiteSpace: 'nowrap' }}>
          {entry.correlationId.slice(0, 8)}
        </span>
      )}
    </div>
  )
}

// ─── Main component ───────────────────────────────────────────────────────────
const MAX_LOGS = 50

export function LogTerminal() {
  const allLogs = useSelector((s: RootState) => s.metrics.recentLogs)
  // Ref points directly at the scrollable container div, not a child anchor.
  // Using scrollTop instead of scrollIntoView prevents the scroll from
  // bubbling up to outer page ancestors.
  const scrollContainerRef = useRef<HTMLDivElement>(null)

  // Cap to last 50 logs, oldest first (state stores newest first from metricsSlice)
  const logs: LogEntry[] = [...(allLogs ?? [])].reverse().slice(-MAX_LOGS)

  // Auto-scroll the INNER container to its bottom whenever logs change.
  // We mutate scrollTop directly so the browser never touches the outer page scroll.
  useEffect(() => {
    const el = scrollContainerRef.current
    if (el) {
      el.scrollTop = el.scrollHeight
    }
  }, [allLogs])

  return (
    <div
      className="flex flex-col rounded-xl border border-[var(--border)] overflow-hidden"
      style={{ minHeight: '240px', maxHeight: '400px' }}
    >
      {/* Terminal title bar */}
      <div
        className="flex items-center gap-2 px-3 py-2 border-b border-[var(--border)]"
        style={{ background: '#111113' }}
      >
        <div className="flex gap-1.5">
          <span className="w-2.5 h-2.5 rounded-full bg-red-500/80" />
          <span className="w-2.5 h-2.5 rounded-full bg-amber-400/80" />
          <span className="w-2.5 h-2.5 rounded-full bg-emerald-500/80" />
        </div>
        <span
          className="text-[10px] font-semibold tracking-widest uppercase ml-2 opacity-50"
          style={{ color: '#9ca3af', fontFamily: 'ui-monospace, Consolas, monospace' }}
        >
          System Log Stream
        </span>
        <div className="flex-1" />
        <span style={{ color: '#4b5563', fontSize: '10px', fontFamily: 'monospace' }}>
          {logs.length}/{MAX_LOGS} lines
        </span>
      </div>

      {/* Log output area — ref lives here so scrollTop targets this div only */}
      <div
        ref={scrollContainerRef}
        className="flex-1 overflow-y-auto py-2"
        style={{ background: '#000000' }}
        id="log-terminal-output"
      >
        {logs.length === 0 ? (
          <div
            style={{
              color: '#374151',
              fontSize: '11px',
              fontFamily: 'ui-monospace, monospace',
              padding: '12px 16px',
            }}
          >
            Waiting for log entries…
          </div>
        ) : (
          logs.map((entry, idx) => (
            <LogLine key={`${entry.timestamp}-${idx}`} entry={entry} index={idx} />
          ))
        )}
      </div>
    </div>
  )
}
