import React from 'react'
import { Helmet } from 'react-helmet-async'
import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import type { PagedResult, Product } from '@vyaapar-nexus/shared-types'
import { Package, AlertCircle, RefreshCw, Layers } from 'lucide-react'

export function Products() {
  const { data, isLoading, error, refetch, isFetching } = useQuery<PagedResult<Product>>({
    queryKey: ['products'],
    queryFn: async () => {
      const res = await api.get<PagedResult<Product>>('/api/v1/products')
      return res.data
    }
  })

  // Defensive array retrieval
  const products = data?.items && Array.isArray(data.items) ? data.items : []

  return (
    <div className="space-y-6">
      <Helmet>
        <title>Products | VyaaparNexus</title>
        <meta name="description" content="View product inventory and stock levels." />
      </Helmet>
      <div className="flex items-center justify-between border-b border-border pb-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-foreground my-1 flex items-center gap-2">
            <Package className="h-8 w-8 text-muted-foreground" />
            Products Catalog
          </h1>
          <p className="text-sm text-muted-foreground">
            Manage inventory, view real-time stock levels, and explore active listings.
          </p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isLoading || isFetching}
          className="flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md border border-border bg-muted/50 text-foreground hover:bg-muted active:scale-95 transition-all duration-200 disabled:opacity-50"
        >
          <RefreshCw className={`h-4 w-4 ${isFetching ? 'animate-spin' : ''}`} />
          Refresh
        </button>
      </div>

      {isLoading ? (
        // Loading Skeleton Grid
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[...Array(6)].map((_, i) => (
            <div
              key={i}
              className="animate-pulse rounded-xl border border-[var(--border)] bg-[var(--social-bg)] p-6 space-y-4 h-48"
            >
              <div className="flex justify-between items-start">
                <div className="h-6 bg-[var(--border)] rounded w-1/2"></div>
                <div className="h-5 bg-[var(--border)] rounded-full w-16"></div>
              </div>
              <div className="h-4 bg-[var(--border)] rounded w-3/4"></div>
              <div className="h-4 bg-[var(--border)] rounded w-1/3 mt-4"></div>
              <div className="flex justify-between items-center pt-2">
                <div className="h-6 bg-[var(--border)] rounded w-20"></div>
                <div className="h-4 bg-[var(--border)] rounded w-16"></div>
              </div>
            </div>
          ))}
        </div>
      ) : error ? (
        // Premium Error State
        <div className="flex flex-col items-center justify-center p-12 rounded-xl border border-red-500/20 bg-red-500/5 text-center max-w-xl mx-auto space-y-4">
          <div className="p-3 bg-red-500/10 rounded-full text-red-500">
            <AlertCircle className="h-8 w-8" />
          </div>
          <h2 className="text-xl font-semibold text-red-500">Failed to load products</h2>
          <p className="text-sm text-[var(--text)]">
            Could not retrieve data from the server. Make sure the backend service is running and accessible at {import.meta.env.VITE_API_URL ?? 'http://localhost:5117'}.
          </p>
          <button
            onClick={() => refetch()}
            className="px-4 py-2 text-sm font-medium rounded-md bg-red-500 text-white hover:bg-red-600 transition-colors"
          >
            Retry Connection
          </button>
        </div>
      ) : products.length === 0 ? (
        // Empty State
        <div className="flex flex-col items-center justify-center p-12 rounded-xl border border-border bg-muted/30 text-center max-w-xl mx-auto space-y-4">
          <div className="p-3 bg-muted rounded-full text-muted-foreground">
            <Package className="h-8 w-8" />
          </div>
          <h2 className="text-xl font-semibold text-foreground">No products found</h2>
          <p className="text-sm text-muted-foreground">
            The catalog is currently empty. Initialize seed data in the backend database to view listings.
          </p>
        </div>
      ) : (
        // Products Grid
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {products.map((product) => {
            if (!product || !product.id) return null
            const stockQuantity = product.stockQuantity ?? 0
            const reorderLevel = product.reorderLevel ?? 0
            const isLowStock = stockQuantity <= reorderLevel
            const isOutOfStock = stockQuantity === 0
            const unitPrice = product.unitPrice ?? 0

            let formattedPrice = '0.00'
            try {
              formattedPrice = typeof unitPrice === 'number'
                ? unitPrice.toLocaleString('en-IN', { minimumFractionDigits: 2 })
                : parseFloat(String(unitPrice)).toLocaleString('en-IN', { minimumFractionDigits: 2 })
            } catch (e) {
              console.error('Error formatting price:', e)
            }

            return (
              <div
                key={product.id}
                className="group relative rounded-xl border border-border bg-card p-6 shadow-sm hover:shadow-md hover:border-foreground/20 hover:scale-[1.02] active:scale-[0.99] transition-all duration-300 flex flex-col justify-between h-48"
              >
                <div>
                  <div className="flex items-start justify-between gap-4">
                    <h3 className="font-bold text-lg text-foreground group-hover:text-foreground transition-colors line-clamp-1">
                      {product.name}
                    </h3>
                    <span className="shrink-0 inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-[var(--social-bg)] text-[var(--text)] border border-[var(--border)]">
                      <Layers className="h-3 w-3" />
                      {product.categoryName || 'General'}
                    </span>
                  </div>
                  <p className="text-sm text-[var(--text)] mt-2 line-clamp-2">
                    {product.description || 'No description available for this product.'}
                  </p>
                </div>

                <div className="flex items-center justify-between pt-4 border-t border-[var(--border)]/50 mt-4">
                  <span className="text-xl font-extrabold text-[var(--text-h)]">
                    ₹{formattedPrice}
                  </span>
                  
                  <span
                    className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${
                      isOutOfStock
                        ? 'bg-red-500/10 text-red-500 border border-red-500/20'
                        : isLowStock
                        ? 'bg-amber-500/10 text-amber-500 border border-amber-500/20'
                        : 'bg-emerald-500/10 text-emerald-500 border border-emerald-500/20'
                    }`}
                  >
                    {isOutOfStock
                      ? 'Out of Stock'
                      : isLowStock
                      ? `Low Stock: ${stockQuantity}`
                      : `In Stock: ${stockQuantity}`}
                  </span>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

