import React from 'react'
import { useQuery } from '@tanstack/react-query'
import { useDispatch, useSelector } from 'react-redux'
import { api } from '../api/client'
import { setSelectedOrder } from '../store/ordersSlice'
import type { RootState } from '../store'
import type { OrderDetail } from '@vyaapar-nexus/shared-types'
import { 
  AlertCircle, RefreshCw, Mail, MapPin, CreditCard
} from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from './ui/sheet'
import { Badge } from './ui/badge'
import { SagaTimeline } from './SagaTimeline'

export function OrderDetailDrawer() {
  const dispatch = useDispatch()
  const selectedOrderId = useSelector((s: RootState) => s.orders.selectedOrderId)
  
  const isOpen = !!selectedOrderId

  const handleOpenChange = (open: boolean) => {
    if (!open) {
      dispatch(setSelectedOrder(null))
    }
  }

  // 1. Fetch Detailed Data for Selected Order
  const { data: orderDetails, isLoading: orderLoading, error } = useQuery<OrderDetail>({
    queryKey: ['order-details', selectedOrderId],
    queryFn: async () => {
      if (!selectedOrderId) throw new Error("No order ID")
      const res = await api.get<OrderDetail>(`/api/v1/orders/${selectedOrderId}`)
      return res.data
    },
    enabled: isOpen
  })

  const getStatusBadgeClass = (status: string) => {
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

  return (
    <Sheet open={isOpen} onOpenChange={handleOpenChange}>
      <SheetContent className="w-[400px] sm:w-[540px] sm:max-w-md overflow-y-auto" side="right">
        <SheetHeader className="mb-6 mt-4">
          <SheetTitle className="text-xl">Order Details</SheetTitle>
          <SheetDescription>
            <span className="font-mono text-xs">{selectedOrderId}</span>
          </SheetDescription>
        </SheetHeader>

        {orderLoading ? (
          <div className="flex flex-col items-center justify-center p-12 space-y-4">
            <RefreshCw className="h-8 w-8 animate-spin text-muted-foreground" />
            <span className="text-sm text-muted-foreground">Loading order details...</span>
          </div>
        ) : !orderDetails || error ? (
          <div className="flex flex-col items-center justify-center p-12 text-center space-y-3">
            <AlertCircle className="h-8 w-8 text-destructive" />
            <span className="text-sm">Could not load order details.</span>
          </div>
        ) : (
          <div className="space-y-8 pb-8">
            {/* Top Section (Order Info) */}
            <div className="p-5 rounded-xl border bg-muted/10 flex flex-col gap-5">
              <div className="flex justify-between items-start">
                 <div>
                   <p className="text-xs text-muted-foreground uppercase tracking-wider font-semibold">Status</p>
                   <Badge variant="outline" className={`mt-1.5 ${getStatusBadgeClass(orderDetails.status)}`}>
                     {orderDetails.status}
                   </Badge>
                 </div>
                 <div className="text-right">
                   <p className="text-xs text-muted-foreground uppercase tracking-wider font-semibold">Total Amount</p>
                   <p className="font-bold text-xl mt-1 text-foreground">
                     ₹{(orderDetails.totalAmount ?? 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}
                   </p>
                 </div>
              </div>
              
              <div className="grid grid-cols-2 gap-6 pt-5 border-t">
                {/* Customer Info */}
                <div className="space-y-2">
                  <h4 className="text-[10px] font-bold uppercase tracking-wider flex items-center gap-1.5 text-muted-foreground mb-3">
                    <Mail className="h-3.5 w-3.5" /> Customer Profile
                  </h4>
                  <div className="text-sm space-y-1">
                    <p className="font-medium text-foreground">{orderDetails.customer.name}</p>
                    <p className="text-muted-foreground text-xs font-mono">{orderDetails.customer.email}</p>
                    <p className="text-xs text-muted-foreground flex items-center gap-1.5 pt-2">
                      <CreditCard className="h-3.5 w-3.5" /> {orderDetails.paymentMethod}
                    </p>
                  </div>
                </div>
                {/* Shipping */}
                <div className="space-y-2">
                  <h4 className="text-[10px] font-bold uppercase tracking-wider flex items-center gap-1.5 text-muted-foreground mb-3">
                    <MapPin className="h-3.5 w-3.5" /> Shipping Address
                  </h4>
                  <div className="text-sm text-muted-foreground space-y-1">
                    <p className="text-foreground font-medium">{orderDetails.shippingAddress.line1}</p>
                    {orderDetails.shippingAddress.line2 && <p>{orderDetails.shippingAddress.line2}</p>}
                    <p>{orderDetails.shippingAddress.city}, {orderDetails.shippingAddress.state} {orderDetails.shippingAddress.pincode}</p>
                    <p className="text-xs">{orderDetails.shippingAddress.country}</p>
                  </div>
                </div>
              </div>
            </div>

            {/* Middle Section (Items) */}
            <div className="space-y-4">
              <h4 className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Order Items</h4>
              <div className="rounded-lg border overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-muted/50 text-xs">
                    <tr>
                      <th className="p-3 text-left font-medium text-muted-foreground">Product</th>
                      <th className="p-3 text-center font-medium text-muted-foreground">Qty</th>
                      <th className="p-3 text-right font-medium text-muted-foreground">Price</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {orderDetails.items.map((item, i) => (
                      <tr key={i} className="hover:bg-muted/30 transition-colors">
                        <td className="p-3">
                          <p className="font-medium text-foreground truncate max-w-[150px]" title={item.productName}>{item.productName}</p>
                          <p className="text-[10px] text-muted-foreground font-mono mt-0.5">{item.sku}</p>
                        </td>
                        <td className="p-3 text-center">{item.quantity}</td>
                        <td className="p-3 text-right font-medium">₹{(item.unitPrice ?? 0).toLocaleString('en-IN', { minimumFractionDigits: 2 })}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Bottom Section (Saga Trace) */}
            <div className="space-y-4 pt-2">
              <SagaTimeline orderId={selectedOrderId!} />
            </div>

          </div>
        )}
      </SheetContent>
    </Sheet>
  )
}
