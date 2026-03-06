import { useEffect, useState } from 'react'
import { getJson, postJson } from '../api'

export function StrategyPanel() {
  const [strategies, setStrategies] = useState<string[]>([])
  const [strategyName, setStrategyName] = useState('sma-crossover')
  const [instrument, setInstrument] = useState('XAU_USD')
  const [granularity, setGranularity] = useState('M5')
  const [mode, setMode] = useState('Paper')
  const [result, setResult] = useState<string>('')

  useEffect(() => {
    getJson<string[]>('/strategy/list').then(setStrategies)
  }, [])

  async function run() {
    const data = await postJson<any>('/strategy/run', {
      strategyName,
      instrument,
      granularity,
      mode,
      acceptLiveRisk: false
    })
    setResult(JSON.stringify(data, null, 2))
  }

  return (
    <div className="card">
      <h3>Strategy Runner</h3>
      <div className="grid two">
        <label>
          Strategy
          <select value={strategyName} onChange={e => setStrategyName(e.target.value)}>
            {strategies.map(s => <option key={s} value={s}>{s}</option>)}
          </select>
        </label>
        <label>
          Instrument
          <select value={instrument} onChange={e => setInstrument(e.target.value)}>
            <option>EUR_USD</option>
            <option>GBP_USD</option>
            <option>XAU_USD</option>
          </select>
        </label>
        <label>
          Granularity
          <select value={granularity} onChange={e => setGranularity(e.target.value)}>
            <option>M1</option>
            <option>M5</option>
            <option>H1</option>
          </select>
        </label>
        <label>
          Mode
          <select value={mode} onChange={e => setMode(e.target.value)}>
            <option>Paper</option>
            <option>Practice</option>
            <option>Live</option>
          </select>
        </label>
      </div>
      <button onClick={run}>Run Strategy</button>
      <p>
        Result now includes the strategy signal, the risk decision, and any order result.
      </p>
      <pre>{result}</pre>
    </div>
  )
}
