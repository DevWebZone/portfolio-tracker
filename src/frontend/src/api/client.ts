import type { MarketPrice, PortfolioSnapshot, Alert } from '../types'

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5273'
export const DEMO_USER = 'demo-user'

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers,
    },
    ...init,
  })

  if (!response.ok) {
    const body = await response.json().catch(() => ({ message: response.statusText }))
    throw new Error(body.message ?? 'Request failed')
  }

  return response.json() as Promise<T>
}

export const api = {
  getPortfolio: () => request<PortfolioSnapshot>(`/api/portfolio/${DEMO_USER}`),
  getPrices: () => request<MarketPrice[]>('/api/market/prices'),
  getAlerts: () => request<Alert[]>(`/api/alerts/${DEMO_USER}`),
}
