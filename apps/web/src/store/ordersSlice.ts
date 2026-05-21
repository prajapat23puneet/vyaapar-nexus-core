import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import type { Order } from '@vyaapar-nexus/shared-types';

export interface OrdersState {
  items: Order[];
  activeOrderId: string | null;
  activeCorrelationId: string | null;
  selectedOrderId: string | null;
  trace: any | null; // Using any for trace as domain model is not fully defined in shared-types
  loading: boolean;
}

const initialState: OrdersState = {
  items: [],
  activeOrderId: null,
  activeCorrelationId: null,
  selectedOrderId: null,
  trace: null,
  loading: false,
};

export const ordersSlice = createSlice({
  name: 'orders',
  initialState,
  reducers: {
    setOrders: (state, action: PayloadAction<Order[]>) => {
      state.items = action.payload;
    },
    setActiveOrder: (state, action: PayloadAction<{ id: string | null; correlationId: string | null }>) => {
      state.activeOrderId = action.payload.id;
      state.activeCorrelationId = action.payload.correlationId;
    },
    setSelectedOrder: (state, action: PayloadAction<string | null>) => {
      state.selectedOrderId = action.payload;
    },
    setTrace: (state, action: PayloadAction<any | null>) => {
      state.trace = action.payload;
    },
    setLoading: (state, action: PayloadAction<boolean>) => {
      state.loading = action.payload;
    },
  },
});

export const { setOrders, setActiveOrder, setSelectedOrder, setTrace, setLoading } = ordersSlice.actions;
export default ordersSlice.reducer;
