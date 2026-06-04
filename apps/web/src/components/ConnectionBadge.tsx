import React from 'react'
import { useSelector } from 'react-redux'
import type { RootState } from '../store'

export function ConnectionBadge() {
  const connected = useSelector((s: RootState) => s.metrics.connected)

  return (
    <div
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold border transition-all duration-500 ${
        connected
          ? 'bg-emerald-500/10 border-emerald-500/40 text-emerald-400'
          : 'bg-red-500/10 border-red-500/40 text-red-400'
      }`}
      aria-label={connected ? 'SSE stream connected' : 'SSE stream disconnected'}
    >
      {/* Pulsing dot */}
      <span className="relative flex h-2 w-2">
        {connected && (
          <span
            className="absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"
            style={{ animation: 'ping 1.2s cubic-bezier(0, 0, 0.2, 1) infinite' }}
          />
        )}
        <span
          className={`relative inline-flex rounded-full h-2 w-2 ${
            connected ? 'bg-emerald-500' : 'bg-red-500'
          }`}
        />
      </span>
      {connected ? 'Live' : 'Disconnected'}
    </div>
  )
}
