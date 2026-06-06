import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit'
import { api } from '../api/client'
import type { Alert, MarketPrice, PortfolioSnapshot } from '../types'

interface PortfolioState {
  snapshot: PortfolioSnapshot | null
  prices: MarketPrice[]
  alerts: Alert[]
  status: 'idle' | 'loading' | 'ready' | 'error'
  connectionStatus: 'connecting' | 'connected' | 'disconnected'
  error: string | null
}

const initialState: PortfolioState = {
  snapshot: null,
  prices: [],
  alerts: [],
  status: 'idle',
  connectionStatus: 'disconnected',
  error: null,
}

export const loadDashboard = createAsyncThunk('portfolio/loadDashboard', async () => {
  const [snapshot, prices, alerts] = await Promise.all([
    api.getPortfolio(),
    api.getPrices(),
    api.getAlerts(),
  ])
  return { snapshot, prices, alerts }
})

const portfolioSlice = createSlice({
  name: 'portfolio',
  initialState,
  reducers: {
    priceUpdated(state, action: PayloadAction<MarketPrice>) {
      const normalized = normalizeMarketPrice(action.payload)
      if (!normalized) return

      const index = state.prices.findIndex((price) => price.symbol === normalized.symbol)
      if (index >= 0) {
        state.prices[index] = normalized
      } else {
        state.prices.push(normalized)
      }
    },
    portfolioUpdated(state, action: PayloadAction<PortfolioSnapshot>) {
      state.snapshot = action.payload
      state.alerts = action.payload.latestAlerts
      state.status = 'ready'
    },
    alertGenerated(state, action: PayloadAction<Alert>) {
      state.alerts = [action.payload, ...state.alerts.filter((alert) => alert.id !== action.payload.id)].slice(0, 20)
    },
    setConnectionStatus(state, action: PayloadAction<PortfolioState['connectionStatus']>) {
      state.connectionStatus = action.payload
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(loadDashboard.pending, (state) => {
        state.status = 'loading'
        state.error = null
      })
      .addCase(loadDashboard.fulfilled, (state, action) => {
        state.snapshot = action.payload.snapshot
        state.prices = action.payload.prices
        state.alerts = action.payload.alerts
        state.status = 'ready'
      })
      .addCase(loadDashboard.rejected, (state, action) => {
        state.status = 'error'
        state.error = action.error.message ?? 'Unable to load dashboard'
      })
  },
})

export const { priceUpdated, portfolioUpdated, alertGenerated, setConnectionStatus } = portfolioSlice.actions
export default portfolioSlice.reducer

function normalizeMarketPrice(value: unknown): MarketPrice | null {
  if (!value || typeof value !== 'object') return null

  const raw = value as Partial<MarketPrice> & { price?: number }
  const currentPrice = Number(raw.currentPrice ?? raw.price)
  const openingPrice = Number(raw.openingPrice)

  if (!raw.symbol || Number.isNaN(currentPrice) || Number.isNaN(openingPrice)) {
    return null
  }

  return {
    symbol: raw.symbol,
    currentPrice,
    openingPrice,
    movePercent: Number(raw.movePercent ?? 0),
    updatedAt: raw.updatedAt ?? new Date().toISOString(),
  }
}
