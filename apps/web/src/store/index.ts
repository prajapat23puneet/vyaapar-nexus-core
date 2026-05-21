import { configureStore } from '@reduxjs/toolkit';
import metricsReducer from './metricsSlice';
import ordersReducer from './ordersSlice';
import uiReducer from './uiSlice';

export const store = configureStore({
  reducer: {
    metrics: metricsReducer,
    orders: ordersReducer,
    ui: uiReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
