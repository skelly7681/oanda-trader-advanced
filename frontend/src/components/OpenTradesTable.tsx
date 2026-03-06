import { OpenTrade } from '../types'
import { deleteApi } from '../api'

export function OpenTradesTable({ trades, onRefresh }: { trades: OpenTrade[]; onRefresh?: () => void }) {
  async function closeTrade(id: string) {
    await deleteApi(`/trades/${id}`)
    onRefresh?.()
  }

  return (
    <div className="card">
      <h3>Open Trades</h3>
      <table>
        <thead>
          <tr>
            <th>Instrument</th>
            <th>Side</th>
            <th>Units</th>
            <th>Entry</th>
            <th>SL</th>
            <th>TP</th>
            <th>UPnL</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {trades.map(t => (
            <tr key={t.tradeId}>
              <td>{t.instrument}</td>
              <td>{t.side}</td>
              <td>{t.units}</td>
              <td>{t.entryPrice}</td>
              <td>{t.stopLoss ?? '-'}</td>
              <td>{t.takeProfit ?? '-'}</td>
              <td>{t.unrealizedPnL}</td>
              <td><button onClick={() => closeTrade(t.tradeId)}>Close</button></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}