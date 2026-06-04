import React, { useEffect, useRef, useState } from 'react'
import { useDispatch, useSelector } from 'react-redux'
import { toast } from 'sonner'
import { api } from '../api/client'
import { setActiveOrder } from '../store/ordersSlice'
import type { RootState } from '../store'
import type { SagaState } from '@vyaapar-nexus/shared-types'

type FailureMode = 'none' | 'payment' | 'inventory'

interface ButtonState {
  loading: boolean
}

const TERMINAL_STATES = new Set(['OrderCompleted', 'OrderCancelled'])

function Spinner() {
  return (
    <svg
      className="animate-spin h-3.5 w-3.5"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2.5}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M12 4v2m0 12v2M4 12H2m20 0h-2M6.34 6.34l-1.42-1.41M19.07 19.07l-1.41-1.42M6.34 17.66l-1.42 1.42M19.07 4.93l-1.41 1.42"
      />
    </svg>
  )
}

export function DemoOrderPanel() {
  const dispatch = useDispatch()
  const [buttonStates, setButtonStates] = useState<Record<FailureMode, ButtonState>>({
    none:      { loading: false },
    payment:   { loading: false },
    inventory: { loading: false },
  })

  // Fix 3a — single component-level ref for the poll interval
  const pollIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  // Fix 3b — dedup guard: never fire toast for the same orderId twice
  const notifiedOrderIds = useRef<Set<string>>(new Set())

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (pollIntervalRef.current) clearInterval(pollIntervalRef.current)
    }
  }, [])

  /**
   * Polls /saga for orderId until it reaches a terminal state, then fires
   * the appropriate toast. Maximum wait: 45 s.
   *
   * Fix 3a: cancels any previous poll before starting a new one.
   * Fix 3b: guards against duplicate toasts with notifiedOrderIds ref.
   */
  function pollSagaAndNotify(orderId: string) {
    // Cancel any previous in-flight poll
    if (pollIntervalRef.current) {
      clearInterval(pollIntervalRef.current)
      pollIntervalRef.current = null
    }

    const MAX_POLLS = 90  // 90 × 500 ms = 45 s ceiling
    let count = 0

    pollIntervalRef.current = setInterval(async () => {
      count++
      if (count > MAX_POLLS) {
        clearInterval(pollIntervalRef.current!)
        pollIntervalRef.current = null
        return
      }

      try {
        const res = await api.get<SagaState>(`/api/v1/Orders/${orderId}/saga`)
        const state = res.data?.currentState ?? ''

        if (TERMINAL_STATES.has(state)) {
          clearInterval(pollIntervalRef.current!)
          pollIntervalRef.current = null

          // Fix 3b — hard dedup guard
          if (notifiedOrderIds.current.has(orderId)) return
          notifiedOrderIds.current.add(orderId)

          if (state === 'OrderCompleted') {
            toast.success('Order completed', {
              description: `Order ${orderId.slice(0, 8)} finished successfully.`,
              duration: 5000,
            })
          } else {
            // OrderCancelled — could be payment failure, inventory failure, etc.
            const reason = res.data?.lastError ?? 'Saga reached cancelled state.'
            toast.error('Order cancelled', {
              description: reason,
              duration: 6000,
            })
          }
        }
      } catch {
        // Ignore transient errors — keep polling
      }
    }, 500)
  }

  const placeOrder = async (mode: FailureMode) => {
    setButtonStates((prev) => ({ ...prev, [mode]: { loading: true } }))

    try {
      const headers: Record<string, string> = {}
      if (mode === 'payment')   headers['X-Force-Failure'] = 'payment'
      if (mode === 'inventory') headers['X-Force-Failure'] = 'inventory'

      const response = await api.post(
        '/api/v1/Orders/demo',
        {},
        { headers }
      )

      if (response.status === 201) {
        const orderId = response.data.id

        // Fix 3d — dispatch setActiveOrder exactly as before (immediately after 201)
        dispatch(
          setActiveOrder({
            id: orderId,
            correlationId: response.data.correlationId,
          })
        )

        // Start polling for the saga terminal state — toast fires only at the end
        pollSagaAndNotify(orderId)
      }
    } catch (err: any) {
      const msg =
        err?.response?.data?.message ??
        err?.message ??
        'Failed to submit order'
      toast.error('Order submission failed', { description: msg, duration: 5000 })
    } finally {
      setButtonStates((prev) => ({ ...prev, [mode]: { loading: false } }))
    }
  }

  // Fix 3c — disable ALL buttons while any single order is in-flight
  const anyLoading =
    buttonStates.none.loading ||
    buttonStates.payment.loading ||
    buttonStates.inventory.loading

  return (
    <div className="rounded-xl border border-border bg-card p-5 flex flex-col gap-4">
      <div>
        <h3 className="text-sm font-semibold text-foreground tracking-wide uppercase mb-1">
          Demo Control Panel
        </h3>
        <p className="text-xs text-muted-foreground">
          Trigger test orders to observe real-time saga orchestration.
        </p>
      </div>

      <div className="flex flex-col gap-2.5">
        {/* Button 1: Normal test order */}
        <button
          id="btn-place-test-order"
          disabled={anyLoading}
          onClick={() => placeOrder('none')}
          className="flex items-center justify-center gap-2 w-full px-4 py-2.5 rounded-lg text-sm font-semibold transition-all duration-200 border border-zinc-400/30 bg-zinc-400/10 text-zinc-100 hover:bg-zinc-400/20 hover:border-zinc-400/50 disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {buttonStates.none.loading ? (
            <>
              <Spinner />
              Placing…
            </>
          ) : (
            <>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className="w-4 h-4">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
              </svg>
              Place Test Order
            </>
          )}
        </button>

        {/* Button 2: Force payment failure */}
        <button
          id="btn-force-payment-failure"
          disabled={anyLoading}
          onClick={() => placeOrder('payment')}
          className="flex items-center justify-center gap-2 w-full px-4 py-2.5 rounded-lg text-sm font-semibold transition-all duration-200 border border-red-500/50 bg-red-500/10 text-red-600 dark:text-red-400 hover:bg-red-500/20 disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {buttonStates.payment.loading ? (
            <>
              <Spinner />
              Sending…
            </>
          ) : (
            <>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className="w-4 h-4">
                <path strokeLinecap="round" strokeLinejoin="round" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
              </svg>
              Force Payment Failure
            </>
          )}
        </button>

        {/* Button 3: Force inventory failure */}
        <button
          id="btn-force-inventory-failure"
          disabled={anyLoading}
          onClick={() => placeOrder('inventory')}
          className="flex items-center justify-center gap-2 w-full px-4 py-2.5 rounded-lg text-sm font-semibold transition-all duration-200 border border-orange-500/50 bg-orange-500/10 text-orange-600 dark:text-orange-400 hover:bg-orange-500/20 disabled:opacity-60 disabled:cursor-not-allowed"
        >
          {buttonStates.inventory.loading ? (
            <>
              <Spinner />
              Sending…
            </>
          ) : (
            <>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={2} className="w-4 h-4">
                <path strokeLinecap="round" strokeLinejoin="round" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
              </svg>
              Force Inventory Failure
            </>
          )}
        </button>
      </div>

      {/* Info callout */}
      <div className="text-[10px] text-muted-foreground border-t border-border pt-3 leading-relaxed">
        Failure buttons attach <code className="text-xs font-mono">X-Force-Failure</code> header to the demo order request, triggering compensating transactions and dead letter routing.
      </div>
    </div>
  )
}
