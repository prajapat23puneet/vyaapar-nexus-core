import { useEffect, useRef } from 'react';
import { useDispatch } from 'react-redux';
import { fetchEventSource } from '@microsoft/fetch-event-source';
import { setConnected, metricsUpdated } from '../store/metricsSlice';
import { setActiveOrder } from '../store/ordersSlice';
import type { StreamMetrics } from '@vyaapar-nexus/shared-types';

export function useSystemStream() {
  const dispatch = useDispatch();
  const abortControllerRef = useRef<AbortController | null>(null);
  const reconnectTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    let retryDelay = 1000;
    let isComponentMounted = true;

    const connect = () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }

      abortControllerRef.current = new AbortController();
      const apiKey = import.meta.env.VITE_API_KEY || 'vyaaparnexus-demo-key-2026';
      const apiUrl = import.meta.env.VITE_API_URL || '';

      fetchEventSource(`${apiUrl}/api/stream`, {
        method: 'GET',
        headers: {
          'Accept': 'text/event-stream',
          'X-Api-Key': apiKey,
        },
        signal: abortControllerRef.current.signal,
        async onopen(response) {
          if (response.ok && response.headers.get('content-type')?.includes('text/event-stream')) {
            if (isComponentMounted) {
              dispatch(setConnected(true));
              retryDelay = 1000; // reset delay on successful connection
            }
            return;
          } else {
            throw new Error(`Failed to connect: ${response.status}`);
          }
        },
        onmessage(ev) {
          if (!isComponentMounted) return;

          // 1. Ignore empty keep-alive pings from the server
          if (!ev.data) return;

          try {
            // Optional: Keep this log temporarily to see exactly what arrives
            // console.log("RAW SSE PAYLOAD:", ev.data);

            const data: StreamMetrics = JSON.parse(ev.data);

            // 2. Guard against backend sending 'null' during cold start
            if (!data) {
              console.warn("Received null snapshot from backend, waiting for next tick...");
              return;
            }

            // 3. Safely dispatch to Redux now that we know data exists
            dispatch(metricsUpdated(data));

            // 4. Update active order tracking in ordersSlice
            if (data.activeOrder) {
              dispatch(setActiveOrder({
                id: data.activeOrder.orderId,
                correlationId: data.activeOrder.correlationId
              }));
            } else {
              dispatch(setActiveOrder({ id: null, correlationId: null }));
            }
          } catch (err) {
            console.error('Failed to parse stream data:', err);
          }
        },
        onclose() {
          if (isComponentMounted) {
            dispatch(setConnected(false));
            scheduleReconnect();
          }
        },
        onerror(err) {
          if (isComponentMounted) {
            dispatch(setConnected(false));
            console.error('Stream error:', err);
            scheduleReconnect();
            throw err; // throw to prevent immediate retry by fetchEventSource itself, we handle backoff
          }
        }
      }).catch((err) => {
        if (err.name === 'AbortError') {
          // Ignored
        }
      });
    };

    const scheduleReconnect = () => {
      if (!isComponentMounted) return;
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
      reconnectTimeoutRef.current = setTimeout(() => {
        retryDelay = Math.min(retryDelay * 2, 30000); // Max backoff 30s
        connect();
      }, retryDelay);
    };

    connect();

    return () => {
      isComponentMounted = false;
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, [dispatch]);
}
