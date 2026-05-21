export interface Customer {
  id: string;
  name: string;
  email: string;
  phone: string;
  city: string;
  state: string;
  pincode: string;
  country: string;
  createdAt: string;
  updatedAt: string;
}

export interface CustomerDetail extends Customer {
  addressLine1: string;
  addressLine2?: string | null;
  recentOrders: RecentOrder[];
}

export interface RecentOrder {
  id: string;
  status: string;
  totalAmount: number;
  createdAt: string;
}

