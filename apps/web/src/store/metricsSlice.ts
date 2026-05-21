import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import type { LogEntry, StreamMetrics, CircuitState } from '@vyaapar-nexus/shared-types';

export interface MetricsState {
  connected: boolean;
  activeSagas: number;
  deadLetterCount: number;
  outboxPending: number;
  ordersPerMinute: number;
  sagaSuccessRate: number;
  p95LatencyMs: number;
  cpuPercent: number;
  memoryPercent: number;
  circuitStates: Record<string, CircuitState>;
  recentLogs: LogEntry[];
}

const initialState: MetricsState = {
  connected: false,
  activeSagas: 0,
  deadLetterCount: 0,
  outboxPending: 0,
  ordersPerMinute: 0,
  sagaSuccessRate: 100,
  p95LatencyMs: 0,
  cpuPercent: 0,
  memoryPercent: 0,
  circuitStates: {},
  recentLogs: [],
};

export const metricsSlice = createSlice({
  name: 'metrics',
  initialState,
  reducers: {
    setConnected: (state, action: PayloadAction<boolean>) => {
      state.connected = action.payload;
    },
    metricsUpdated: (state, action: PayloadAction<StreamMetrics>) => {
      const payload = action.payload;
      state.activeSagas = payload.activeSagas;
      state.sagaSuccessRate = payload.sagaSuccessRate * 100;
      state.ordersPerMinute = payload.ordersPerMinute;
      state.p95LatencyMs = payload.p95LatencyMs;
      state.cpuPercent = payload.cpuPercent;
      state.memoryPercent = payload.memoryPercent;
      state.deadLetterCount = payload.deadLetterCount;
      state.outboxPending = payload.outboxPending;
      
      // Map flat record of service breaker states to UI CircuitState structure
      state.circuitStates = Object.entries(payload.circuitStates || {}).reduce((acc, [key, value]) => {
        acc[key] = {
          state: value as 'Closed' | 'Open' | 'HalfOpen',
          failures: 0,
        };
        return acc;
      }, {} as Record<string, CircuitState>);

      // Prepend recent logs, keeping max 100
      state.recentLogs = [...(payload.recentLogs || []), ...state.recentLogs].slice(0, 100);
    },
  },
});

export const { setConnected, metricsUpdated } = metricsSlice.actions;
export default metricsSlice.reducer;
