import { useEffect, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { Snapshot } from '../types'

export function useTradingHub() {
  const [snapshot, setSnapshot] = useState<Snapshot | null>(null)
  const [connected, setConnected] = useState(false)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hub/trading')
      .withAutomaticReconnect()
      .build()

    connection.on('snapshot', (data: Snapshot) => {
      setSnapshot(data)
    })

    connection.start()
      .then(() => setConnected(true))
      .catch(console.error)

    return () => {
      connection.stop().catch(() => {})
    }
  }, [])

  return { snapshot, connected }
}