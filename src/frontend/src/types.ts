export type AssetType = 'Equity' | 'Crypto'
export type AlertSeverity = 'Warning' | 'Critical'

export interface MarketPrice {
  symbol: string
  currentPrice: number
  openingPrice: number
  movePercent: number
  updatedAt: string
}

export interface Alert {
  id: string
  symbol: string
  threshold: number
  direction: string
  severity: AlertSeverity
  triggeredAt: string
}

export interface Holding {
  symbol: string
  name: string
  assetType: AssetType
  quantity: number
  averageBuyPrice: number
  currentPrice: number
  costValue: number
  marketValue: number
  unrealizedPnL: number
  pnLPercent: number
  exposurePercent: number
}

export interface PortfolioSnapshot {
  userId: string
  totalValue: number
  costValue: number
  unrealizedPnL: number
  realizedPnL: number
  pnLPercent: number
  holdings: Holding[]
  latestAlerts: Alert[]
  updatedAt: string
}
