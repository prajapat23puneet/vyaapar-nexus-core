import React, { useEffect, useRef, useState, useCallback } from 'react'
import { useSelector } from 'react-redux'
import { api } from '../api/client'
import type { RootState } from '../store'
import type { SagaState } from '@vyaapar-nexus/shared-types'

// ─── Types ────────────────────────────────────────────────────────────────────
type NodeName = 'Order' | 'Inventory' | 'Payment' | 'Shipping' | 'Notification'
type NodeStatus = 'idle' | 'active' | 'done' | 'failed'

interface NodeDef {
  name: NodeName
  id: string
}

const NODES: NodeDef[] = [
  { name: 'Order',        id: 'mesh-node-order' },
  { name: 'Inventory',    id: 'mesh-node-inventory' },
  { name: 'Payment',      id: 'mesh-node-payment' },
  { name: 'Shipping',     id: 'mesh-node-shipping' },
  { name: 'Notification', id: 'mesh-node-notification' },
]

// ─── Saga state → node truth mapping ─────────────────────────────────────────
// Returns the TRUE statuses for a given backend currentState string.
function deriveNodeStatuses(currentState: string): Record<NodeName, NodeStatus> {
  const cs = currentState ?? ''
  const isCompleted = cs === 'OrderCompleted'
  const isCancelled = cs === 'OrderCancelled'

  return {
    Order:
      cs === 'Submitted'
        ? 'active'
        : cs
        ? 'done'
        : 'idle',

    Inventory:
      cs === 'InventoryChecking'
        ? 'active'
        : cs === 'InventoryFailed'
        ? 'failed'
        : ['InventoryReserved', 'PaymentProcessing', 'PaymentProcessed', 'PaymentFailed',
           'ShippingDispatching', 'ShippingDispatched', 'NotificationSending',
           'OrderCompleted', 'OrderCancelled'].includes(cs)
        ? 'done'
        : 'idle',

    Payment:
      cs === 'PaymentProcessing'
        ? 'active'
        : cs === 'PaymentFailed'
        ? 'failed'
        : ['PaymentProcessed', 'ShippingDispatching', 'ShippingDispatched',
           'NotificationSending', 'OrderCompleted', 'OrderCancelled'].includes(cs)
        ? 'done'
        : 'idle',

    Shipping:
      cs === 'ShippingDispatching'
        ? 'active'
        : ['ShippingDispatched', 'NotificationSending', 'OrderCompleted', 'OrderCancelled'].includes(cs)
        ? 'done'
        : 'idle',

    Notification:
      cs === 'NotificationSending'
        ? 'active'
        : isCompleted
        ? 'done'
        : isCancelled
        ? 'failed'
        : 'idle',
  }
}

// ─── Node rank — used to sequence the animation ───────────────────────────────
// Higher rank = later in the pipeline.
const NODE_RANK: Record<NodeName, number> = {
  Order: 0, Inventory: 1, Payment: 2, Shipping: 3, Notification: 4,
}

// STATUS_WEIGHT — used to compare "how far along" a status is.
// idle(0) < active(1) < done/failed(2)
const STATUS_WEIGHT: Record<NodeStatus, number> = {
  idle: 0, active: 1, done: 2, failed: 2,
}

// ─── Animation step delay ─────────────────────────────────────────────────────
// How many ms to wait between lighting up each successive node.
const STEP_MS = 420

// ─── Styles ───────────────────────────────────────────────────────────────────
function nodeBoxClass(status: NodeStatus): string {
  switch (status) {
    case 'done':
      return [
        'border-emerald-500 bg-emerald-500/12 text-emerald-300',
        'shadow-[0_0_10px_rgba(16,185,129,0.25)]',
      ].join(' ')
    case 'active':
      return [
        'border-emerald-400 bg-emerald-400/10 text-emerald-200',
        // ring pulse — expands outward instead of opacity fade
        'ring-2 ring-emerald-400/40 ring-offset-0',
        // CSS keyframe-based glow pulse (defined inline below)
        'animate-[meshpulse_1.1s_ease-in-out_infinite]',
      ].join(' ')
    case 'failed':
      return 'border-red-500 bg-red-500/12 text-red-300 shadow-[0_0_8px_rgba(239,68,68,0.2)]'
    default:
      return 'border-border bg-muted/20 text-muted-foreground'
  }
}

function nodeStatusLabel(status: NodeStatus): string {
  switch (status) {
    case 'done':   return '✓ Done'
    case 'active': return '● Active'
    case 'failed': return '✗ Failed'
    default:       return '○ Idle'
  }
}

// ─── Connector ────────────────────────────────────────────────────────────────
function Connector({ lit }: { lit: boolean }) {
  return (
    <div className="flex-1 relative flex items-center min-w-[16px]">
      <div
        className={`h-px w-full transition-colors duration-500 ${
          lit ? 'bg-emerald-500/60' : 'bg-border/50'
        }`}
      />
      {/* arrowhead */}
      <div
        className={`absolute right-0 w-0 h-0
          border-t-[4px] border-b-[4px] border-l-[5px]
          border-t-transparent border-b-transparent
          transition-colors duration-500
          ${lit ? 'border-l-emerald-500/70' : 'border-l-border/50'}
        `}
        style={{ transform: 'translateX(4px)' }}
      />
    </div>
  )
}

// ─── Circuit breaker overlay ──────────────────────────────────────────────────
function CircuitBreakerOverlay({ state }: { state: string }) {
  const isOpen = state === 'Open'
  const isHalf = state === 'HalfOpen'
  if (!isOpen && !isHalf) return null
  return (
    <div
      className={`absolute -top-3.5 left-1/2 -translate-x-1/2 flex items-center gap-1
        px-2 py-0.5 rounded-full text-[9px] font-bold border z-10 whitespace-nowrap
        ${isOpen
          ? 'bg-red-600/90 border-red-400 text-white animate-pulse'
          : 'bg-amber-500/90 border-amber-300 text-white'
        }`}
      title={`Circuit breaker is ${state}`}
    >
      ⚡ {state}
    </div>
  )
}

// ─── Main component ───────────────────────────────────────────────────────────
export function ServiceMesh() {
  const activeOrderId  = useSelector((s: RootState) => s.orders.activeOrderId)
  const circuitStates  = useSelector((s: RootState) => s.metrics.circuitStates)
  const paymentCircuit = circuitStates?.payment

  // serverState — the raw latest state from the backend
  const [serverState, setServerState]   = useState<string>('')
  // serverStateRef — tracks the latest server state to avoid stale closure in poll()
  const serverStateRef = useRef<string>('')
  const [fetchError,  setFetchError]    = useState<string | null>(null)

  // displayStatuses — what is ACTUALLY shown (animated, lagging behind serverState
  // intentionally to produce the one-by-one lighting effect)
  const [displayStatuses, setDisplayStatuses] = useState<Record<NodeName, NodeStatus>>(
    () => deriveNodeStatuses('')
  )

  const intervalRef  = useRef<ReturnType<typeof setInterval>  | null>(null)
  const animQueueRef = useRef<ReturnType<typeof setTimeout>[] >([])

  // Fix 1b — keep a ref in sync with displayStatuses so poll() can read the
  // current value without a stale closure
  const displayStatusesRef = useRef(displayStatuses)
  useEffect(() => {
    displayStatusesRef.current = displayStatuses
  }, [displayStatuses])

  // ── Cancel all pending animation steps ──────────────────────────────────────
  const cancelAnimQueue = useCallback(() => {
    animQueueRef.current.forEach(clearTimeout)
    animQueueRef.current = []
  }, [])

  // ── Walk displayStatuses from current → target, one node at a time ──────────
  // Only advances nodes whose rank ≤ the highest active/done rank in target.
  // This gives the "left-to-right lighting" feel.
  //
  // Fix 1b — signature takes currentDisplay explicitly; poll() passes
  // displayStatusesRef.current so we never read a stale closure value.
  const animateToTarget = useCallback(
    (targetState: string, currentDisplay: Record<NodeName, NodeStatus>) => {
      cancelAnimQueue()

      const target = deriveNodeStatuses(targetState)

      // Find which nodes actually need to change and in what order
      const toUpdate: NodeName[] = NODES
        .map(n => n.name)
        .filter(name => {
          const cur = currentDisplay[name]
          const tgt = target[name]
          return cur !== tgt && STATUS_WEIGHT[tgt] >= STATUS_WEIGHT[cur]
        })
        // sort by rank so Order lights before Inventory, etc.
        .sort((a, b) => NODE_RANK[a] - NODE_RANK[b])

      if (toUpdate.length === 0) return

      // Also include nodes that stay as 'done' but need to reflect failed status update
      const failUpdates: NodeName[] = NODES
        .map(n => n.name)
        .filter(name => !toUpdate.includes(name) && target[name] === 'failed' && currentDisplay[name] !== 'failed')

      const allUpdates = [
        ...toUpdate,
        ...failUpdates.sort((a, b) => NODE_RANK[a] - NODE_RANK[b]),
      ]

      let running = { ...currentDisplay }

      // Fix 1e — stagger uses i (0-based index in sorted array) × STEP_MS
      allUpdates.forEach((name, i) => {
        const tid = setTimeout(() => {
          running = { ...running, [name]: target[name] }
          setDisplayStatuses({ ...running })
        }, i * STEP_MS)
        animQueueRef.current.push(tid)
      })
    },
    [cancelAnimQueue]
  )

  // ── Poll the saga state ──────────────────────────────────────────────────────
  useEffect(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current)
      intervalRef.current = null
    }
    cancelAnimQueue()
    setFetchError(null)

    if (!activeOrderId) {
      // Fix 1c — DO NOT wipe displayStatuses when activeOrderId goes null.
      // Keep showing the last completed mesh state until a new order begins.
      return
    }

    // New order — reset to fresh idle so we animate from scratch
    const freshIdle = deriveNodeStatuses('')
    setDisplayStatuses(freshIdle)
    displayStatusesRef.current = freshIdle
    setServerState('')
    serverStateRef.current = ''

    // Fix 1a — pre-flight guard flag: if first poll gets 404, don't start interval
    let got404 = false

    const poll = async () => {
      try {
        const res = await api.get<SagaState>(`/api/v1/Orders/${activeOrderId}/saga`)
        const state = res.data?.currentState ?? ''

        setServerState(state)
        // Fix 1b — keep ref in sync so the stale-closure comparison is correct
        const prevState = serverStateRef.current
        serverStateRef.current = state
        setFetchError(null)

        // Fix 1b — compare against ref value (not stale closure)
        if (state !== prevState) {
          animateToTarget(state, displayStatusesRef.current)
        }

        if (state === 'OrderCompleted' || state === 'OrderCancelled') {
          if (intervalRef.current) {
            clearInterval(intervalRef.current)
            intervalRef.current = null
          }
        }
      } catch (err: any) {
        if (err?.response?.status === 404) {
          // Fix 1a — saga row not in DB yet; mark so interval won't start
          got404 = true
          setFetchError(null)
        } else {
          setFetchError(err?.response?.data?.message ?? 'Failed to fetch saga state')
        }
      }
    }

    // Fix 1a — pre-flight: do one initial poll synchronously before starting
    // the interval. If the saga row doesn't exist yet (404), bail out entirely.
    const startPolling = async () => {
      await poll()
      // Abort if: order was cleared, saga hit 404, or terminal was already reached
      if (got404 || intervalRef.current !== null) return
      intervalRef.current = setInterval(poll, 600)
    }

    startPolling()

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
      cancelAnimQueue()
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeOrderId])

  return (
    <>
      {/* meshpulse keyframe — glow ring that grows outward */}
      <style>{`
        @keyframes meshpulse {
          0%, 100% { box-shadow: 0 0 0 0 rgba(52,211,153,0.4), 0 0 6px rgba(52,211,153,0.2); }
          50%       { box-shadow: 0 0 0 5px rgba(52,211,153,0),  0 0 14px rgba(52,211,153,0.35); }
        }
      `}</style>

      <div className="rounded-xl border border-border bg-card p-5">
        {/* Header */}
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-sm font-semibold text-foreground tracking-wide uppercase">
            Service Mesh
          </h3>
          {serverState && (
            <span className="text-xs px-2 py-0.5 rounded-full border border-border text-muted-foreground font-mono">
              {serverState}
            </span>
          )}
        </div>

        {fetchError && (
          <div className="mb-3 text-xs text-red-400 bg-red-500/10 border border-red-500/30 rounded-lg px-3 py-2">
            ⚠ {fetchError}
          </div>
        )}

        {!activeOrderId && (
          <div className="mb-3 text-xs text-muted-foreground italic">
            No active order — place a test order to see live saga flow.
          </div>
        )}

        {/* Nodes row */}
        <div className="flex items-center gap-0 overflow-x-auto pb-2">
          {NODES.map((node, idx) => {
            const status          = displayStatuses[node.name]
            const isPayment       = node.name === 'Payment'
            const payCircuitState = paymentCircuit?.state ?? ''

            // Connector is lit when THIS node is done and the next node is at least active
            const nextNode     = NODES[idx + 1]
            const nextStatus   = nextNode ? displayStatuses[nextNode.name] : 'idle'
            const connectorLit = status === 'done' && (nextStatus === 'active' || nextStatus === 'done')

            return (
              <React.Fragment key={node.name}>
                <div className="flex flex-col items-center gap-1.5 min-w-[80px]">
                  <div
                    id={node.id}
                    className={`relative flex flex-col items-center justify-center
                      rounded-lg border px-2 py-2 w-[76px] text-center
                      transition-all duration-500
                      ${nodeBoxClass(status)}`}
                    style={{ minHeight: '58px' }}
                  >
                    {isPayment && <CircuitBreakerOverlay state={payCircuitState} />}
                    <span className="text-[11px] font-bold leading-tight">{node.name}</span>
                    <span className="text-[9px] mt-0.5 opacity-70 leading-none">
                      {nodeStatusLabel(status)}
                    </span>
                  </div>
                </div>

                {idx < NODES.length - 1 && (
                  <Connector lit={connectorLit} />
                )}
              </React.Fragment>
            )
          })}
        </div>

        {/* Legend */}
        <div className="flex items-center gap-3 mt-4 flex-wrap">
          {(['idle', 'active', 'done', 'failed'] as NodeStatus[]).map((s) => (
            <div key={s} className="flex items-center gap-1 text-[10px]">
              <div className={`w-2.5 h-2.5 rounded-sm border ${nodeBoxClass(s)}`} />
              <span className="capitalize text-muted-foreground">{s}</span>
            </div>
          ))}
        </div>
      </div>
    </>
  )
}
