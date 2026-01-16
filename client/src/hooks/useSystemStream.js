import { useState, useEffect, useCallback, useMemo } from 'react';

// JSDoc Definitions for Types
/**
 * @typedef {Object} MetricPacket
 * @property {string} Service
 * @property {number} Cpu
 * @property {number} Memory
 * @property {string} Status 'UP' | 'DOWN' | 'DEGRADED'
 */

/**
 * @typedef {Object} LogEntry
 * @property {string} Timestamp
 * @property {string} Level 'INFO' | 'WARN' | 'ERROR'
 * @property {string} Message
 * @property {string} CorrelationId
 */

/**
 * @typedef {Object} SystemState
 * @property {object} Metrics - Global metrics
 * @property {number} Metrics.OrdersPerSec
 * @property {number} Metrics.ActiveSagas
 * @property {number} Metrics.DeadLetters
 * @property {MetricPacket[]} Services - List of services
 * @property {LogEntry[]} Logs
 */

/**
 * Hook to stream system state with Chaos capability.
 * @returns {{ state: SystemState, actions: { simulateChaos: () => void } }}
 */
export const useSystemStream = () => {
    const [isChaos, setIsChaos] = useState(false);
    const [systemState, setSystemState] = useState({
        Metrics: { OrdersPerSec: 0, ActiveSagas: 0, DeadLetters: 0 },
        Services: [
            { Id: 'order-service', Service: 'Order Service', Status: 'UP', Cpu: 12, Memory: 256 },
            { Id: 'payment-service', Service: 'Payment Service', Status: 'UP', Cpu: 45, Memory: 512 },
            { Id: 'inventory-service', Service: 'Inventory Service', Status: 'UP', Cpu: 30, Memory: 300 },
            { Id: 'shipping-service', Service: 'Shipping Service', Status: 'UP', Cpu: 55, Memory: 420 },
            { Id: 'notification-service', Service: 'Notification Service', Status: 'UP', Cpu: 10, Memory: 128 },
        ],
        Logs: []
    });

    // Historical data for trend calculation and sparklines
    const [metricHistory, setMetricHistory] = useState({
        OrdersPerSec: [],
        ActiveSagas: [],
        DeadLetters: []
    });

    const simulateChaos = useCallback(() => {
        setIsChaos(true);
        setTimeout(() => setIsChaos(false), 5000); // 5 Seconds of Chaos
    }, []);

    // Helper function to calculate trend and percentage change
    const calculateTrend = (current, history, windowSize = 5) => {
        if (history.length < windowSize) return { direction: 'stable', percentage: 0 };
        
        const recent = history.slice(-windowSize);
        const avgRecent = recent.reduce((a, b) => a + b, 0) / recent.length;
        
        if (avgRecent === 0) return { direction: 'stable', percentage: 0 };
        
        const percentage = ((current - avgRecent) / avgRecent) * 100;
        const threshold = 0.5; // 0.5% threshold for stable
        
        let direction = 'stable';
        if (percentage > threshold) direction = 'up';
        else if (percentage < -threshold) direction = 'down';
        
        return { direction, percentage };
    };

    useEffect(() => {
        const interval = setInterval(() => {
            setSystemState(prev => {
                // CHAOS MODIFIERS
                const cpuBase = isChaos ? 80 : 0;
                const errorProb = isChaos ? 0.8 : 0.05;

                // 1. Simulate Metrics (with decimal support for OrdersPerSec)
                const newOrders = Math.max(0, prev.Metrics.OrdersPerSec + (Math.random() - 0.5) * (isChaos ? 100 : 20));
                const newSagas = Math.max(0, prev.Metrics.ActiveSagas + (Math.random() - 0.5) * 5);
                const newDeadLetters = prev.Metrics.DeadLetters + (Math.random() > (isChaos ? 0.2 : 0.95) ? 1 : 0);

                // 2. Simulate Service Health
                const updatedServices = prev.Services.map(svc => {
                    const cpuChange = (Math.random() - 0.5) * 10;
                    let newCpu = Math.min(100, Math.max(0, svc.Cpu + cpuChange + (isChaos ? 10 : 0)));
                    if (isChaos && Math.random() > 0.5) newCpu = 99; // Spike

                    let newStatus = newCpu > 90 ? 'DEGRADED' : 'UP';
                    if (Math.random() > 0.995 || (isChaos && Math.random() > 0.7)) newStatus = 'DOWN';

                    return {
                        ...svc,
                        Cpu: newCpu,
                        Status: newStatus
                    };
                });

                // 3. Simulate Logs
                let newLogs = [...prev.Logs];
                if (Math.random() > 0.6 || isChaos) {
                    const servicesList = ['Order', 'Payment', 'Inventory', 'Shipping'];
                    const levels = isChaos
                        ? ['ERROR', 'ERROR', 'WARN', 'CRITICAL']
                        : ['INFO', 'INFO', 'INFO', 'WARN', 'ERROR'];

                    const randomService = servicesList[Math.floor(Math.random() * servicesList.length)];
                    const randomLevel = levels[Math.floor(Math.random() * levels.length)];

                    let msg = `[${randomService}] Processed batch ${Math.floor(Math.random() * 9999)}`;
                    if (randomLevel === 'ERROR') msg = `[${randomService}] Connection timeout to DB shard 0${Math.floor(Math.random() * 4)}`;
                    if (isChaos) msg = `[${randomService}] CRITICAL FAILURE: CIRCUIT BREAKER OPEN`;

                    const logMsg = {
                        Timestamp: new Date().toLocaleTimeString(),
                        Level: randomLevel,
                        Message: msg,
                        CorrelationId: crypto.randomUUID()
                    };

                    newLogs = [logMsg, ...prev.Logs].slice(0, 100);
                }

                const ordersValue = Math.max(0, Number(newOrders.toFixed(1)));
                const sagasValue = Math.max(0, Math.floor(newSagas));
                const deadLettersValue = newDeadLetters;

                // Update history (keep last 20 values)
                setMetricHistory(prevHistory => ({
                    OrdersPerSec: [...prevHistory.OrdersPerSec.slice(-19), ordersValue],
                    ActiveSagas: [...prevHistory.ActiveSagas.slice(-19), sagasValue],
                    DeadLetters: [...prevHistory.DeadLetters.slice(-19), deadLettersValue]
                }));

                return {
                    Metrics: {
                        OrdersPerSec: ordersValue,
                        ActiveSagas: sagasValue,
                        DeadLetters: deadLettersValue
                    },
                    Services: updatedServices,
                    Logs: newLogs
                };
            });
        }, 1000);

        return () => clearInterval(interval);
    }, [isChaos]);

    // Calculate trends for each metric (memoized for performance)
    const ordersTrend = useMemo(
        () => calculateTrend(systemState.Metrics.OrdersPerSec, metricHistory.OrdersPerSec),
        [systemState.Metrics.OrdersPerSec, metricHistory.OrdersPerSec]
    );
    const sagasTrend = useMemo(
        () => calculateTrend(systemState.Metrics.ActiveSagas, metricHistory.ActiveSagas),
        [systemState.Metrics.ActiveSagas, metricHistory.ActiveSagas]
    );
    const deadLettersTrend = useMemo(
        () => calculateTrend(systemState.Metrics.DeadLetters, metricHistory.DeadLetters),
        [systemState.Metrics.DeadLetters, metricHistory.DeadLetters]
    );

    return { 
        state: systemState, 
        actions: { simulateChaos },
        metrics: {
            OrdersPerSec: {
                value: systemState.Metrics.OrdersPerSec,
                trend: ordersTrend,
                history: metricHistory.OrdersPerSec
            },
            ActiveSagas: {
                value: systemState.Metrics.ActiveSagas,
                trend: sagasTrend,
                history: metricHistory.ActiveSagas
            },
            DeadLetters: {
                value: systemState.Metrics.DeadLetters,
                trend: deadLettersTrend,
                history: metricHistory.DeadLetters
            }
        }
    };
};
