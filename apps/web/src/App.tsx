import React from 'react'
import { Routes, Route } from 'react-router-dom'
import { Toaster } from 'sonner'
import { Dashboard } from './pages/Dashboard'
import { Orders } from './pages/Orders'
import { Products } from './pages/Products'
import { Customers } from './pages/Customers'
import { DashboardShell } from './components/DashboardShell'
import { ErrorBoundary } from './components/ErrorBoundary'
import { useSystemStream } from './hooks/useSystemStream'

export default function App() {
  useSystemStream() // called once at app root
  return (
    <ErrorBoundary>
      <Routes>
        <Route element={<DashboardShell />}>
          <Route path="/"          element={<Dashboard />} />
          <Route path="/orders"    element={<Orders />} />
          <Route path="/products"  element={<Products />} />
          <Route path="/customers" element={<Customers />} />
        </Route>
      </Routes>
      <Toaster richColors position="bottom-right" />
    </ErrorBoundary>
  )
}
