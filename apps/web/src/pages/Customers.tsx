import React from 'react'
import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import type { PagedResult, Customer } from '@vyaapar-nexus/shared-types'
import { Users, AlertCircle, RefreshCw, Mail, Phone, MapPin } from 'lucide-react'

export function Customers() {
  const { data, isLoading, error, refetch, isFetching } = useQuery<PagedResult<Customer>>({
    queryKey: ['customers'],
    queryFn: async () => {
      const res = await api.get<PagedResult<Customer>>('/api/v1/customers')
      return res.data
    }
  })

  const customers = data?.items && Array.isArray(data.items) ? data.items : []

  return (
    <div className="space-y-6">
      {/* Header Panel */}
      <div className="flex items-center justify-between border-b border-border pb-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-foreground my-1 flex items-center gap-2">
            <Users className="h-8 w-8 text-muted-foreground" />
            Customers Registry
          </h1>
          <p className="text-sm text-muted-foreground">
            View registered user profiles, contact details, and locations.
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
        // Loading Skeleton Table
        <div className="rounded-xl border border-[var(--border)] bg-[var(--bg)] overflow-hidden">
          <div className="p-4 border-b border-[var(--border)] bg-[var(--social-bg)]/20 animate-pulse">
            <div className="h-6 bg-[var(--border)] rounded w-1/4"></div>
          </div>
          <div className="divide-y divide-[var(--border)] animate-pulse">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="p-6 flex items-center justify-between gap-4">
                <div className="h-5 bg-[var(--border)] rounded w-1/3"></div>
                <div className="h-5 bg-[var(--border)] rounded w-1/4"></div>
                <div className="h-5 bg-[var(--border)] rounded w-1/5"></div>
              </div>
            ))}
          </div>
        </div>
      ) : error ? (
        // Error State
        <div className="flex flex-col items-center justify-center p-12 rounded-xl border border-red-500/20 bg-red-500/5 text-center max-w-xl mx-auto space-y-4">
          <div className="p-3 bg-red-500/10 rounded-full text-red-500">
            <AlertCircle className="h-8 w-8" />
          </div>
          <h2 className="text-xl font-semibold text-red-500">Failed to load customers</h2>
          <p className="text-sm text-[var(--text)]">
            Could not fetch customers from the server. Ensure the backend api is active.
          </p>
          <button
            onClick={() => refetch()}
            className="px-4 py-2 text-sm font-medium rounded-md bg-red-500 text-white hover:bg-red-600 transition-colors"
          >
            Retry Connection
          </button>
        </div>
      ) : customers.length === 0 ? (
        // Empty State
        <div className="flex flex-col items-center justify-center p-12 rounded-xl border border-border bg-muted/30 text-center max-w-xl mx-auto space-y-4">
          <div className="p-3 bg-muted rounded-full text-muted-foreground">
            <Users className="h-8 w-8" />
          </div>
          <h2 className="text-xl font-semibold text-foreground">No customers found</h2>
          <p className="text-sm text-muted-foreground">
            There are no customer records in the database.
          </p>
        </div>
      ) : (
        // Premium Customer Table Grid
        <div className="rounded-xl border border-[var(--border)] bg-[var(--bg)] shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-sm">
              <thead>
                <tr className="border-b border-[var(--border)] bg-[var(--social-bg)]/20 text-[var(--text-h)] font-semibold">
                  <th className="p-4">Customer Name</th>
                  <th className="p-4">Contact Info</th>
                  <th className="p-4">Location</th>
                  <th className="p-4 text-right">Created</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--border)] text-[var(--text)]">
                {customers.map((customer) => {
                  if (!customer || !customer.id) return null
                  
                  const hasAddress = customer.city || customer.state || customer.country
                  const locationText = hasAddress
                    ? [customer.city, customer.state, customer.country].filter(Boolean).join(', ')
                    : 'Not specified'

                  return (
                    <tr 
                      key={customer.id} 
                      className="hover:bg-[var(--social-bg)]/10 transition-colors duration-150"
                    >
                      <td className="p-4 font-medium text-[var(--text-h)]">
                        {customer.name || 'Anonymous User'}
                      </td>
                      <td className="p-4 space-y-1">
                        <div className="flex items-center gap-2 text-xs">
                          <Mail className="h-3 w-3 shrink-0 text-muted-foreground" />
                          <span>{customer.email}</span>
                        </div>
                        {customer.phone && (
                          <div className="flex items-center gap-2 text-xs">
                            <Phone className="h-3 w-3 shrink-0 text-sky-400" />
                            <span>{customer.phone}</span>
                          </div>
                        )}
                      </td>
                      <td className="p-4">
                        <div className="flex items-center gap-2 text-xs">
                          <MapPin className="h-3.5 w-3.5 shrink-0 text-emerald-500" />
                          <span>{locationText}</span>
                        </div>
                      </td>
                      <td className="p-4 text-right text-xs">
                        {customer.createdAt 
                          ? new Date(customer.createdAt).toLocaleDateString(undefined, {
                              year: 'numeric',
                              month: 'short',
                              day: 'numeric'
                            })
                          : '-'
                        }
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          <div className="p-4 border-t border-[var(--border)] bg-[var(--social-bg)]/10 flex items-center justify-between text-xs">
            <span>Showing {customers.length} registry entries</span>
            <span>Page {data?.page ?? 1} of {data?.totalPages ?? 1}</span>
          </div>
        </div>
      )}
    </div>
  )
}
