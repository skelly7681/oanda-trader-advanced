export function AuditLogPanel({ logs }: { logs: string[] }) {
  return (
    <div className="card">
      <h3>Audit Log</h3>
      <div className="log-box">
        {logs.map((log, i) => <div key={i}>{log}</div>)}
      </div>
    </div>
  )
}