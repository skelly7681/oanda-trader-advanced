import { postJson } from '../api'

export function KillSwitchPanel() {
  async function engage(closePositions: boolean) {
    await postJson('/kill-switch', { closePositions })
    alert('Kill switch engaged')
  }

  return (
    <div className="card danger">
      <h3>Kill Switch</h3>
      <p>Stops trading immediately. Optionally closes positions.</p>
      <div className="row">
        <button className="danger-btn" onClick={() => engage(false)}>Engage</button>
        <button className="danger-btn" onClick={() => engage(true)}>Engage + Close All</button>
      </div>
    </div>
  )
}