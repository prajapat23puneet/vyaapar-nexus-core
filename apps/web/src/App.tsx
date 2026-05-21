import React from 'react'
import { Routes, Route, Navigate, NavLink } from 'react-router-dom'
import { Dashboard } from './pages/Dashboard'
import { Orders } from './pages/Orders'
import { Products } from './pages/Products'
import { Customers } from './pages/Customers'
import { Activity, ShoppingCart, Package, Users } from 'lucide-react'
import { ErrorBoundary } from './components/ErrorBoundary'

export default function App() {
  return (
    <div className="flex flex-col min-h-screen">
      {/* Premium Header */}
      <header className="sticky top-0 z-50 w-full border-b border-[var(--border)] bg-[var(--bg)]/90 backdrop-blur-md">
        <div className="flex h-16 items-center justify-between px-6">
          <div className="flex items-center gap-2 font-bold text-xl tracking-tight text-[var(--text-h)]">
            <span className="bg-gradient-to-r from-[var(--accent)] to-purple-600 bg-clip-text text-transparent">VyaaparNexus</span>
            <span className="text-xs px-2 py-0.5 rounded-full border border-[var(--accent-border)] bg-[var(--accent-bg)] text-[var(--accent)] font-semibold">v2</span>
          </div>
          
          <nav className="flex items-center gap-6">
            <NavLink 
              to="/dashboard" 
              className={({ isActive }) => 
                `flex items-center gap-2 text-sm font-medium transition-all duration-300 px-3 py-2 rounded-md ${
                  isActive 
                    ? 'text-[var(--accent)] bg-[var(--accent-bg)] border border-[var(--accent-border)]/20 shadow-sm font-semibold' 
                    : 'text-[var(--text)] hover:text-[var(--text-h)] hover:bg-[var(--social-bg)]'
                }`
              }
            >
              <Activity className="h-4 w-4" />
              Dashboard
            </NavLink>
            <NavLink 
              to="/orders" 
              className={({ isActive }) => 
                `flex items-center gap-2 text-sm font-medium transition-all duration-300 px-3 py-2 rounded-md ${
                  isActive 
                    ? 'text-[var(--accent)] bg-[var(--accent-bg)] border border-[var(--accent-border)]/20 shadow-sm font-semibold' 
                    : 'text-[var(--text)] hover:text-[var(--text-h)] hover:bg-[var(--social-bg)]'
                }`
              }
            >
              <ShoppingCart className="h-4 w-4" />
              Orders
            </NavLink>
            <NavLink 
              to="/products" 
              className={({ isActive }) => 
                `flex items-center gap-2 text-sm font-medium transition-all duration-300 px-3 py-2 rounded-md ${
                  isActive 
                    ? 'text-[var(--accent)] bg-[var(--accent-bg)] border border-[var(--accent-border)]/20 shadow-sm font-semibold' 
                    : 'text-[var(--text)] hover:text-[var(--text-h)] hover:bg-[var(--social-bg)]'
                }`
              }
            >
              <Package className="h-4 w-4" />
              Products
            </NavLink>
            <NavLink 
              to="/customers" 
              className={({ isActive }) => 
                `flex items-center gap-2 text-sm font-medium transition-all duration-300 px-3 py-2 rounded-md ${
                  isActive 
                    ? 'text-[var(--accent)] bg-[var(--accent-bg)] border border-[var(--accent-border)]/20 shadow-sm font-semibold' 
                    : 'text-[var(--text)] hover:text-[var(--text-h)] hover:bg-[var(--social-bg)]'
                }`
              }
            >
              <Users className="h-4 w-4" />
              Customers
            </NavLink>
          </nav>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="flex-1 w-full max-w-7xl mx-auto px-6 py-8 text-left">
        <Routes>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<ErrorBoundary fallbackTitle="Dashboard Crash"><Dashboard /></ErrorBoundary>} />
          <Route path="/orders" element={<ErrorBoundary fallbackTitle="Orders Management Crash"><Orders /></ErrorBoundary>} />
          <Route path="/products" element={<ErrorBoundary fallbackTitle="Products Catalog Crash"><Products /></ErrorBoundary>} />
          <Route path="/customers" element={<ErrorBoundary fallbackTitle="Customers Registry Crash"><Customers /></ErrorBoundary>} />
        </Routes>
      </main>
    </div>
  )
}

