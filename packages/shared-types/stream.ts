export interface LogEntry {
  timestamp: string;
  level: string;
  service: string;
  message: string;
  correlationId?: string | null;
}

export interface ActiveOrder {
  orderId: string;
  correlationId: string;
  currentState: string;
}

export interface StreamMetrics {
  activeSagas: number;
  deadLetterCount: number;
  outboxPending: number;
  ordersPerMinute: number;
  sagaSuccessRate: number;
  p95LatencyMs: number;
  cpuPercent: number;
  memoryPercent: number;
  circuitStates: Record<string, string>;
  activeOrder: ActiveOrder | null;
  recentLogs: LogEntry[];
  timestamp: string;
}

