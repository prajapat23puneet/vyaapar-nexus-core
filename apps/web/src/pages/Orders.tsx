import React, { useState } from 'react'
import { Helmet } from 'react-helmet-async'
import { useQuery } from '@tanstack/react-query'
import { useDispatch } from 'react-redux'
import { api } from '../api/client'
import { setSelectedOrder } from '../store/ordersSlice'
import type { PagedResult, Order } from '@vyaapar-nexus/shared-types'
import { 
  ShoppingCart, AlertCircle, RefreshCw, ChevronLeft, ChevronRight
} from 'lucide-react'
import { OrderDetailDrawer } from '../components/OrderDetailDrawer'
import { Badge } from '../components/ui/badge'

export function Orders() {
  const dispatch = useDispatch()
  const [page, setPage] = useState(1)

  // 1. Fetch Orders List (Paginated)
  const { data, isLoading, error, refetch, isFetching } = useQuery<PagedResult<Order>>({
    queryKey: ['orders', page],
    queryFn: async () => {
      const res = await api.get<PagedResult<Order>>(`/api/v1/orders?page=${page}&size=10`)
      return res.data
    },
    refetchInterval: 10000
  })

  const orders = data?.items && Array.isArray(data.items) ? data.items : []

  function getStatusBadgeClass(status: string) {
    switch (status) {
      case 'OrderCompleted':
        return 'bg-emerald-500 hover:bg-emerald-600 text-white border-transparent'
      case 'OrderCancelled':
      case 'PaymentFailed':
      case 'InventoryFailed':
        return 'bg-destructive hover:bg-destructive/90 text-destructive-foreground border-transparent'
      case 'InventoryReserved':
      case 'PaymentProcessed':
      case 'ShippingDispatched':
      case 'Processing':
        return 'bg-blue-500/20 text-blue-700 dark:text-blue-400 hover:bg-blue-500/30 border-transparent'
      case 'Submitted':
      default:
        return 'bg-amber-500/20 text-amber-700 dark:text-amber-400 hover:bg-amber-500/30 border-transparent'
    }
  }

  const handleRowClick = (orderId: string) => {
    dispatch(setSelectedOrder(orderId))
  }

  return (
    <div className="space-y-6 relative">
      <Helmet>
        <title>Orders | VyaaparNexus</title>
        <meta name="description" content="Manage and view live order statuses." />
      </Helmet>
      {/* Header */}
      <div className="flex items-center justify-between border-b border-border pb-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-foreground my-1 flex items-center gap-2">
            <ShoppingCart className="h-8 w-8 text-muted-foreground" />
            Orders Management
          </h1>
          <p className="text-sm text-muted-foreground">
            Monitor incoming order requests, process status changes, and track active saga workflows.
          </p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isLoading || isFetching}
          className="flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md border border-border bg-muted/50 text-foreground hover:bg-muted active:scale-95 transition-all duration-200 disabled:opacity-50"
        >
          <RefreshCw className={`h-4 w-4 ${isFetching ? 'animate-spin' : ''}`} />
          Refresh
        </button>
      </div>

      {/* Main Grid Table */}
      {isLoading ? (
        <div className="rounded-xl border border-[var(--border)] bg-[var(--bg)] overflow-hidden">
          <div className="p-4 border-b border-[var(--border)] bg-[var(--social-bg)]/20 animate-pulse">
            <div className="h-6 bg-[var(--border)] rounded w-1/4"></div>
          </div>
          <div className="divide-y divide-[var(--border)] animate-pulse">
            {[...Array(6)].map((_, i) => (
              <div key={i} className="p-6 flex items-center justify-between gap-4">
                <div className="h-5 bg-[var(--border)] rounded w-1/4"></div>
                <div className="h-5 bg-[var(--border)] rounded w-1/3"></div>
                <div className="h-5 bg-[var(--border)] rounded w-12"></div>
                <div className="h-5 bg-[var(--border)] rounded w-16"></div>
              </div>
            ))}
          </div>
        </div>
      ) : error ? (
        <div className="flex flex-col items-center justify-center p-12 rounded-xl border border-red-500/20 bg-red-500/5 text-center max-w-xl mx-auto space-y-4">
          <div className="p-3 bg-red-500/10 rounded-full text-red-500">
            <AlertCircle className="h-8 w-8" />
          </div>
          <h2 className="text-xl font-semibold text-red-500">Failed to load orders</h2>
          <p className="text-sm text-[var(--text)]">
            Could not fetch orders from the server. Ensure the backend order service is online.
          </p>
          <button
            onClick={() => refetch()}
            className="px-4 py-2 text-sm font-medium rounded-md bg-red-500 text-white hover:bg-red-600 transition-colors"
          >
            Retry Connection
          </button>
        </div>
      ) : orders.length === 0 ? (
        <div className="flex flex-col items-center justify-center p-12 rounded-xl border border-border bg-muted/30 text-center max-w-xl mx-auto space-y-4">
          <div className="p-3 bg-muted rounded-full text-muted-foreground">
            <ShoppingCart className="h-8 w-8" />
          </div>
          <h2 className="text-xl font-semibold text-foreground">No orders found</h2>
          <p className="text-sm text-muted-foreground">
            There are no orders submitted yet. Try placing a test order from the Dashboard.
          </p>
        </div>
      ) : (
        <div className="rounded-xl border border-[var(--border)] bg-[var(--bg)] shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-sm">
              <thead>
                <tr className="border-b border-[var(--border)] bg-[var(--social-bg)]/20 text-[var(--text-h)] font-semibold">
                  <th className="p-4">Order ID</th>
                  <th className="p-4">Customer</th>
                  <th className="p-4">Items</th>
                  <th className="p-4">Total</th>
                  <th className="p-4">Status</th>
                  <th className="p-4 text-right">Time</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--border)] text-[var(--text)]">
                {orders.map((order) => {
                  if (!order || !order.id) return null
                  const total = order.totalAmount ?? 0
                  const formattedTotal = typeof total === 'number'
                    ? total.toLocaleString('en-IN', { minimumFractionDigits: 2 })
                    : parseFloat(String(total)).toLocaleString('en-IN', { minimumFractionDigits: 2 })
                  
                  return (
                    <tr 
                      key={order.id} 
                      onClick={() => handleRowClick(order.id)}
                      className="hover:bg-[var(--social-bg)]/10 cursor-pointer transition-colors duration-150"
                    >
                      <td className="p-4 font-mono font-bold text-xs text-[var(--text-h)]">
                        {order.id.slice(0, 8)}...
                      </td>
                      <td className="p-4 text-[var(--text-h)]">
                        {order.customerName || 'Standard Customer'}
                      </td>
                      <td className="p-4">
                        {order.itemCount ?? 0}
                      </td>
                      <td className="p-4 font-semibold text-[var(--text-h)]">
                        ₹{formattedTotal}
                      </td>
                      <td className="p-4">
                        <Badge variant="outline" className={getStatusBadgeClass(order.status)}>
                          {order.status}
                        </Badge>
                      </td>
                      <td className="p-4 text-right text-xs">
                        {order.createdAt 
                          ? new Date(order.createdAt).toLocaleTimeString(undefined, {
                              hour: '2-digit',
                              minute: '2-digit',
                              second: '2-digit'
                            })
                          : '-'
                        }
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          <div className="p-4 border-t border-[var(--border)] bg-[var(--social-bg)]/10 flex items-center justify-between text-xs">
            <span>Showing {orders.length} orders this page</span>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setPage((p) => Math.max(p - 1, 1))}
                disabled={page === 1}
                className="p-1.5 rounded border border-[var(--border)] bg-[var(--bg)] hover:bg-[var(--social-bg)] disabled:opacity-40 transition-colors"
              >
                <ChevronLeft className="h-4 w-4" />
              </button>
              <span>Page {data?.page ?? 1} of {data?.totalPages ?? 1}</span>
              <button
                onClick={() => setPage((p) => Math.min(p + 1, data?.totalPages ?? 1))}
                disabled={page === (data?.totalPages ?? 1)}
                className="p-1.5 rounded border border-[var(--border)] bg-[var(--bg)] hover:bg-[var(--social-bg)] disabled:opacity-40 transition-colors"
              >
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Drawer Overlay & Content via Component */}
      <OrderDetailDrawer />
    </div>
  )
}
