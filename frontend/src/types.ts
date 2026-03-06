export type AccountSnapshot = {
  accountId: string
  balance: number
  equity: number
  marginUsed: number
  marginAvailable: number
  unrealizedPnL: number
  dailyPnL: number
  startingBalance: number
  drawdownFraction: number
}

export type PriceTick = {
  instrument: string
  bid: number
  ask: number
  mid: number
  spread: number
  timestamp: string
}

export type OpenTrade = {
  tradeId: string
  instrument: string
  side: string
  units: number
  entryPrice: number
  stopLoss?: number
  takeProfit?: number
  unrealizedPnL: number
  openedAt: string
}

export type Snapshot = {
  account: AccountSnapshot
  prices: PriceTick[]
  trades: OpenTrade[]
  logs: string[]
  timestamp: string
}