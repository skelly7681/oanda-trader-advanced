import { useEffect, useState } from 'react'
import { getJson } from '../api'

export function ConfigEditor() {
  const [config, setConfig] = useState<any>(null)

  useEffect(() => {
    getJson('/config').then(setConfig)
  }, [])

  if (!config) return <div className="card">Loading config...</div>

  return (
    <div className="card">
      <h3>Current Config</h3>
      <pre>{JSON.stringify(config, null, 2)}</pre>
      <p>This starter exposes config read-only. Add a write endpoint before enabling live edits.</p>
    </div>
  )
}