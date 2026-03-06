import { Header } from './components/Header'
import { AccountCard } from './components/AccountCard'
import { PriceGrid } from './components/PriceGrid'
import { OpenTradesTable } from './components/OpenTradesTable'
import { RiskPanel } from './components/RiskPanel'
import { KillSwitchPanel } from './components/KillSwitchPanel'
import { StrategyPanel } from './components/StrategyPanel'
import { ConfigEditor } from './components/ConfigEditor'
import { AuditLogPanel } from './components/AuditLogPanel'
import { EquityPlaceholderChart } from './components/EquityPlaceholderChart'
import { useTradingHub } from './hooks/useTradingHub'

export default function App() {
  const { snapshot, connected } = useTradingHub()

  return (
    <div className="app-shell">
      <Header connected={connected} />
      {!snapshot ? (
        <div className="card">Waiting for live snapshot...</div>
      ) : (
        <>
          <div className="grid layout-top">
            <AccountCard account={snapshot.account} />
            <RiskPanel />
            <KillSwitchPanel />
          </div>
          <PriceGrid prices={snapshot.prices} />
          <EquityPlaceholderChart />
          <OpenTradesTable trades={snapshot.trades} />
          <div className="grid layout-bottom">
            <StrategyPanel />
            <ConfigEditor />
          </div>
          <AuditLogPanel logs={snapshot.logs} />
        </>
      )}
    </div>
  )
}