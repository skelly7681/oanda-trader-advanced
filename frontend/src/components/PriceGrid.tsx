import { PriceTick } from '../types'

export function PriceGrid({ prices }: { prices: PriceTick[] }) {
  return (
    <div className="card">
      <h3>Prices</h3>
      <div className="grid three">
        {prices.map(p => (
          <div className="price-box" key={p.instrument}>
            <div>{p.instrument}</div>
            <strong>{p.mid.toFixed(5)}</strong>
            <small>Bid {p.bid} / Ask {p.ask}</small>
            <small>Spread {p.spread}</small>
          </div>
        ))}
      </div>
    </div>
  )
}