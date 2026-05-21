export interface SagaMetrics {
  activeSagas: number;
  successRate: number;
}

export interface CircuitState {
  state: 'Closed' | 'Open' | 'HalfOpen';
  failures: number;
  lastFailureAt?: string;
}

export interface SystemMetrics {
  ordersPerMinute: number;
  p95LatencyMs: number;
  cpuPercent: number;
  memoryPercent: number;
  deadLetterCount: number;
  outboxPending: number;
}
