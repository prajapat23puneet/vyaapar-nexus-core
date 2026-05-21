export type OrderStatus =
  | 'Submitted'
  | 'InventoryChecking'
  | 'InventoryReserved'
  | 'InventoryFailed'
  | 'PaymentProcessing'
  | 'PaymentProcessed'
  | 'PaymentFailed'
  | 'ShippingDispatching'
  | 'ShippingDispatched'
  | 'NotificationSending'
  | 'OrderCompleted'
  | 'OrderCancelled';

export type PaymentMethod = 'UPI' | 'Card' | 'Wallet' | 'COD';

export interface OrderItem {
  productId: string;
  quantity: number;
  unitPrice: number;
}

export interface Order {
  id: string;
  correlationId: string;
  customerName: string;
  itemCount: number;
  totalAmount: number;
  status: string;
  failureReason: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateOrderRequest {
  customerId: string;
  items: Omit<OrderItem, 'unitPrice'>[];
  paymentMethod: PaymentMethod;
}

export interface OrderCustomer {
  id: string;
  name: string;
  email: string;
}

export interface ShippingAddress {
  line1: string;
  line2?: string | null;
  city: string;
  state: string;
  pincode: string;
  country: string;
}

export interface OrderDetailItem {
  productId: string;
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface OrderDetail {
  id: string;
  correlationId: string;
  customer: OrderCustomer;
  status: string;
  paymentMethod: string;
  paymentReference?: string | null;
  shippingAddress: ShippingAddress;
  items: OrderDetailItem[];
  subtotal: number;
  taxAmount: number;
  shippingAmount: number;
  totalAmount: number;
  failureReason?: string | null;
  createdAt: string;
  updatedAt: string;
  completedAt?: string | null;
  cancelledAt?: string | null;
}

export interface SagaState {
  orderId: string;
  correlationId: string;
  currentState: string;
  inventoryReserved: boolean;
  paymentProcessed: boolean;
  shippingDispatched: boolean;
  notificationSent: boolean;
  startedAt: string;
  completedAt?: string | null;
  durationMs?: number | null;
  lastError?: string | null;
}

export interface SagaTraceEvent {
  eventType: string;
  serviceName: string;
  previousState?: string | null;
  currentState: string;
  message?: string | null;
  durationMs?: number | null;
  createdAt: string;
  metadata?: string | null;
}

export interface SagaTrace {
  orderId: string;
  correlationId: string;
  events: SagaTraceEvent[];
}
