import React from 'react'
import { useSelector } from 'react-redux'
import { Card, Metric, Text, Flex } from '@tremor/react'
import type { RootState } from '../store'

// ─── Threshold helpers → Tremor decoration colors ─────────────────────────────

type TremorColor =
  | 'red'
  | 'orange'
  | 'amber'
  | 'yellow'
  | 'lime'
  | 'green'
  | 'emerald'
  | 'teal'
  | 'cyan'
  | 'sky'
  | 'blue'
  | 'zinc'
  | 'pink'
  | 'rose'
  | 'slate'
  | 'gray'
  | 'neutral'
  | 'stone'

type DeltaType = 'increase' | 'moderateIncrease' | 'unchanged' | 'moderateDecrease' | 'decrease'

// ─── Arrow badge with guaranteed white icon contrast ─────────────────────────
// Tremor's BadgeDelta renders the arrow in the same hue as the badge which
// makes it invisible in dark mode. This component always uses white text.
function ArrowBadge({ deltaType, size }: { deltaType: DeltaType; size?: string }) {
  const map: Record<DeltaType, { bg: string; arrow: string }> = {
    increase:         { bg: 'bg-emerald-500',  arrow: '↑' },
    moderateIncrease: { bg: 'bg-amber-500',    arrow: '↑' },
    unchanged:        { bg: 'bg-zinc-600',     arrow: '—' },
    moderateDecrease: { bg: 'bg-amber-600',    arrow: '↓' },
    decrease:         { bg: 'bg-red-500',      arrow: '↓' },
  }
  const { bg, arrow } = map[deltaType]
  return (
    <span
      className={`inline-flex items-center justify-center w-5 h-5 rounded text-[11px] font-bold text-white leading-none select-none ${bg}`}
      aria-hidden
    >
      {arrow}
    </span>
  )
}

function getDeadLetterSeverity(count: number): { color: TremorColor; delta: DeltaType; subtext: string } {
  if (count > 0) return { color: 'red', delta: 'increase', subtext: 'Messages require investigation' }
  return { color: 'emerald', delta: 'unchanged', subtext: 'Queue clear' }
}

function getOutboxSeverity(pending: number): { color: TremorColor; delta: DeltaType; subtext: string } {
  if (pending > 50) return { color: 'red', delta: 'increase', subtext: 'Critical backlog' }
  if (pending > 0) return { color: 'amber', delta: 'moderateIncrease', subtext: 'Messages queued for dispatch' }
  return { color: 'emerald', delta: 'unchanged', subtext: 'All dispatched' }
}

function getLatencySeverity(ms: number): { color: TremorColor; delta: DeltaType; subtext: string } {
  if (ms > 1000) return { color: 'red', delta: 'increase', subtext: 'Critical — > 1 000 ms' }
  if (ms > 500) return { color: 'amber', delta: 'moderateIncrease', subtext: 'Warning — > 500 ms' }
  return { color: 'emerald', delta: 'unchanged', subtext: 'Within acceptable range' }
}

function getSuccessRateSeverity(rate: number): { color: TremorColor; delta: DeltaType; subtext: string } {
  if (rate < 90) return { color: 'red', delta: 'decrease', subtext: 'Below 90% threshold' }
  return { color: 'emerald', delta: 'unchanged', subtext: 'Within acceptable range' }
}

function getCpuMemSeverity(pct: number, label: string): { color: TremorColor; delta: DeltaType; subtext: string } {
  if (pct > 80) return { color: 'red', delta: 'increase', subtext: `High ${label} utilization` }
  return { color: 'emerald', delta: 'unchanged', subtext: 'Normal' }
}

// ─── Icons (inline SVG) ───────────────────────────────────────────────────────
const icons = {
  saga: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} className="w-4 h-4">
      <path strokeLinecap="round" strokeLinejoin="round" d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01" />
    </svg>
  ),
  orders: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} className="w-4 h-4">
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
    </svg>
  ),
  success: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} className="w-4 h-4">
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
  ),
  latency: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} className="w-4 h-4">
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
    </svg>
  ),
  cpu: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} className="w-4 h-4">
      <rect x="4" y="4" width="16" height="16" rx="2" />
      <path strokeLinecap="round" d="M9 9h6v6H9z" />
      <path strokeLinecap="round" d="M9 1v3M15 1v3M9 20v3M15 20v3M1 9h3M1 15h3M20 9h3M20 15h3" />
    </svg>
  ),
  memory: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} className="w-4 h-4">
      <path strokeLinecap="round" strokeLinejoin="round" d="M4 7h16M4 17h16M4 12h4m4 0h4" />
      <rect x="2" y="5" width="20" height="14" rx="2" />
    </svg>
  ),
  dead: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} className="w-4 h-4">
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
    </svg>
  ),
  outbox: (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} className="w-4 h-4">
      <path strokeLinecap="round" strokeLinejoin="round" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0H4m16 0l-3-3m-10 3l3-3" />
    </svg>
  ),
}

// ─── Main export ──────────────────────────────────────────────────────────────
export function MetricCards() {
  const {
    activeSagas,
    ordersPerMinute,
    sagaSuccessRate,
    p95LatencyMs,
    cpuPercent,
    memoryPercent,
    deadLetterCount,
    outboxPending,
  } = useSelector((s: RootState) => s.metrics)

  const safeSagaSuccessRate =
    typeof sagaSuccessRate === 'number' && !isNaN(sagaSuccessRate) ? sagaSuccessRate : 100
  const safeCpu = typeof cpuPercent === 'number' && !isNaN(cpuPercent) ? cpuPercent : 0
  const safeMem = typeof memoryPercent === 'number' && !isNaN(memoryPercent) ? memoryPercent : 0
  const safeLatency = typeof p95LatencyMs === 'number' && !isNaN(p95LatencyMs) ? p95LatencyMs : 0

  const latency = getLatencySeverity(safeLatency)
  const successRate = getSuccessRateSeverity(safeSagaSuccessRate)
  const cpu = getCpuMemSeverity(safeCpu, 'CPU')
  const mem = getCpuMemSeverity(safeMem, 'memory')
  const deadLetter = getDeadLetterSeverity(deadLetterCount ?? 0)
  const outbox = getOutboxSeverity(outboxPending ?? 0)

  return (
    <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
      {/* Active Sagas — neutral blue, no PRD threshold */}
      <Card id="kpi-active-sagas" decoration="left" decorationColor="zinc">
        <Flex justifyContent="between" alignItems="center">
          <Text className="text-xs font-semibold uppercase tracking-widest">Active Sagas</Text>
          <span className="opacity-50">{icons.saga}</span>
        </Flex>
        <Metric className="mt-2">{activeSagas ?? 0}</Metric>
        <Text className="mt-1 text-[11px] text-tremor-content-subtle">In-flight saga orchestrations</Text>
      </Card>

      {/* Orders / min — neutral */}
      <Card id="kpi-orders-per-min" decoration="left" decorationColor="slate">
        <Flex justifyContent="between" alignItems="center">
          <Text className="text-xs font-semibold uppercase tracking-widest">Orders / min</Text>
          <span className="opacity-60">{icons.orders}</span>
        </Flex>
        <Metric className="mt-2">{ordersPerMinute ?? 0}</Metric>
        <Text className="mt-1 text-[11px] text-tremor-content-subtle">Rolling 60s throughput</Text>
      </Card>

      {/* Saga Success Rate — PRD: < 90% → Red */}
      <Card id="kpi-saga-success-rate" decoration="left" decorationColor={successRate.color}>
        <Flex justifyContent="between" alignItems="center">
          <Text className="text-xs font-semibold uppercase tracking-widest">Saga Success Rate</Text>
          <span className="opacity-60">{icons.success}</span>
        </Flex>
        <Flex alignItems="baseline" className="gap-2 mt-2">
          <Metric>{safeSagaSuccessRate.toFixed(1)}%</Metric>
          <ArrowBadge deltaType={successRate.delta} size="sm" />
        </Flex>
        <Text className="mt-1 text-[11px] text-tremor-content-subtle">{successRate.subtext}</Text>
      </Card>

      {/* p95 Latency — PRD: > 500 → Amber, > 1000 → Red */}
      <Card id="kpi-p95-latency" decoration="left" decorationColor={latency.color}>
        <Flex justifyContent="between" alignItems="center">
          <Text className="text-xs font-semibold uppercase tracking-widest">p95 Latency</Text>
          <span className="opacity-60">{icons.latency}</span>
        </Flex>
        <Flex alignItems="baseline" className="gap-2 mt-2">
          <Metric>{safeLatency} ms</Metric>
          <ArrowBadge deltaType={latency.delta} size="sm" />
        </Flex>
        <Text className="mt-1 text-[11px] text-tremor-content-subtle">{latency.subtext}</Text>
      </Card>

      {/* CPU Usage — PRD: > 80% → Red */}
      <Card id="kpi-cpu" decoration="left" decorationColor={cpu.color}>
        <Flex justifyContent="between" alignItems="center">
          <Text className="text-xs font-semibold uppercase tracking-widest">CPU Usage</Text>
          <span className="opacity-60">{icons.cpu}</span>
        </Flex>
        <Flex alignItems="baseline" className="gap-2 mt-2">
          <Metric>{safeCpu.toFixed(1)}%</Metric>
          <ArrowBadge deltaType={cpu.delta} size="sm" />
        </Flex>
        <Text className="mt-1 text-[11px] text-tremor-content-subtle">{cpu.subtext}</Text>
      </Card>

      {/* Memory Usage — PRD: > 80% → Red */}
      <Card id="kpi-memory" decoration="left" decorationColor={mem.color}>
        <Flex justifyContent="between" alignItems="center">
          <Text className="text-xs font-semibold uppercase tracking-widest">Memory Usage</Text>
          <span className="opacity-60">{icons.memory}</span>
        </Flex>
        <Flex alignItems="baseline" className="gap-2 mt-2">
          <Metric>{safeMem.toFixed(1)}%</Metric>
          <ArrowBadge deltaType={mem.delta} size="sm" />
        </Flex>
        <Text className="mt-1 text-[11px] text-tremor-content-subtle">{mem.subtext}</Text>
      </Card>

      {/* Dead Letters — PRD: > 0 → Red */}
      <Card id="kpi-dead-letter" decoration="left" decorationColor={deadLetter.color}>
        <Flex justifyContent="between" alignItems="center">
          <Text className="text-xs font-semibold uppercase tracking-widest">Dead Letters</Text>
          <span className="opacity-60">{icons.dead}</span>
        </Flex>
        <Flex alignItems="baseline" className="gap-2 mt-2">
          <Metric>{deadLetterCount ?? 0}</Metric>
          <ArrowBadge deltaType={deadLetter.delta} size="sm" />
        </Flex>
        <Text className="mt-1 text-[11px] text-tremor-content-subtle">{deadLetter.subtext}</Text>
      </Card>

      {/* Outbox Pending — PRD: > 0 → Amber, > 50 → Red */}
      <Card id="kpi-outbox-pending" decoration="left" decorationColor={outbox.color}>
        <Flex justifyContent="between" alignItems="center">
          <Text className="text-xs font-semibold uppercase tracking-widest">Outbox Pending</Text>
          <span className="opacity-60">{icons.outbox}</span>
        </Flex>
        <Flex alignItems="baseline" className="gap-2 mt-2">
          <Metric>{outboxPending ?? 0}</Metric>
          <ArrowBadge deltaType={outbox.delta} size="sm" />
        </Flex>
        <Text className="mt-1 text-[11px] text-tremor-content-subtle">{outbox.subtext}</Text>
      </Card>
    </div>
  )
}
