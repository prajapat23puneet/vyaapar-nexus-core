import React, { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useDispatch, useSelector } from 'react-redux'
import { api } from '../api/client'
import type { RootState } from '../store'
import { setSelectedOrder } from '../store/ordersSlice'
import { setOrderDetailsDrawerOpen } from '../store/uiSlice'
import type { PagedResult, Order, OrderDetail, SagaTrace, SagaState } from '@vyaapar-nexus/shared-types'
import { 
  ShoppingCart, AlertCircle, RefreshCw, X, Mail, MapPin, 
  Clock, CreditCard, ChevronLeft, ChevronRight, Activity, Tag, ListOrdered
} from 'lucide-react'

export function Orders() {
  const dispatch = useDispatch()
  const [page, setPage] = useState(1)
  
  const selectedOrderId = useSelector((s: RootState) => s.orders.selectedOrderId)
  const orderDetailsDrawerOpen = useSelector((s: RootState) => s.ui.orderDetailsDrawerOpen)

  // 1. Fetch Orders List (Paginated)
  const { data, isLoading, error, refetch, isFetching } = useQuery<PagedResult<Order>>({
    queryKey: ['orders', page],
    queryFn: async () => {
      const res = await api.get<PagedResult<Order>>(`/api/v1/orders?page=${page}&size=10`)
      return res.data
    }
  })

  // 2. Fetch Detailed Data for Selected Order
  const { data: orderDetails, isLoading: orderLoading } = useQuery<OrderDetail>({
    queryKey: ['order-details', selectedOrderId],
    queryFn: async () => {
      const res = await api.get<OrderDetail>(`/api/v1/orders/${selectedOrderId}`)
      return res.data
    },
    enabled: !!selectedOrderId && orderDetailsDrawerOpen
  })

  const { data: orderTrace, isLoading: traceLoading } = useQuery<SagaTrace>({
    queryKey: ['order-trace', selectedOrderId],
    queryFn: async () => {
      const res = await api.get<SagaTrace>(`/api/v1/orders/${selectedOrderId}/trace`)
      return res.data
    },
    enabled: !!selectedOrderId && orderDetailsDrawerOpen
  })

  const { data: sagaState } = useQuery<SagaState>({
    queryKey: ['order-saga', selectedOrderId],
    queryFn: async () => {
      const res = await api.get<SagaState>(`/api/v1/orders/${selectedOrderId}/saga`)
      return res.data
    },
    enabled: !!selectedOrderId && orderDetailsDrawerOpen
  })

  const orders = data?.items && Array.isArray(data.items) ? data.items : []

  function getStatusBadgeClass(status: string) {
    switch (status) {
      case 'OrderCompleted':
        return 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
      case 'OrderCancelled':
      case 'PaymentFailed':
      case 'InventoryFailed':
        return 'bg-red-500/10 text-red-400 border border-red-500/20'
      case 'InventoryReserved':
      case 'PaymentProcessed':
      case 'ShippingDispatched':
        return 'bg-blue-500/10 text-blue-400 border border-blue-500/20'
      default:
        return 'bg-amber-500/10 text-amber-400 border border-amber-500/20'
    }
  }

  function getEventColorClass(eventType: string) {
    if (eventType.includes('Completed') || eventType.includes('Reserved') || eventType.includes('Processed') || eventType.includes('Dispatched') || eventType.includes('Sent')) {
      return 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
    }
    if (eventType.includes('Failed') || eventType.includes('Cancelled')) {
      return 'bg-red-500/20 text-red-400 border border-red-500/30'
    }
    if (eventType.includes('Processing') || eventType.includes('Checking') || eventType.includes('Dispatching') || eventType.includes('Sending')) {
      return 'bg-amber-500/20 text-amber-400 border border-amber-500/30 animate-pulse'
    }
    return 'bg-blue-500/20 text-blue-400 border border-blue-500/30'
  }

  const handleRowClick = (orderId: string) => {
    dispatch(setSelectedOrder(orderId))
    dispatch(setOrderDetailsDrawerOpen(true))
  }

  return (
    <div className="space-y-6 relative">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-[var(--border)] pb-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-[var(--text-h)] my-1 flex items-center gap-2">
            <ShoppingCart className="h-8 w-8 text-[var(--accent)]" />
            Orders Management
          </h1>
          <p className="text-sm text-[var(--text)]">
            Monitor incoming order requests, process status changes, and track active saga workflows.
          </p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isLoading || isFetching}
          className="flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md border border-[var(--border)] bg-[var(--social-bg)] text-[var(--text-h)] hover:bg-[var(--border)] active:scale-95 transition-all duration-200 disabled:opacity-50"
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
        <div className="flex flex-col items-center justify-center p-12 rounded-xl border border-[var(--border)] bg-[var(--social-bg)] text-center max-w-xl mx-auto space-y-4">
          <div className="p-3 bg-[var(--accent-bg)] rounded-full text-[var(--accent)]">
            <ShoppingCart className="h-8 w-8" />
          </div>
          <h2 className="text-xl font-semibold text-[var(--text-h)]">No orders found</h2>
          <p className="text-sm text-[var(--text)]">
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
                        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${getStatusBadgeClass(order.status)}`}>
                          {order.status}
                        </span>
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

      {/* Drawer Overlay */}
      {orderDetailsDrawerOpen && (
        <div 
          className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm transition-opacity duration-300"
          onClick={() => {
            dispatch(setSelectedOrder(null))
            dispatch(setOrderDetailsDrawerOpen(false))
          }}
        />
      )}

      {/* Drawer Content */}
      <div 
        className={`fixed inset-y-0 right-0 z-50 w-full max-w-2xl bg-[var(--bg)] border-l border-[var(--border)] shadow-2xl transition-transform duration-300 transform flex flex-col ${
          orderDetailsDrawerOpen ? 'translate-x-0' : 'translate-x-full'
        }`}
      >
        <div className="flex items-center justify-between p-6 border-b border-[var(--border)] bg-[var(--social-bg)]/20">
          <div>
            <h2 className="text-xl font-bold text-[var(--text-h)] flex items-center gap-2">
              <ListOrdered className="h-5 w-5 text-[var(--accent)]" />
              Saga Audit Details
            </h2>
            <p className="text-xs text-[var(--text)] font-mono mt-1 select-all">{selectedOrderId}</p>
          </div>
          <button 
            onClick={() => {
              dispatch(setSelectedOrder(null))
              dispatch(setOrderDetailsDrawerOpen(false))
            }}
            className="p-2 rounded-md hover:bg-[var(--social-bg)] text-[var(--text)] hover:text-[var(--text-h)] transition-all active:scale-90"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {orderLoading ? (
          <div className="flex-grow flex flex-col items-center justify-center p-8 space-y-4 animate-pulse">
            <RefreshCw className="h-8 w-8 animate-spin text-[var(--accent)]" />
            <span className="text-sm text-[var(--text)]">Loading order details & trace...</span>
          </div>
        ) : !orderDetails ? (
          <div className="flex-grow flex flex-col items-center justify-center p-8 text-center space-y-3">
            <AlertCircle className="h-8 w-8 text-red-500" />
            <span className="text-sm text-[var(--text)]">Could not load order trace details.</span>
          </div>
        ) : (
          <div className="flex-grow overflow-y-auto p-6 space-y-6">
            
            {/* Status Panel */}
            <div className="p-4 rounded-xl border border-[var(--border)] bg-[var(--social-bg)]/10 flex items-center justify-between">
              <div>
                <span className="text-xs text-[var(--text)]">Current State</span>
                <div className="mt-1 flex items-center gap-2">
                  <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${getStatusBadgeClass(orderDetails.status)}`}>
                    {orderDetails.status}
                  </span>
                </div>
              </div>
              <div className="text-right">
                <span className="text-xs text-[var(--text)]">Total Amount</span>
                <h3 className="text-lg font-bold text-[var(--text-h)] mt-0.5">
                  ₹{(orderDetails.totalAmount ?? 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}
                </h3>
              </div>
            </div>

            {/* Grid details */}
            <div className="grid grid-cols-2 gap-6">
              {/* Customer Column */}
              <div className="space-y-3">
                <h4 className="text-xs font-bold uppercase tracking-wider text-[var(--text-h)] flex items-center gap-1.5 border-b border-[var(--border)] pb-1.5">
                  <Mail className="h-3.5 w-3.5 text-[var(--accent)]" />
                  Customer Profile
                </h4>
                <div className="text-sm space-y-1">
                  <p className="font-medium text-[var(--text-h)]">{orderDetails.customer.name}</p>
                  <p className="text-[var(--text)] text-xs font-mono">{orderDetails.customer.email}</p>
                  <p className="text-xs text-[var(--text)] flex items-center gap-1.5 mt-2">
                    <CreditCard className="h-3.5 w-3.5" />
                    <span>Payment Method: {orderDetails.paymentMethod}</span>
                  </p>
                  {orderDetails.paymentReference && (
                    <p className="text-[10px] font-mono text-[var(--text)]">
                      Ref: {orderDetails.paymentReference}
                    </p>
                  )}
                </div>
              </div>

              {/* Shipping Address Column */}
              <div className="space-y-3">
                <h4 className="text-xs font-bold uppercase tracking-wider text-[var(--text-h)] flex items-center gap-1.5 border-b border-[var(--border)] pb-1.5">
                  <MapPin className="h-3.5 w-3.5 text-emerald-400" />
                  Shipping Address
                </h4>
                <div className="text-sm text-[var(--text)] space-y-1">
                  <p className="text-[var(--text-h)]">{orderDetails.shippingAddress.line1}</p>
                  {orderDetails.shippingAddress.line2 && <p>{orderDetails.shippingAddress.line2}</p>}
                  <p>
                    {orderDetails.shippingAddress.city}, {orderDetails.shippingAddress.state} - {orderDetails.shippingAddress.pincode}
                  </p>
                  <p className="text-xs font-medium text-[var(--text-h)]">{orderDetails.shippingAddress.country}</p>
                </div>
              </div>
            </div>

            {/* Saga Timeline Trace */}
            <div className="space-y-4">
              <h4 className="text-xs font-bold uppercase tracking-wider text-[var(--text-h)] flex items-center gap-1.5 border-b border-[var(--border)] pb-1.5">
                <Activity className="h-3.5 w-3.5 text-sky-400 animate-pulse" />
                Saga Execution Log Timeline
              </h4>
              
              {traceLoading ? (
                <div className="py-4 text-xs text-[var(--text)] flex items-center gap-2">
                  <RefreshCw className="h-3.5 w-3.5 animate-spin" /> Retrieving events...
                </div>
              ) : !orderTrace || !orderTrace.events || orderTrace.events.length === 0 ? (
                <p className="text-xs text-[var(--text)] italic py-2">No saga trace events available.</p>
              ) : (
                <div className="relative border-l-2 border-[var(--border)] ml-3 pl-6 space-y-6 my-2">
                  {orderTrace.events.map((event, index) => {
                    const eventDate = event.createdAt ? new Date(event.createdAt) : null
                    return (
                      <div key={index} className="relative">
                        {/* Dot indicator */}
                        <span className={`absolute -left-[35px] top-0.5 flex h-6 w-6 items-center justify-center rounded-full border bg-[var(--bg)] text-[10px] ${getEventColorClass(event.eventType)}`}>
                          {index + 1}
                        </span>
                        
                        <div className="space-y-1 text-left">
                          <div className="flex items-center justify-between">
                            <h5 className="font-semibold text-sm text-[var(--text-h)]">{event.eventType}</h5>
                            {event.durationMs !== null && event.durationMs !== undefined && (
                              <span className="text-[10px] px-1.5 py-0.5 rounded bg-[var(--social-bg)] font-mono text-[var(--text)]">
                                {event.durationMs}ms
                              </span>
                            )}
                          </div>
                          <p className="text-xs text-[var(--text)]">{event.message || event.currentState}</p>
                          <div className="flex items-center gap-2 text-[10px] text-[var(--text)]/80">
                            <span>Service: <strong className="text-[var(--text-h)]">{event.serviceName}</strong></span>
                            <span>•</span>
                            <span>{eventDate ? eventDate.toLocaleTimeString() : '-'}</span>
                          </div>
                        </div>
                      </div>
                    )
                  })}
                </div>
              )}
            </div>

            {/* Order Items Table */}
            <div className="space-y-3">
              <h4 className="text-xs font-bold uppercase tracking-wider text-[var(--text-h)] flex items-center gap-1.5 border-b border-[var(--border)] pb-1.5">
                <Tag className="h-3.5 w-3.5 text-amber-400" />
                Line Items
              </h4>
              <div className="rounded-lg border border-[var(--border)] bg-[var(--bg)] overflow-hidden">
                <table className="w-full text-left border-collapse text-xs">
                  <thead>
                    <tr className="border-b border-[var(--border)] bg-[var(--social-bg)]/20 font-semibold text-[var(--text-h)]">
                      <th className="p-3">Product</th>
                      <th className="p-3">SKU</th>
                      <th className="p-3 text-right">Price</th>
                      <th className="p-3 text-center">Qty</th>
                      <th className="p-3 text-right">Total</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[var(--border)] text-[var(--text)]">
                    {orderDetails.items.map((item, i) => (
                      <tr key={i} className="hover:bg-[var(--social-bg)]/5">
                        <td className="p-3 font-medium text-[var(--text-h)] max-w-[160px] truncate" title={item.productName}>
                          {item.productName}
                        </td>
                        <td className="p-3 font-mono">{item.sku}</td>
                        <td className="p-3 text-right">₹{(item.unitPrice ?? 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</td>
                        <td className="p-3 text-center">{item.quantity}</td>
                        <td className="p-3 text-right font-semibold text-[var(--text-h)]">
                          ₹{(item.lineTotal ?? 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Calculations Breakdown */}
            <div className="border-t border-[var(--border)] pt-4 space-y-2 text-xs max-w-xs ml-auto">
              <div className="flex justify-between text-[var(--text)]">
                <span>Subtotal</span>
                <span>₹{(orderDetails.subtotal ?? 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
              </div>
              <div className="flex justify-between text-[var(--text)]">
                <span>Tax Amount</span>
                <span>₹{(orderDetails.taxAmount ?? 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
              </div>
              <div className="flex justify-between text-[var(--text)]">
                <span>Shipping Amount</span>
                <span>₹{(orderDetails.shippingAmount ?? 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
              </div>
              <div className="flex justify-between font-bold text-sm text-[var(--text-h)] border-t border-[var(--border)]/50 pt-2">
                <span>Grand Total</span>
                <span>₹{(orderDetails.totalAmount ?? 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</span>
              </div>
            </div>

            {/* Failure Alert (if cancelled/failed) */}
            {orderDetails.failureReason && (
              <div className="p-4 rounded-xl border border-red-500/20 bg-red-500/5 text-xs text-red-400 space-y-1">
                <h5 className="font-bold flex items-center gap-1.5">
                  <AlertCircle className="h-4 w-4" />
                  Saga Compensated / Order Cancelled
                </h5>
                <p>{orderDetails.failureReason}</p>
              </div>
            )}
            
          </div>
        )}
      </div>
    </div>
  )
}
