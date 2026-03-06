export function RiskPanel() {
  return (
    <div className="card">
      <h3>Risk Controls</h3>
      <ul>
        <li>Max 3 trades per day per instrument</li>
        <li>35% hard drawdown stop</li>
        <li>Stop loss required</li>
        <li>Spread filter active</li>
        <li>Max leverage ceiling active</li>
      </ul>
    </div>
  )
}