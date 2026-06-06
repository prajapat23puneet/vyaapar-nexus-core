import React from 'react'
import { Helmet } from 'react-helmet-async'
import { MetricCards } from '../components/MetricCards'
import { ServiceMesh } from '../components/ServiceMesh'
import { DemoOrderPanel } from '../components/DemoOrderPanel'
import { SagaTimeline } from '../components/SagaTimeline'
import { LogTerminal } from '../components/LogTerminal'

export function Dashboard() {
  return (
    <div className="flex flex-col gap-5">
      <Helmet>
        <title>Dashboard | VyaaparNexus</title>
        <meta name="description" content="View live business metrics and order orchestration flow." />
      </Helmet>
      {/* ── Row 1: KPI Metric Cards ──────────────────────────────────────── */}
      <section aria-label="Key Performance Indicators">
        <MetricCards />
      </section>

      {/* ── Row 2: Demo Controls (left) + Service Mesh (right) ───────────── */}
      <section
        aria-label="Demo Controls and Service Mesh"
        className="grid grid-cols-1 lg:grid-cols-[280px_1fr] gap-5"
      >
        <DemoOrderPanel />
        <ServiceMesh />
      </section>

      {/* ── Row 3: Saga Timeline (left) + Log Terminal (right) ───────────── */}
      <section
        aria-label="Saga Timeline and Log Stream"
        className="grid grid-cols-1 lg:grid-cols-2 gap-5"
      >
        <SagaTimeline />
        <LogTerminal />
      </section>
    </div>
  )
}
