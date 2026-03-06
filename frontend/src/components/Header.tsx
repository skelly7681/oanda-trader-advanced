export function Header({ connected }: { connected: boolean }) {
  return (
    <div className="header">
      <div>
        <h1>Oanda Trader Advanced</h1>
        <p>Paper / Practice / Live dashboard</p>
      </div>
      <div className={connected ? 'status ok' : 'status bad'}>
        {connected ? 'Connected' : 'Disconnected'}
      </div>
    </div>
  )
}