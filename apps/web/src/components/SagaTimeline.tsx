import React, { useEffect, useRef, useState } from 'react'
import { useSelector } from 'react-redux'
import { api } from '../api/client'
import type { RootState } from '../store'
import type { SagaTrace, SagaTraceEvent } from '@vyaapar-nexus/shared-types'

// ─── Terminal states that stop polling ────────────────────────────────────────
const TERMINAL_STATES = new Set(['OrderCompleted', 'OrderCancelled'])

// ─── Helpers ──────────────────────────────────────────────────────────────────
function isFailedEvent(event: SagaTraceEvent): boolean {
  const t = event.eventType ?? ''
  return (
    t.toLowerCase().includes('fail') ||
    t.toLowerCase().includes('cancel') ||
    t.toLowerCase().includes('error') ||
    t === 'OrderCancelled'
  )
}

function isSuccessEvent(event: SagaTraceEvent): boolean {
  return !isFailedEvent(event)
}

function formatTime(ts: string): string {
  try {
    return new Date(ts).toLocaleTimeString('en-IN', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    })
  } catch {
    return ts ?? ''
  }
}

// ─── Single timeline step ─────────────────────────────────────────────────────
function TimelineStep({ event, index, isLast }: { event: SagaTraceEvent; index: number; isLast: boolean }) {
  const failed = isFailedEvent(event)
  const success = isSuccessEvent(event)

  const dotColor = failed
    ? '#f87171'   // red-400
    : '#34d399'   // emerald-400
  const lineColor = failed ? '#7f1d1d' : '#065f46'

  return (
    <div className="flex items-stretch gap-3">
      {/* Left column: dot + line */}
      <div className="flex flex-col items-center" style={{ width: '16px', flexShrink: 0 }}>
        {/* Dot */}
        <div
          className="rounded-full border-2 flex-shrink-0 z-10"
          style={{
            width: '14px',
            height: '14px',
            marginTop: '3px',
            background: dotColor,
            borderColor: dotColor,
            boxShadow: `0 0 6px ${dotColor}66`,
          }}
        />
        {/* Connecting line */}
        {!isLast && (
          <div
            className="flex-1 w-0.5 mt-1"
            style={{ background: lineColor, minHeight: '20px' }}
          />
        )}
      </div>

      {/* Right column: content */}
      <div
        className="flex flex-col gap-1 pb-4 flex-1"
        style={{ minWidth: 0 }}
      >
        {/* Event type + service */}
        <div className="flex items-baseline gap-2 flex-wrap">
          <span
            className="text-xs font-semibold"
            style={{ color: failed ? '#f87171' : '#34d399' }}
          >
            {event.eventType ?? '—'}
          </span>
          {event.serviceName && (
            <span className="text-[10px] px-1.5 py-0.5 rounded-md border border-[var(--border)] text-[var(--text)] opacity-70">
              {event.serviceName}
            </span>
          )}
        </div>

        {/* Metadata row */}
        <div className="flex items-center gap-3 flex-wrap">
          {/* Timestamp */}
          <span className="text-[10px] text-[var(--text)] opacity-50 font-mono">
            {formatTime(event.createdAt)}
          </span>
          {/* Duration */}
          {event.durationMs != null && (
            <span className="text-[10px] text-[var(--text)] opacity-60">
              ⏱ {event.durationMs} ms
            </span>
          )}
          {/* State transition */}
          {event.previousState && event.currentState && (
            <span className="text-[10px] text-[var(--text)] opacity-50 font-mono">
              {event.previousState} → {event.currentState}
            </span>
          )}
        </div>

        {/* Message if present */}
        {event.message && (
          <span className="text-[10px] text-[var(--text)] opacity-60 italic">
            {event.message}
          </span>
        )}
      </div>
    </div>
  )
}

// ─── Main component ───────────────────────────────────────────────────────────
export function SagaTimeline() {
  const activeOrderId = useSelector((s: RootState) => s.orders.activeOrderId)
  const [trace, setTrace] = useState<SagaTrace | null>(null)
  const [error, setError] = useState<string | null>(null)
  // Track which orderId the current trace belongs to
  const [traceOrderId, setTraceOrderId] = useState<string | null>(null)

  // Fix 2a — pendingOrderId: tracks a new order that's being polled but hasn't
  // returned data yet. Old trace stays visible until first successful response.
  const [pendingOrderId, setPendingOrderId] = useState<string | null>(null)

  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    // Stop any existing poll
    if (intervalRef.current) {
      clearInterval(intervalRef.current)
      intervalRef.current = null
    }
    setError(null)

    if (!activeOrderId) {
      // Fix 2c — no active order: keep showing last trace, do not clear
      return
    }

    // Fix 2a — set pending but do NOT wipe the existing trace yet
    // Old trace stays visible until first successful poll for new order
    setPendingOrderId(activeOrderId)

    const poll = async () => {
      try {
        const response = await api.get<SagaTrace>(`/api/v1/Orders/${activeOrderId}/trace`)
        const data = response.data
        // Fix 2a — only now replace the trace (new data has arrived)
        setTrace(data)
        setTraceOrderId(activeOrderId)
        setPendingOrderId(null)
        setError(null)

        // Stop polling when terminal state is reached
        const events = data?.events ?? []
        const lastEvent = events[events.length - 1]
        if (lastEvent && TERMINAL_STATES.has(lastEvent.currentState)) {
          if (intervalRef.current) {
            clearInterval(intervalRef.current)
            intervalRef.current = null
          }
        }
      } catch (err: any) {
        // 404 means saga trace doesn't exist yet — keep polling silently
        if (err?.response?.status === 404) {
          // Not an error — trace just not ready yet, keep polling
        } else {
          setError(err?.response?.data?.message ?? 'Failed to fetch trace')
        }
      }
    }

    poll()
    intervalRef.current = setInterval(poll, 1000)

    // Fix 2b — cleanup does NOT call setTrace(null); trace persists until
    // replaced by fresh data for the next order
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
    }
  }, [activeOrderId])

  const events = trace?.events ?? []

  return (
    <div
      className="rounded-xl border border-[var(--border)] p-5 flex flex-col gap-3"
      style={{ background: 'rgba(0,0,0,0.04)', minHeight: '240px' }}
    >
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-[var(--text-h)] tracking-wide uppercase">
          Saga Timeline
        </h3>
        {trace?.orderId && (
          <span className="text-[10px] font-mono px-2 py-0.5 rounded-md border border-[var(--border)] text-[var(--text)] opacity-60">
            {trace.orderId.slice(0, 8)}…
          </span>
        )}
      </div>

      {error && (
        <div className="text-xs text-red-400 bg-red-500/10 border border-red-500/30 rounded-lg px-3 py-2">
          ⚠ {error}
        </div>
      )}

      {/* Fix 2d — show subtle loading banner while a new order trace is pending
          but the old trace is still displayed */}
      {pendingOrderId && trace && (
        <div className="flex items-center gap-1.5 text-[10px] text-[var(--text)] opacity-50 italic">
          <svg className="animate-spin w-3 h-3 flex-shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.5}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v2m0 12v2M4 12H2m20 0h-2M6.34 6.34l-1.42-1.41M19.07 19.07l-1.41-1.42M6.34 17.66l-1.42 1.42M19.07 4.93l-1.41 1.42" />
          </svg>
          Loading trace for new order…
        </div>
      )}

      {/* Empty state: only when no trace has ever been loaded */}
      {!trace && !activeOrderId && !pendingOrderId && (
        <div className="flex-1 flex items-center justify-center">
          <p className="text-xs text-[var(--text)] opacity-40 italic text-center">
            No active order — place a demo order to trace its saga events.
          </p>
        </div>
      )}

      {/* Waiting for first events on a new order (no previous trace to show) */}
      {pendingOrderId && !trace && !error && (
        <div className="flex-1 flex items-center justify-center">
          <div className="flex items-center gap-2 text-xs text-[var(--text)] opacity-50">
            <svg className="animate-spin w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2.5}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v2m0 12v2M4 12H2m20 0h-2M6.34 6.34l-1.42-1.41M19.07 19.07l-1.41-1.42M6.34 17.66l-1.42 1.42M19.07 4.93l-1.41 1.42" />
            </svg>
            Waiting for saga events…
          </div>
        </div>
      )}

      {events.length > 0 && (
        <div
          className="flex flex-col overflow-y-auto"
          style={{ maxHeight: '320px' }}
          id="saga-timeline-steps"
        >
          {events.map((event, idx) => (
            <TimelineStep
              key={`${event.eventType}-${idx}`}
              event={event}
              index={idx}
              isLast={idx === events.length - 1}
            />
          ))}
        </div>
      )}
    </div>
  )
}
