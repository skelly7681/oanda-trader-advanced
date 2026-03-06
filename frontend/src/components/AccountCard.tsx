import { AccountSnapshot } from '../types'

export function AccountCard({ account }: { account: AccountSnapshot }) {
  return (
    <div className="card">
      <h3>Account</h3>
      <div className="grid two">
        <div><span>Balance</span><strong>{account.balance.toFixed(2)}</strong></div>
        <div><span>Equity</span><strong>{account.equity.toFixed(2)}</strong></div>
        <div><span>Margin Used</span><strong>{account.marginUsed.toFixed(2)}</strong></div>
        <div><span>Margin Available</span><strong>{account.marginAvailable.toFixed(2)}</strong></div>
        <div><span>Unrealized PnL</span><strong>{account.unrealizedPnL.toFixed(2)}</strong></div>
        <div><span>Drawdown</span><strong>{(account.drawdownFraction * 100).toFixed(2)}%</strong></div>
      </div>
    </div>
  )
}