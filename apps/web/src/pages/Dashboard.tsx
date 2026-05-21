import React from 'react'
import { useSelector } from 'react-redux'
import { useSystemStream } from '../hooks/useSystemStream'
import type { RootState } from '../store'

export function Dashboard() {
  useSystemStream()
  const { connected, activeSagas, ordersPerMinute, sagaSuccessRate, p95LatencyMs, cpuPercent, memoryPercent, deadLetterCount, outboxPending } = useSelector((s: RootState) => s.metrics)

  const safeSagaSuccessRate = typeof sagaSuccessRate === 'number' && !isNaN(sagaSuccessRate) ? sagaSuccessRate : 100
  const safeCpuPercent = typeof cpuPercent === 'number' && !isNaN(cpuPercent) ? cpuPercent : 0
  const safeMemoryPercent = typeof memoryPercent === 'number' && !isNaN(memoryPercent) ? memoryPercent : 0

  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-4">Dashboard</h1>
      <div className="mb-4 flex items-center gap-2">
        <span className={`inline-block w-3 h-3 rounded-full ${connected ? 'bg-green-500' : 'bg-red-500'}`} />
        <span className="text-sm">{connected ? 'Live' : 'Disconnected'}</span>
      </div>
      <div className="grid grid-cols-4 gap-4">
        {[
          { label: 'Active Sagas', value: activeSagas ?? 0 },
          { label: 'Orders/min', value: ordersPerMinute ?? 0 },
          { label: 'Saga Success %', value: `${safeSagaSuccessRate.toFixed(1)}%` },
          { label: 'p95 Latency ms', value: p95LatencyMs ?? 0 },
          { label: 'CPU %', value: `${safeCpuPercent.toFixed(1)}%` },
          { label: 'Memory %', value: `${safeMemoryPercent.toFixed(1)}%` },
          { label: 'Dead Letters', value: deadLetterCount ?? 0 },
          { label: 'Outbox Pending', value: outboxPending ?? 0 },
        ].map(({ label, value }) => (
          <div key={label} className="rounded border p-4 bg-card">
            <div className="text-sm text-muted-foreground">{label}</div>
            <div className="text-2xl font-bold mt-1">{value}</div>
          </div>
        ))}
      </div>
    </div>
  )
}
