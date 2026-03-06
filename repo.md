# Oanda Trader Advanced — React + ASP.NET Reference Repo

This is a copy-pasteable multi-file repository scaffold for an advanced local trading workstation using:

* **Backend:** ASP.NET Core (.NET 8) Web API
* **Frontend:** React + TypeScript + Vite
* **Architecture:** Clean-ish separation with Domain / Application / Infrastructure / API / UI
* **Modes:** Paper, Practice, Live (live blocked behind explicit confirmation)
* **Core features:**

  * OANDA connectivity scaffold
  * paper broker with identical orchestration path
  * strategy engine
  * risk gate
  * kill switch
  * reconciliation job scaffold
  * backtest endpoint scaffold
  * SignalR live dashboard updates
  * React dashboard for account, prices, open trades, logs, config, strategies

---

## 1) Repository Layout

```text
oanda-trader-advanced/
├─ README.md
├─ backend/
│  ├─ OandaTrader.sln
│  └─ src/
│     ├─ OandaTrader.Domain/
│     │  ├─ OandaTrader.Domain.csproj
│     │  ├─ Enums.cs
│     │  ├─ Instrument.cs
│     │  ├─ PriceTick.cs
│     │  ├─ Candle.cs
│     │  ├─ Signal.cs
│     │  ├─ OrderModels.cs
│     │  ├─ AccountModels.cs
│     │  ├─ RiskModels.cs
│     │  └─ StrategyModels.cs
│     ├─ OandaTrader.Application/
│     │  ├─ OandaTrader.Application.csproj
│     │  ├─ Abstractions.cs
│     │  ├─ RiskGate.cs
│     │  ├─ Strategies/
│     │  │  ├─ SmaCrossoverStrategy.cs
│     │  │  └─ BreakoutAtrStrategy.cs
│     │  ├─ TradingEngine.cs
│     │  ├─ ReconciliationService.cs
│     │  └─ BacktestService.cs
│     ├─ OandaTrader.Infrastructure/
│     │  ├─ OandaTrader.Infrastructure.csproj
│     │  ├─ Configuration/
│     │  │  ├─ TradingOptions.cs
│     │  │  └─ OandaOptions.cs
│     │  ├─ Persistence/
│     │  │  ├─ FileAuditStore.cs
│     │  │  ├─ FileKillSwitchStore.cs
│     │  │  └─ InMemoryStateStore.cs
│     │  ├─ Brokers/
│     │  │  ├─ OandaBrokerGateway.cs
│     │  │  └─ PaperBrokerGateway.cs
│     │  ├─ MarketData/
│     │  │  ├─ OandaMarketDataClient.cs
│     │  │  └─ SimulatedMarketDataClient.cs
│     │  └─ DependencyInjection.cs
│     ├─ OandaTrader.Api/
│     │  ├─ OandaTrader.Api.csproj
│     │  ├─ Program.cs
│     │  ├─ Hubs/
│     │  │  └─ TradingHub.cs
│     │  ├─ Models/
│     │  │  ├─ ApiDtos.cs
│     │  │  └─ ConfigDtos.cs
│     │  ├─ Controllers/
│     │  │  ├─ AccountController.cs
│     │  │  ├─ PricesController.cs
│     │  │  ├─ TradesController.cs
│     │  │  ├─ ConfigController.cs
│     │  │  ├─ StrategyController.cs
│     │  │  ├─ KillSwitchController.cs
│     │  │  └─ BacktestController.cs
│     │  ├─ Services/
│     │  │  ├─ DashboardBroadcaster.cs
│     │  │  └─ HostedMarketLoop.cs
│     │  └─ appsettings.json
│     └─ OandaTrader.Tests/
│        ├─ OandaTrader.Tests.csproj
│        ├─ RiskGateTests.cs
│        └─ StrategyTests.cs
└─ frontend/
   ├─ package.json
   ├─ vite.config.ts
   ├─ tsconfig.json
   └─ src/
      ├─ main.tsx
      ├─ App.tsx
      ├─ types.ts
      ├─ api.ts
      ├─ styles.css
      ├─ components/
      │  ├─ Header.tsx
      │  ├─ AccountCard.tsx
      │  ├─ PriceGrid.tsx
      │  ├─ OpenTradesTable.tsx
      │  ├─ RiskPanel.tsx
      │  ├─ KillSwitchPanel.tsx
      │  ├─ StrategyPanel.tsx
      │  ├─ ConfigEditor.tsx
      │  ├─ AuditLogPanel.tsx
      │  └─ EquityPlaceholderChart.tsx
      └─ hooks/
         └─ useTradingHub.ts
```

---

## 2) High-Level Architecture

```text
React Dashboard
   |
   | REST + SignalR
   v
ASP.NET API
   |
   +--> TradingEngine ------------------------------+
   |        |                                       |
   |        +--> Strategy Engine                    |
   |        +--> Risk Gate                          |
   |        +--> Kill Switch                        |
   |        +--> Audit Store                        |
   |        +--> Reconciliation                     |
   |                                                |
   +--> Market Data Client --------------------+    |
   |                                           |    |
   +--> Broker Gateway (Paper / OANDA) <-------+----+

Modes:
- paper       => simulated fills
- practice    => OANDA practice endpoints
- live        => OANDA live endpoints + explicit live-risk confirmation required
```

---

## 3) Backend Solution Files

### `backend/OandaTrader.sln`

Create with:

```bash
dotnet new sln -n OandaTrader
```

Then add projects below.

---

## 4) Domain Project

### `backend/src/OandaTrader.Domain/OandaTrader.Domain.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

### `backend/src/OandaTrader.Domain/Enums.cs`

```csharp
namespace OandaTrader.Domain;

public enum TradeSide { Buy, Sell }
public enum SignalAction { Hold, Buy, Sell }
public enum OrderType { Market, Limit, Stop }
public enum OrderState { Created, Submitted, Filled, Rejected, Managed, Closed, Cancelled }
public enum TradingMode { Paper, Practice, Live }
public enum KillSwitchState { Disengaged, Engaged }
```

### `backend/src/OandaTrader.Domain/Instrument.cs`

```csharp
namespace OandaTrader.Domain;

public sealed record Instrument(string Symbol, int DisplayPrecision, decimal PipSize)
{
    public static readonly Instrument EurUsd = new("EUR_USD", 5, 0.0001m);
    public static readonly Instrument GbpUsd = new("GBP_USD", 5, 0.0001m);
    public static readonly Instrument XauUsd = new("XAU_USD", 3, 0.01m);

    public static IReadOnlyList<Instrument> Supported => new[] { EurUsd, GbpUsd, XauUsd };
}
```

### `backend/src/OandaTrader.Domain/PriceTick.cs`

```csharp
namespace OandaTrader.Domain;

public sealed record PriceTick(
    string Instrument,
    decimal Bid,
    decimal Ask,
    DateTimeOffset Timestamp)
{
    public decimal Mid => (Bid + Ask) / 2m;
    public decimal Spread => Ask - Bid;
}
```

### `backend/src/OandaTrader.Domain/Candle.cs`

```csharp
namespace OandaTrader.Domain;

public sealed record Candle(
    string Instrument,
    string Granularity,
    DateTimeOffset Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    bool Complete);
```

### `backend/src/OandaTrader.Domain/Signal.cs`

```csharp
namespace OandaTrader.Domain;

public sealed record Signal(
    string Strategy,
    string Instrument,
    SignalAction Action,
    decimal? Entry,
    decimal? StopLoss,
    decimal? TakeProfit,
    decimal Units,
    string Reason,
    DateTimeOffset Timestamp);
```

### `backend/src/OandaTrader.Domain/OrderModels.cs`

```csharp
namespace OandaTrader.Domain;

public sealed class OrderRequest
{
    public required string Instrument { get; init; }
    public required OrderType Type { get; init; }
    public required TradeSide Side { get; init; }
    public required decimal Units { get; init; }
    public decimal? Price { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    public bool UseTrailingStop { get; init; }
    public decimal? TrailingStopDistance { get; init; }
    public required string ClientOrderId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed class OrderResult
{
    public required string OrderId { get; init; }
    public required OrderState State { get; init; }
    public string? BrokerTradeId { get; init; }
    public string? BrokerMessage { get; init; }
    public decimal? FillPrice { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

public sealed class OpenTrade
{
    public required string TradeId { get; init; }
    public required string Instrument { get; init; }
    public required TradeSide Side { get; init; }
    public required decimal Units { get; init; }
    public required decimal EntryPrice { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    public required decimal UnrealizedPnL { get; init; }
    public required DateTimeOffset OpenedAt { get; init; }
}

public sealed class PositionSnapshot
{
    public required string Instrument { get; init; }
    public decimal LongUnits { get; init; }
    public decimal ShortUnits { get; init; }
    public decimal NetUnits => LongUnits - ShortUnits;
    public decimal UnrealizedPnL { get; init; }
}
```

### `backend/src/OandaTrader.Domain/AccountModels.cs`

```csharp
namespace OandaTrader.Domain;

public sealed class AccountSnapshot
{
    public required string AccountId { get; init; }
    public decimal Balance { get; init; }
    public decimal Equity { get; init; }
    public decimal MarginUsed { get; init; }
    public decimal MarginAvailable { get; init; }
    public decimal UnrealizedPnL { get; init; }
    public decimal DailyPnL { get; init; }
    public decimal StartingBalance { get; init; }
    public decimal DrawdownFraction => StartingBalance <= 0 ? 0 : Math.Max(0, (StartingBalance - Equity) / StartingBalance);
    public List<OpenTrade> OpenTrades { get; init; } = new();
    public List<PositionSnapshot> Positions { get; init; } = new();
}
```

### `backend/src/OandaTrader.Domain/RiskModels.cs`

```csharp
namespace OandaTrader.Domain;

public sealed record RiskDecision(bool Approved, string Reason);

public sealed class StrategyRunRequest
{
    public required string StrategyName { get; init; }
    public required string Instrument { get; init; }
    public required string Granularity { get; init; }
    public required TradingMode Mode { get; init; }
    public bool AcceptLiveRisk { get; init; }
}
```

### `backend/src/OandaTrader.Domain/StrategyModels.cs`

```csharp
namespace OandaTrader.Domain;

public sealed class StrategyContext
{
    public required string Instrument { get; init; }
    public required string Granularity { get; init; }
    public required IReadOnlyList<Candle> Candles { get; init; }
    public PriceTick? CurrentTick { get; init; }
    public required DateTimeOffset Now { get; init; }
}
```

---

## 5) Application Project

### `backend/src/OandaTrader.Application/OandaTrader.Application.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\OandaTrader.Domain\OandaTrader.Domain.csproj" />
  </ItemGroup>
</Project>
```

### `backend/src/OandaTrader.Application/Abstractions.cs`

```csharp
using OandaTrader.Domain;

namespace OandaTrader.Application;

public interface IStrategy
{
    string Name { get; }
    void OnTick(PriceTick tick);
    void OnBar(Candle bar);
    Signal GenerateSignal(StrategyContext context);
}

public interface IBrokerGateway
{
    TradingMode Mode { get; }
    Task<AccountSnapshot> GetAccountSnapshotAsync(CancellationToken ct);
    Task<IReadOnlyList<PriceTick>> GetLatestPricesAsync(IEnumerable<string> instruments, CancellationToken ct);
    Task<OrderResult> PlaceOrderAsync(OrderRequest request, CancellationToken ct);
    Task<IReadOnlyList<OpenTrade>> GetOpenTradesAsync(CancellationToken ct);
    Task CloseTradeAsync(string tradeId, CancellationToken ct);
    Task EngageKillSwitchCloseAllAsync(CancellationToken ct);
}

public interface IMarketDataClient
{
    Task<IReadOnlyList<Candle>> GetCandlesAsync(string instrument, string granularity, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}

public interface IAuditStore
{
    Task AppendAsync(string eventType, object payload, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRecentAsync(int count, CancellationToken ct);
}

public interface IKillSwitchStore
{
    Task<KillSwitchState> GetStateAsync(CancellationToken ct);
    Task SetStateAsync(KillSwitchState state, CancellationToken ct);
}

public interface IStrategyRegistry
{
    IReadOnlyList<string> Names { get; }
    IStrategy Resolve(string name);
}
```

### `backend/src/OandaTrader.Application/RiskGate.cs`

```csharp
using OandaTrader.Domain;

namespace OandaTrader.Application;

public sealed class RiskGate
{
    public RiskDecision Evaluate(
        AccountSnapshot account,
        Signal signal,
        PriceTick tick,
        int tradesTodayForInstrument,
        decimal maxRiskPerTradeFraction,
        decimal maxDailyLossFraction,
        int maxTradesPerDayPerInstrument,
        decimal maxSpread,
        decimal maxLeverage,
        bool stopLossRequired)
    {
        if (signal.Action == SignalAction.Hold)
            return new(true, "Hold signal does not require blocking.");

        if (account.DrawdownFraction >= 0.35m)
            return new(false, "Drawdown hard stop triggered at 35%.");

        if (account.StartingBalance > 0 && Math.Abs(account.DailyPnL) / account.StartingBalance >= maxDailyLossFraction && account.DailyPnL < 0)
            return new(false, "Daily loss limit exceeded.");

        if (tradesTodayForInstrument >= maxTradesPerDayPerInstrument)
            return new(false, "Trade count limit exceeded for instrument.");

        if (tick.Spread > maxSpread)
            return new(false, $"Spread too wide: {tick.Spread}");

        if (stopLossRequired && signal.StopLoss is null)
            return new(false, "Stop loss is required.");

        if (signal.Entry is null || signal.StopLoss is null)
            return new(false, "Signal is missing entry or stop loss.");

        var stopDistance = Math.Abs(signal.Entry.Value - signal.StopLoss.Value);
        if (stopDistance <= 0)
            return new(false, "Stop distance must be positive.");

        var riskDollars = stopDistance * signal.Units;
        var riskFraction = account.Equity <= 0 ? 1m : riskDollars / account.Equity;
        if (riskFraction > maxRiskPerTradeFraction)
            return new(false, $"Risk per trade exceeds threshold: {riskFraction:P2}");

        var leverage = account.Equity <= 0 || signal.Entry.Value <= 0 ? decimal.MaxValue : (signal.Units * signal.Entry.Value) / account.Equity;
        if (leverage > maxLeverage)
            return new(false, $"Leverage exceeds threshold: {leverage:N2}");

        return new(true, "Approved");
    }
}
```

### `backend/src/OandaTrader.Application/Strategies/SmaCrossoverStrategy.cs`

```csharp
using OandaTrader.Domain;

namespace OandaTrader.Application.Strategies;

public sealed class SmaCrossoverStrategy : IStrategy
{
    public string Name => "sma-crossover";

    public void OnTick(PriceTick tick) { }
    public void OnBar(Candle bar) { }

    public Signal GenerateSignal(StrategyContext context)
    {
        var closes = context.Candles.Select(c => c.Close).ToList();
        if (closes.Count < 25)
            return Hold(context, "Not enough candles");

        decimal fast = closes.TakeLast(10).Average();
        decimal slow = closes.TakeLast(20).Average();
        var entry = closes.Last();

        if (fast > slow)
        {
            return new Signal(Name, context.Instrument, SignalAction.Buy, entry, entry - (entry * 0.0025m), entry + (entry * 0.005m), 1000m, "Fast SMA > Slow SMA", context.Now);
        }

        if (fast < slow)
        {
            return new Signal(Name, context.Instrument, SignalAction.Sell, entry, entry + (entry * 0.0025m), entry - (entry * 0.005m), 1000m, "Fast SMA < Slow SMA", context.Now);
        }

        return Hold(context, "No crossover edge");
    }

    private Signal Hold(StrategyContext context, string reason)
        => new(Name, context.Instrument, SignalAction.Hold, null, null, null, 0, reason, context.Now);
}
```

### `backend/src/OandaTrader.Application/Strategies/BreakoutAtrStrategy.cs`

```csharp
using OandaTrader.Domain;

namespace OandaTrader.Application.Strategies;

public sealed class BreakoutAtrStrategy : IStrategy
{
    public string Name => "breakout-atr";

    public void OnTick(PriceTick tick) { }
    public void OnBar(Candle bar) { }

    public Signal GenerateSignal(StrategyContext context)
    {
        var candles = context.Candles.ToList();
        if (candles.Count < 30)
            return Hold(context, "Not enough candles");

        var last = candles.Last();
        var rangeWindow = candles.TakeLast(20).ToList();
        var breakoutHigh = rangeWindow.Max(x => x.High);
        var breakoutLow = rangeWindow.Min(x => x.Low);
        var atr = CalcAtr(candles.TakeLast(15).ToList());

        if (last.Close >= breakoutHigh)
        {
            var entry = last.Close;
            var stop = entry - (atr * 1.5m);
            var tp = entry + (atr * 3m);
            return new Signal(Name, context.Instrument, SignalAction.Buy, entry, stop, tp, 1000m, "Upside breakout with ATR stop", context.Now);
        }

        if (last.Close <= breakoutLow)
        {
            var entry = last.Close;
            var stop = entry + (atr * 1.5m);
            var tp = entry - (atr * 3m);
            return new Signal(Name, context.Instrument, SignalAction.Sell, entry, stop, tp, 1000m, "Downside breakout with ATR stop", context.Now);
        }

        return Hold(context, "No breakout");
    }

    private static decimal CalcAtr(List<Candle> candles)
    {
        if (candles.Count < 2) return 0;
        var trs = new List<decimal>();
        for (int i = 1; i < candles.Count; i++)
        {
            var cur = candles[i];
            var prev = candles[i - 1];
            var tr = new[]
            {
                cur.High - cur.Low,
                Math.Abs(cur.High - prev.Close),
                Math.Abs(cur.Low - prev.Close)
            }.Max();
            trs.Add(tr);
        }
        return trs.Count == 0 ? 0 : trs.Average();
    }

    private Signal Hold(StrategyContext context, string reason)
        => new(Name, context.Instrument, SignalAction.Hold, null, null, null, 0, reason, context.Now);
}
```

### `backend/src/OandaTrader.Application/TradingEngine.cs`

```csharp
using System.Collections.Concurrent;
using OandaTrader.Domain;

namespace OandaTrader.Application;

public sealed class TradingEngine
{
    private readonly IBrokerGateway _broker;
    private readonly IMarketDataClient _marketData;
    private readonly IStrategyRegistry _strategies;
    private readonly IAuditStore _audit;
    private readonly IKillSwitchStore _killSwitch;
    private readonly RiskGate _riskGate;

    private readonly ConcurrentDictionary<string, int> _tradeCountsByInstrument = new();

    public TradingEngine(
        IBrokerGateway broker,
        IMarketDataClient marketData,
        IStrategyRegistry strategies,
        IAuditStore audit,
        IKillSwitchStore killSwitch,
        RiskGate riskGate)
    {
        _broker = broker;
        _marketData = marketData;
        _strategies = strategies;
        _audit = audit;
        _killSwitch = killSwitch;
        _riskGate = riskGate;
    }

    public async Task<Signal> EvaluateAndMaybeTradeAsync(
        StrategyRunRequest request,
        decimal maxRiskPerTradeFraction,
        decimal maxDailyLossFraction,
        int maxTradesPerDayPerInstrument,
        decimal maxSpread,
        decimal maxLeverage,
        bool stopLossRequired,
        CancellationToken ct)
    {
        var kill = await _killSwitch.GetStateAsync(ct);
        if (kill == KillSwitchState.Engaged)
            throw new InvalidOperationException("Kill switch is engaged.");

        if (request.Mode == TradingMode.Live && !request.AcceptLiveRisk)
            throw new InvalidOperationException("Live mode requires explicit acceptance.");

        var strategy = _strategies.Resolve(request.StrategyName);
        var account = await _broker.GetAccountSnapshotAsync(ct);
        var tick = (await _broker.GetLatestPricesAsync(new[] { request.Instrument }, ct)).Single();

        var candles = await _marketData.GetCandlesAsync(
            request.Instrument,
            request.Granularity,
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow,
            ct);

        var context = new StrategyContext
        {
            Instrument = request.Instrument,
            Granularity = request.Granularity,
            Candles = candles,
            CurrentTick = tick,
            Now = DateTimeOffset.UtcNow
        };

        var signal = strategy.GenerateSignal(context);
        await _audit.AppendAsync("signal", signal, ct);

        var tradesToday = _tradeCountsByInstrument.GetValueOrDefault(request.Instrument, 0);
        var riskDecision = _riskGate.Evaluate(
            account,
            signal,
            tick,
            tradesToday,
            maxRiskPerTradeFraction,
            maxDailyLossFraction,
            maxTradesPerDayPerInstrument,
            maxSpread,
            maxLeverage,
            stopLossRequired);

        await _audit.AppendAsync("risk-decision", riskDecision, ct);

        if (!riskDecision.Approved || signal.Action == SignalAction.Hold)
            return signal;

        var order = new OrderRequest
        {
            Instrument = signal.Instrument,
            Type = OrderType.Market,
            Side = signal.Action == SignalAction.Buy ? TradeSide.Buy : TradeSide.Sell,
            Units = signal.Units,
            StopLoss = signal.StopLoss,
            TakeProfit = signal.TakeProfit,
            ClientOrderId = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow
        };

        await _audit.AppendAsync("order-request", order, ct);
        var result = await _broker.PlaceOrderAsync(order, ct);
        await _audit.AppendAsync("order-result", result, ct);

        if (result.State == OrderState.Filled)
            _tradeCountsByInstrument.AddOrUpdate(request.Instrument, 1, (_, current) => current + 1);

        return signal;
    }

    public async Task EngageKillSwitchAsync(bool closePositions, CancellationToken ct)
    {
        await _killSwitch.SetStateAsync(KillSwitchState.Engaged, ct);
        await _audit.AppendAsync("kill-switch", new { state = "engaged", closePositions }, ct);
        if (closePositions)
            await _broker.EngageKillSwitchCloseAllAsync(ct);
    }
}
```

### `backend/src/OandaTrader.Application/ReconciliationService.cs`

```csharp
using OandaTrader.Domain;

namespace OandaTrader.Application;

public sealed class ReconciliationService
{
    private readonly IBrokerGateway _broker;
    private readonly IAuditStore _audit;

    public ReconciliationService(IBrokerGateway broker, IAuditStore audit)
    {
        _broker = broker;
        _audit = audit;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var trades = await _broker.GetOpenTradesAsync(ct);
        await _audit.AppendAsync("reconciliation", new { openTradeCount = trades.Count, ranAt = DateTimeOffset.UtcNow }, ct);
    }
}
```

### `backend/src/OandaTrader.Application/BacktestService.cs`

```csharp
using OandaTrader.Domain;

namespace OandaTrader.Application;

public sealed class BacktestService
{
    private readonly IMarketDataClient _marketData;
    private readonly IStrategyRegistry _strategies;

    public BacktestService(IMarketDataClient marketData, IStrategyRegistry strategies)
    {
        _marketData = marketData;
        _strategies = strategies;
    }

    public async Task<object> RunAsync(string strategyName, string instrument, string granularity, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var candles = await _marketData.GetCandlesAsync(instrument, granularity, from, to, ct);
        var strategy = _strategies.Resolve(strategyName);

        int trades = 0;
        int wins = 0;
        decimal pnl = 0;

        for (int i = 30; i < candles.Count; i++)
        {
            var window = candles.Take(i + 1).ToList();
            var context = new StrategyContext
            {
                Instrument = instrument,
                Granularity = granularity,
                Candles = window,
                CurrentTick = null,
                Now = window.Last().Timestamp
            };

            var signal = strategy.GenerateSignal(context);
            if (signal.Action == SignalAction.Hold || signal.Entry is null || signal.TakeProfit is null || signal.StopLoss is null)
                continue;

            trades++;
            var rr = Math.Abs(signal.TakeProfit.Value - signal.Entry.Value) / Math.Abs(signal.Entry.Value - signal.StopLoss.Value);
            if (rr >= 2)
            {
                wins++;
                pnl += 2;
            }
            else
            {
                pnl -= 1;
            }
        }

        return new
        {
            strategy = strategyName,
            instrument,
            granularity,
            from,
            to,
            tradeCount = trades,
            winRate = trades == 0 ? 0 : (decimal)wins / trades,
            netR = pnl
        };
    }
}
```

---

## 6) Infrastructure Project

### `backend/src/OandaTrader.Infrastructure/OandaTrader.Infrastructure.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\OandaTrader.Domain\OandaTrader.Domain.csproj" />
    <ProjectReference Include="..\OandaTrader.Application\OandaTrader.Application.csproj" />
  </ItemGroup>
</Project>
```

### `backend/src/OandaTrader.Infrastructure/Configuration/TradingOptions.cs`

```csharp
namespace OandaTrader.Infrastructure.Configuration;

public sealed class TradingOptions
{
    public string DefaultInstrument { get; set; } = "XAU_USD";
    public string DefaultGranularity { get; set; } = "M5";
    public decimal MaxRiskPerTradeFraction { get; set; } = 0.01m;
    public decimal MaxDailyLossFraction { get; set; } = 0.03m;
    public int MaxTradesPerDayPerInstrument { get; set; } = 3;
    public decimal MaxSpread { get; set; } = 0.50m;
    public decimal MaxLeverage { get; set; } = 5m;
    public bool StopLossRequired { get; set; } = true;
    public bool ClosePositionsOnKillSwitch { get; set; } = false;
}
```

### `backend/src/OandaTrader.Infrastructure/Configuration/OandaOptions.cs`

```csharp
namespace OandaTrader.Infrastructure.Configuration;

public sealed class OandaOptions
{
    public string Environment { get; set; } = "practice";
    public string AccountId { get; set; } = "";
    public string Token { get; set; } = "";
    public string PracticeRestBaseUrl { get; set; } = "https://api-fxpractice.oanda.com";
    public string LiveRestBaseUrl { get; set; } = "https://api-fxtrade.oanda.com";
}
```

### `backend/src/OandaTrader.Infrastructure/Persistence/FileAuditStore.cs`

```csharp
using System.Text.Json;
using OandaTrader.Application;

namespace OandaTrader.Infrastructure.Persistence;

public sealed class FileAuditStore : IAuditStore
{
    private readonly string _path = Path.Combine(AppContext.BaseDirectory, "audit.log");

    public async Task AppendAsync(string eventType, object payload, CancellationToken ct)
    {
        var line = JsonSerializer.Serialize(new { ts = DateTimeOffset.UtcNow, eventType, payload });
        await File.AppendAllTextAsync(_path, line + Environment.NewLine, ct);
    }

    public async Task<IReadOnlyList<string>> GetRecentAsync(int count, CancellationToken ct)
    {
        if (!File.Exists(_path)) return Array.Empty<string>();
        var lines = await File.ReadAllLinesAsync(_path, ct);
        return lines.TakeLast(count).ToArray();
    }
}
```

### `backend/src/OandaTrader.Infrastructure/Persistence/FileKillSwitchStore.cs`

```csharp
using OandaTrader.Application;
using OandaTrader.Domain;

namespace OandaTrader.Infrastructure.Persistence;

public sealed class FileKillSwitchStore : IKillSwitchStore
{
    private readonly string _path = Path.Combine(AppContext.BaseDirectory, "kill-switch.txt");

    public async Task<KillSwitchState> GetStateAsync(CancellationToken ct)
    {
        if (!File.Exists(_path)) return KillSwitchState.Disengaged;
        var raw = await File.ReadAllTextAsync(_path, ct);
        return Enum.TryParse<KillSwitchState>(raw, true, out var state) ? state : KillSwitchState.Disengaged;
    }

    public Task SetStateAsync(KillSwitchState state, CancellationToken ct)
        => File.WriteAllTextAsync(_path, state.ToString(), ct);
}
```

### `backend/src/OandaTrader.Infrastructure/Persistence/InMemoryStateStore.cs`

```csharp
using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Application.Strategies;

namespace OandaTrader.Infrastructure.Persistence;

public sealed class InMemoryStrategyRegistry : IStrategyRegistry
{
    private readonly Dictionary<string, IStrategy> _strategies;

    public InMemoryStrategyRegistry()
    {
        _strategies = new(StringComparer.OrdinalIgnoreCase)
        {
            ["sma-crossover"] = new SmaCrossoverStrategy(),
            ["breakout-atr"] = new BreakoutAtrStrategy()
        };
    }

    public IReadOnlyList<string> Names => _strategies.Keys.OrderBy(x => x).ToArray();

    public IStrategy Resolve(string name)
        => _strategies.TryGetValue(name, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"Unknown strategy '{name}'");
}

public sealed class InMemoryBrokerState
{
    public AccountSnapshot Account { get; set; } = new()
    {
        AccountId = "PAPER-001",
        Balance = 10_000m,
        Equity = 10_000m,
        MarginAvailable = 10_000m,
        MarginUsed = 0m,
        StartingBalance = 10_000m,
        UnrealizedPnL = 0m,
        DailyPnL = 0m,
        OpenTrades = new(),
        Positions = new()
    };
}
```

### `backend/src/OandaTrader.Infrastructure/Brokers/PaperBrokerGateway.cs`

```csharp
using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Infrastructure.Persistence;

namespace OandaTrader.Infrastructure.Brokers;

public sealed class PaperBrokerGateway : IBrokerGateway
{
    private readonly InMemoryBrokerState _state;
    public TradingMode Mode => TradingMode.Paper;

    public PaperBrokerGateway(InMemoryBrokerState state)
    {
        _state = state;
    }

    public Task<AccountSnapshot> GetAccountSnapshotAsync(CancellationToken ct)
        => Task.FromResult(_state.Account);

    public Task<IReadOnlyList<PriceTick>> GetLatestPricesAsync(IEnumerable<string> instruments, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var list = instruments.Select(i => i switch
        {
            "EUR_USD" => new PriceTick(i, 1.0830m, 1.0831m, now),
            "GBP_USD" => new PriceTick(i, 1.2710m, 1.2712m, now),
            "XAU_USD" => new PriceTick(i, 2148.20m, 2148.60m, now),
            _ => new PriceTick(i, 1m, 1.01m, now)
        }).ToList();

        return Task.FromResult<IReadOnlyList<PriceTick>>(list);
    }

    public Task<OrderResult> PlaceOrderAsync(OrderRequest request, CancellationToken ct)
    {
        var fillPrice = request.Price ?? 1m;
        if (request.Instrument == "EUR_USD") fillPrice = 1.0831m;
        if (request.Instrument == "GBP_USD") fillPrice = 1.2712m;
        if (request.Instrument == "XAU_USD") fillPrice = 2148.60m;

        var trade = new OpenTrade
        {
            TradeId = Guid.NewGuid().ToString("N"),
            Instrument = request.Instrument,
            Side = request.Side,
            Units = request.Units,
            EntryPrice = fillPrice,
            StopLoss = request.StopLoss,
            TakeProfit = request.TakeProfit,
            UnrealizedPnL = 0m,
            OpenedAt = DateTimeOffset.UtcNow
        };

        _state.Account.OpenTrades.Add(trade);
        _state.Account.MarginUsed += fillPrice * request.Units * 0.02m;
        _state.Account.MarginAvailable = _state.Account.Equity - _state.Account.MarginUsed;

        return Task.FromResult(new OrderResult
        {
            OrderId = Guid.NewGuid().ToString("N"),
            BrokerTradeId = trade.TradeId,
            State = OrderState.Filled,
            BrokerMessage = "Paper fill",
            FillPrice = fillPrice,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public Task<IReadOnlyList<OpenTrade>> GetOpenTradesAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<OpenTrade>>(_state.Account.OpenTrades.ToList());

    public Task CloseTradeAsync(string tradeId, CancellationToken ct)
    {
        _state.Account.OpenTrades.RemoveAll(x => x.TradeId == tradeId);
        return Task.CompletedTask;
    }

    public Task EngageKillSwitchCloseAllAsync(CancellationToken ct)
    {
        _state.Account.OpenTrades.Clear();
        _state.Account.MarginUsed = 0m;
        _state.Account.MarginAvailable = _state.Account.Equity;
        return Task.CompletedTask;
    }
}
```

### `backend/src/OandaTrader.Infrastructure/Brokers/OandaBrokerGateway.cs`

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Infrastructure.Configuration;

namespace OandaTrader.Infrastructure.Brokers;

public sealed class OandaBrokerGateway : IBrokerGateway
{
    private readonly HttpClient _httpClient;
    private readonly OandaOptions _options;

    public TradingMode Mode => _options.Environment.Equals("live", StringComparison.OrdinalIgnoreCase)
        ? TradingMode.Live
        : TradingMode.Practice;

    public OandaBrokerGateway(HttpClient httpClient, OandaOptions options)
    {
        _httpClient = httpClient;
        _options = options;

        var baseUrl = Mode == TradingMode.Live ? _options.LiveRestBaseUrl : _options.PracticeRestBaseUrl;
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<AccountSnapshot> GetAccountSnapshotAsync(CancellationToken ct)
    {
        var res = await _httpClient.GetAsync($"/v3/accounts/{_options.AccountId}/summary", ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var account = doc.RootElement.GetProperty("account");

        return new AccountSnapshot
        {
            AccountId = _options.AccountId,
            Balance = decimal.Parse(account.GetProperty("balance").GetString() ?? "0"),
            Equity = decimal.Parse(account.GetProperty("NAV").GetString() ?? "0"),
            MarginUsed = decimal.Parse(account.GetProperty("marginUsed").GetString() ?? "0"),
            MarginAvailable = decimal.Parse(account.GetProperty("marginAvailable").GetString() ?? "0"),
            UnrealizedPnL = decimal.Parse(account.GetProperty("unrealizedPL").GetString() ?? "0"),
            StartingBalance = decimal.Parse(account.GetProperty("balance").GetString() ?? "0"),
            DailyPnL = 0m,
            OpenTrades = new(),
            Positions = new()
        };
    }

    public async Task<IReadOnlyList<PriceTick>> GetLatestPricesAsync(IEnumerable<string> instruments, CancellationToken ct)
    {
        var query = string.Join(",", instruments);
        var res = await _httpClient.GetAsync($"/v3/accounts/{_options.AccountId}/pricing?instruments={query}", ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var list = new List<PriceTick>();
        foreach (var p in doc.RootElement.GetProperty("prices").EnumerateArray())
        {
            var instrument = p.GetProperty("instrument").GetString()!;
            var bid = decimal.Parse(p.GetProperty("bids")[0].GetProperty("price").GetString() ?? "0");
            var ask = decimal.Parse(p.GetProperty("asks")[0].GetProperty("price").GetString() ?? "0");
            var time = DateTimeOffset.Parse(p.GetProperty("time").GetString()!);
            list.Add(new PriceTick(instrument, bid, ask, time));
        }

        return list;
    }

    public async Task<OrderResult> PlaceOrderAsync(OrderRequest request, CancellationToken ct)
    {
        var units = request.Side == TradeSide.Buy ? request.Units : -request.Units;

        var payload = new
        {
            order = new
            {
                units = units.ToString(System.Globalization.CultureInfo.InvariantCulture),
                instrument = request.Instrument,
                timeInForce = "FOK",
                type = "MARKET",
                positionFill = "DEFAULT",
                stopLossOnFill = request.StopLoss is null ? null : new { price = request.StopLoss.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                takeProfitOnFill = request.TakeProfit is null ? null : new { price = request.TakeProfit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            }
        };

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"/v3/accounts/{_options.AccountId}/orders");
        msg.Headers.Add("ClientRequestID", request.ClientOrderId);
        msg.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var res = await _httpClient.SendAsync(msg, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            return new OrderResult
            {
                OrderId = request.ClientOrderId,
                State = OrderState.Rejected,
                BrokerMessage = body,
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        return new OrderResult
        {
            OrderId = request.ClientOrderId,
            State = OrderState.Filled,
            BrokerMessage = body,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public async Task<IReadOnlyList<OpenTrade>> GetOpenTradesAsync(CancellationToken ct)
    {
        var res = await _httpClient.GetAsync($"/v3/accounts/{_options.AccountId}/openTrades", ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var list = new List<OpenTrade>();
        foreach (var t in doc.RootElement.GetProperty("trades").EnumerateArray())
        {
            var units = decimal.Parse(t.GetProperty("currentUnits").GetString() ?? "0");
            list.Add(new OpenTrade
            {
                TradeId = t.GetProperty("id").GetString()!,
                Instrument = t.GetProperty("instrument").GetString()!,
                Side = units >= 0 ? TradeSide.Buy : TradeSide.Sell,
                Units = Math.Abs(units),
                EntryPrice = decimal.Parse(t.GetProperty("price").GetString() ?? "0"),
                UnrealizedPnL = decimal.Parse(t.GetProperty("unrealizedPL").GetString() ?? "0"),
                OpenedAt = DateTimeOffset.Parse(t.GetProperty("openTime").GetString()!),
                StopLoss = null,
                TakeProfit = null
            });
        }
        return list;
    }

    public async Task CloseTradeAsync(string tradeId, CancellationToken ct)
    {
        var res = await _httpClient.PutAsync($"/v3/accounts/{_options.AccountId}/trades/{tradeId}/close", content: null, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task EngageKillSwitchCloseAllAsync(CancellationToken ct)
    {
        var trades = await GetOpenTradesAsync(ct);
        foreach (var trade in trades)
            await CloseTradeAsync(trade.TradeId, ct);
    }
}
```

### `backend/src/OandaTrader.Infrastructure/MarketData/OandaMarketDataClient.cs`

```csharp
using System.Text.Json;
using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Infrastructure.Configuration;

namespace OandaTrader.Infrastructure.MarketData;

public sealed class OandaMarketDataClient : IMarketDataClient
{
    private readonly HttpClient _httpClient;
    private readonly OandaOptions _options;

    public OandaMarketDataClient(HttpClient httpClient, OandaOptions options)
    {
        _httpClient = httpClient;
        _options = options;
        var baseUrl = _options.Environment.Equals("live", StringComparison.OrdinalIgnoreCase)
            ? _options.LiveRestBaseUrl
            : _options.PracticeRestBaseUrl;
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.Token);
    }

    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(string instrument, string granularity, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var url = $"/v3/instruments/{instrument}/candles?price=M&granularity={granularity}&from={Uri.EscapeDataString(from.UtcDateTime.ToString("O"))}&to={Uri.EscapeDataString(to.UtcDateTime.ToString("O"))}";
        var res = await _httpClient.GetAsync(url, ct);
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var candles = new List<Candle>();
        foreach (var c in doc.RootElement.GetProperty("candles").EnumerateArray())
        {
            var mid = c.GetProperty("mid");
            candles.Add(new Candle(
                instrument,
                granularity,
                DateTimeOffset.Parse(c.GetProperty("time").GetString()!),
                decimal.Parse(mid.GetProperty("o").GetString() ?? "0"),
                decimal.Parse(mid.GetProperty("h").GetString() ?? "0"),
                decimal.Parse(mid.GetProperty("l").GetString() ?? "0"),
                decimal.Parse(mid.GetProperty("c").GetString() ?? "0"),
                c.GetProperty("volume").GetInt64(),
                c.GetProperty("complete").GetBoolean()));
        }
        return candles;
    }
}
```

### `backend/src/OandaTrader.Infrastructure/MarketData/SimulatedMarketDataClient.cs`

```csharp
using OandaTrader.Application;
using OandaTrader.Domain;

namespace OandaTrader.Infrastructure.MarketData;

public sealed class SimulatedMarketDataClient : IMarketDataClient
{
    public Task<IReadOnlyList<Candle>> GetCandlesAsync(string instrument, string granularity, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var list = new List<Candle>();
        var rand = new Random(42);
        decimal price = instrument == "XAU_USD" ? 2100m : 1.10m;
        var t = from;
        while (t < to)
        {
            var drift = (decimal)(rand.NextDouble() - 0.5) * (instrument == "XAU_USD" ? 10m : 0.01m);
            var open = price;
            var close = price + drift;
            var high = Math.Max(open, close) + Math.Abs(drift * 0.5m);
            var low = Math.Min(open, close) - Math.Abs(drift * 0.5m);
            list.Add(new Candle(instrument, granularity, t, open, high, low, close, 1000, true));
            price = close;
            t = granularity switch
            {
                "M1" => t.AddMinutes(1),
                "M5" => t.AddMinutes(5),
                "H1" => t.AddHours(1),
                _ => t.AddMinutes(5)
            };
        }
        return Task.FromResult<IReadOnlyList<Candle>>(list);
    }
}
```

### `backend/src/OandaTrader.Infrastructure/DependencyInjection.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Infrastructure.Brokers;
using OandaTrader.Infrastructure.Configuration;
using OandaTrader.Infrastructure.MarketData;
using OandaTrader.Infrastructure.Persistence;

namespace OandaTrader.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTradingInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<TradingOptions>(config.GetSection("Trading"));
        services.Configure<OandaOptions>(config.GetSection("Oanda"));

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<OandaOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TradingOptions>>().Value);

        services.AddSingleton<IAuditStore, FileAuditStore>();
        services.AddSingleton<IKillSwitchStore, FileKillSwitchStore>();
        services.AddSingleton<InMemoryBrokerState>();
        services.AddSingleton<IStrategyRegistry, InMemoryStrategyRegistry>();

        services.AddHttpClient<OandaBrokerGateway>();
        services.AddHttpClient<OandaMarketDataClient>();

        services.AddSingleton<PaperBrokerGateway>();
        services.AddSingleton<SimulatedMarketDataClient>();

        services.AddSingleton<IBrokerGateway>(sp =>
        {
            var env = sp.GetRequiredService<OandaOptions>().Environment;
            return env.Equals("paper", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<PaperBrokerGateway>()
                : sp.GetRequiredService<OandaBrokerGateway>();
        });

        services.AddSingleton<IMarketDataClient>(sp =>
        {
            var env = sp.GetRequiredService<OandaOptions>().Environment;
            return env.Equals("paper", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<SimulatedMarketDataClient>()
                : sp.GetRequiredService<OandaMarketDataClient>();
        });

        services.AddSingleton<RiskGate>();
        services.AddSingleton<TradingEngine>();
        services.AddSingleton<ReconciliationService>();
        services.AddSingleton<BacktestService>();

        return services;
    }
}
```

---

## 7) API Project

### `backend/src/OandaTrader.Api/OandaTrader.Api.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\OandaTrader.Domain\OandaTrader.Domain.csproj" />
    <ProjectReference Include="..\OandaTrader.Application\OandaTrader.Application.csproj" />
    <ProjectReference Include="..\OandaTrader.Infrastructure\OandaTrader.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### `backend/src/OandaTrader.Api/Program.cs`

```csharp
using OandaTrader.Application;
using OandaTrader.Infrastructure;
using OandaTrader.Infrastructure.Configuration;
using OandaTrader.Api.Hubs;
using OandaTrader.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true));
});

builder.Services.AddTradingInfrastructure(builder.Configuration);
builder.Services.AddSingleton<DashboardBroadcaster>();
builder.Services.AddHostedService<HostedMarketLoop>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend");
app.MapControllers();
app.MapHub<TradingHub>("/hub/trading");

app.Run();
```

### `backend/src/OandaTrader.Api/Hubs/TradingHub.cs`

```csharp
using Microsoft.AspNetCore.SignalR;

namespace OandaTrader.Api.Hubs;

public sealed class TradingHub : Hub
{
}
```

### `backend/src/OandaTrader.Api/Models/ApiDtos.cs`

```csharp
namespace OandaTrader.Api.Models;

public sealed class RunStrategyDto
{
    public required string StrategyName { get; init; }
    public required string Instrument { get; init; }
    public required string Granularity { get; init; }
    public required string Mode { get; init; }
    public bool AcceptLiveRisk { get; init; }
}

public sealed class KillSwitchDto
{
    public bool ClosePositions { get; init; }
}

public sealed class BacktestDto
{
    public required string StrategyName { get; init; }
    public required string Instrument { get; init; }
    public required string Granularity { get; init; }
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }
}
```

### `backend/src/OandaTrader.Api/Models/ConfigDtos.cs`

```csharp
namespace OandaTrader.Api.Models;

public sealed class TradingConfigDto
{
    public string DefaultInstrument { get; set; } = "XAU_USD";
    public string DefaultGranularity { get; set; } = "M5";
    public decimal MaxRiskPerTradeFraction { get; set; } = 0.01m;
    public decimal MaxDailyLossFraction { get; set; } = 0.03m;
    public int MaxTradesPerDayPerInstrument { get; set; } = 3;
    public decimal MaxSpread { get; set; } = 0.50m;
    public decimal MaxLeverage { get; set; } = 5m;
    public bool StopLossRequired { get; set; } = true;
    public bool ClosePositionsOnKillSwitch { get; set; } = false;
}
```

### `backend/src/OandaTrader.Api/Controllers/AccountController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using OandaTrader.Application;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController : ControllerBase
{
    private readonly IBrokerGateway _broker;

    public AccountController(IBrokerGateway broker)
    {
        _broker = broker;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _broker.GetAccountSnapshotAsync(ct));
}
```

### `backend/src/OandaTrader.Api/Controllers/PricesController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using OandaTrader.Application;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/prices")]
public sealed class PricesController : ControllerBase
{
    private readonly IBrokerGateway _broker;

    public PricesController(IBrokerGateway broker)
    {
        _broker = broker;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var prices = await _broker.GetLatestPricesAsync(new[] { "EUR_USD", "GBP_USD", "XAU_USD" }, ct);
        return Ok(prices);
    }
}
```

### `backend/src/OandaTrader.Api/Controllers/TradesController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using OandaTrader.Application;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/trades")]
public sealed class TradesController : ControllerBase
{
    private readonly IBrokerGateway _broker;

    public TradesController(IBrokerGateway broker)
    {
        _broker = broker;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _broker.GetOpenTradesAsync(ct));

    [HttpDelete("{tradeId}")]
    public async Task<IActionResult> Close(string tradeId, CancellationToken ct)
    {
        await _broker.CloseTradeAsync(tradeId, ct);
        return NoContent();
    }
}
```

### `backend/src/OandaTrader.Api/Controllers/ConfigController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OandaTrader.Api.Models;
using OandaTrader.Infrastructure.Configuration;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/config")]
public sealed class ConfigController : ControllerBase
{
    private readonly TradingOptions _options;

    public ConfigController(IOptions<TradingOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet]
    public IActionResult Get()
        => Ok(new TradingConfigDto
        {
            DefaultInstrument = _options.DefaultInstrument,
            DefaultGranularity = _options.DefaultGranularity,
            MaxRiskPerTradeFraction = _options.MaxRiskPerTradeFraction,
            MaxDailyLossFraction = _options.MaxDailyLossFraction,
            MaxTradesPerDayPerInstrument = _options.MaxTradesPerDayPerInstrument,
            MaxSpread = _options.MaxSpread,
            MaxLeverage = _options.MaxLeverage,
            StopLossRequired = _options.StopLossRequired,
            ClosePositionsOnKillSwitch = _options.ClosePositionsOnKillSwitch
        });
}
```

### `backend/src/OandaTrader.Api/Controllers/StrategyController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OandaTrader.Api.Models;
using OandaTrader.Application;
using OandaTrader.Domain;
using OandaTrader.Infrastructure.Configuration;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/strategy")]
public sealed class StrategyController : ControllerBase
{
    private readonly TradingEngine _engine;
    private readonly IStrategyRegistry _strategies;
    private readonly TradingOptions _options;

    public StrategyController(TradingEngine engine, IStrategyRegistry strategies, IOptions<TradingOptions> options)
    {
        _engine = engine;
        _strategies = strategies;
        _options = options.Value;
    }

    [HttpGet("list")]
    public IActionResult List() => Ok(_strategies.Names);

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] RunStrategyDto dto, CancellationToken ct)
    {
        var req = new StrategyRunRequest
        {
            StrategyName = dto.StrategyName,
            Instrument = dto.Instrument,
            Granularity = dto.Granularity,
            Mode = Enum.Parse<TradingMode>(dto.Mode, ignoreCase: true),
            AcceptLiveRisk = dto.AcceptLiveRisk
        };

        var result = await _engine.EvaluateAndMaybeTradeAsync(
            req,
            _options.MaxRiskPerTradeFraction,
            _options.MaxDailyLossFraction,
            _options.MaxTradesPerDayPerInstrument,
            _options.MaxSpread,
            _options.MaxLeverage,
            _options.StopLossRequired,
            ct);

        return Ok(result);
    }
}
```

### `backend/src/OandaTrader.Api/Controllers/KillSwitchController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OandaTrader.Api.Models;
using OandaTrader.Application;
using OandaTrader.Infrastructure.Configuration;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/kill-switch")]
public sealed class KillSwitchController : ControllerBase
{
    private readonly TradingEngine _engine;
    private readonly TradingOptions _options;

    public KillSwitchController(TradingEngine engine, IOptions<TradingOptions> options)
    {
        _engine = engine;
        _options = options.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Engage([FromBody] KillSwitchDto dto, CancellationToken ct)
    {
        await _engine.EngageKillSwitchAsync(dto.ClosePositions || _options.ClosePositionsOnKillSwitch, ct);
        return Ok(new { status = "engaged" });
    }
}
```

### `backend/src/OandaTrader.Api/Controllers/BacktestController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using OandaTrader.Api.Models;
using OandaTrader.Application;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/backtest")]
public sealed class BacktestController : ControllerBase
{
    private readonly BacktestService _backtests;

    public BacktestController(BacktestService backtests)
    {
        _backtests = backtests;
    }

    [HttpPost]
    public async Task<IActionResult> Run([FromBody] BacktestDto dto, CancellationToken ct)
        => Ok(await _backtests.RunAsync(dto.StrategyName, dto.Instrument, dto.Granularity, dto.From, dto.To, ct));
}
```

### `backend/src/OandaTrader.Api/Services/DashboardBroadcaster.cs`

```csharp
using Microsoft.AspNetCore.SignalR;
using OandaTrader.Api.Hubs;
using OandaTrader.Application;

namespace OandaTrader.Api.Services;

public sealed class DashboardBroadcaster
{
    private readonly IHubContext<TradingHub> _hub;
    private readonly IBrokerGateway _broker;
    private readonly IAuditStore _audit;

    public DashboardBroadcaster(IHubContext<TradingHub> hub, IBrokerGateway broker, IAuditStore audit)
    {
        _hub = hub;
        _broker = broker;
        _audit = audit;
    }

    public async Task BroadcastSnapshotAsync(CancellationToken ct)
    {
        var account = await _broker.GetAccountSnapshotAsync(ct);
        var prices = await _broker.GetLatestPricesAsync(new[] { "EUR_USD", "GBP_USD", "XAU_USD" }, ct);
        var trades = await _broker.GetOpenTradesAsync(ct);
        var logs = await _audit.GetRecentAsync(50, ct);

        await _hub.Clients.All.SendAsync("snapshot", new
        {
            account,
            prices,
            trades,
            logs,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }
}
```

### `backend/src/OandaTrader.Api/Services/HostedMarketLoop.cs`

```csharp
using OandaTrader.Application;

namespace OandaTrader.Api.Services;

public sealed class HostedMarketLoop : BackgroundService
{
    private readonly DashboardBroadcaster _broadcaster;
    private readonly ReconciliationService _reconciliation;
    private readonly ILogger<HostedMarketLoop> _logger;

    public HostedMarketLoop(DashboardBroadcaster broadcaster, ReconciliationService reconciliation, ILogger<HostedMarketLoop> logger)
    {
        _broadcaster = broadcaster;
        _reconciliation = reconciliation;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _broadcaster.BroadcastSnapshotAsync(stoppingToken);
                await _reconciliation.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hosted market loop failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}
```

### `backend/src/OandaTrader.Api/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Oanda": {
    "Environment": "paper",
    "AccountId": "YOUR_ACCOUNT_ID",
    "Token": "SET_VIA_ENV_OR_SECRETS",
    "PracticeRestBaseUrl": "https://api-fxpractice.oanda.com",
    "LiveRestBaseUrl": "https://api-fxtrade.oanda.com"
  },
  "Trading": {
    "DefaultInstrument": "XAU_USD",
    "DefaultGranularity": "M5",
    "MaxRiskPerTradeFraction": 0.01,
    "MaxDailyLossFraction": 0.03,
    "MaxTradesPerDayPerInstrument": 3,
    "MaxSpread": 0.50,
    "MaxLeverage": 5,
    "StopLossRequired": true,
    "ClosePositionsOnKillSwitch": false
  }
}
```

---

## 8) Tests Project

### `backend/src/OandaTrader.Tests/OandaTrader.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\OandaTrader.Domain\OandaTrader.Domain.csproj" />
    <ProjectReference Include="..\OandaTrader.Application\OandaTrader.Application.csproj" />
  </ItemGroup>
</Project>
```

### `backend/src/OandaTrader.Tests/RiskGateTests.cs`

```csharp
using OandaTrader.Application;
using OandaTrader.Domain;

namespace OandaTrader.Tests;

public sealed class RiskGateTests
{
    [Fact]
    public void Blocks_When_Spread_Too_High()
    {
        var gate = new RiskGate();
        var account = MakeAccount();
        var signal = new Signal("test", "XAU_USD", SignalAction.Buy, 2000m, 1995m, 2010m, 1m, "x", DateTimeOffset.UtcNow);
        var tick = new PriceTick("XAU_USD", 2000m, 2001m, DateTimeOffset.UtcNow);

        var result = gate.Evaluate(account, signal, tick, 0, 0.01m, 0.03m, 3, 0.50m, 5m, true);
        Assert.False(result.Approved);
    }

    [Fact]
    public void Blocks_When_Max_Trades_Exceeded()
    {
        var gate = new RiskGate();
        var account = MakeAccount();
        var signal = new Signal("test", "EUR_USD", SignalAction.Buy, 1.10m, 1.095m, 1.11m, 1000m, "x", DateTimeOffset.UtcNow);
        var tick = new PriceTick("EUR_USD", 1.10m, 1.1001m, DateTimeOffset.UtcNow);

        var result = gate.Evaluate(account, signal, tick, 3, 0.01m, 0.03m, 3, 0.001m, 5m, true);
        Assert.False(result.Approved);
    }

    private static AccountSnapshot MakeAccount() => new()
    {
        AccountId = "A",
        Balance = 10000m,
        Equity = 10000m,
        MarginAvailable = 10000m,
        MarginUsed = 0m,
        StartingBalance = 10000m,
        DailyPnL = 0m,
        UnrealizedPnL = 0m,
        OpenTrades = new(),
        Positions = new()
    };
}
```

### `backend/src/OandaTrader.Tests/StrategyTests.cs`

```csharp
using OandaTrader.Application.Strategies;
using OandaTrader.Domain;

namespace OandaTrader.Tests;

public sealed class StrategyTests
{
    [Fact]
    public void Sma_Crossover_Returns_Buy_When_Fast_Above_Slow()
    {
        var strategy = new SmaCrossoverStrategy();
        var candles = Enumerable.Range(1, 30)
            .Select(i => new Candle("EUR_USD", "M5", DateTimeOffset.UtcNow.AddMinutes(i), 1, 1, 1, 1 + i * 0.001m, 100, true))
            .ToList();

        var signal = strategy.GenerateSignal(new StrategyContext
        {
            Instrument = "EUR_USD",
            Granularity = "M5",
            Candles = candles,
            Now = DateTimeOffset.UtcNow
        });

        Assert.Equal(SignalAction.Buy, signal.Action);
    }
}
```

---

## 9) Frontend

### `frontend/package.json`

```json
{
  "name": "oanda-trader-frontend",
  "private": true,
  "version": "0.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc && vite build",
    "preview": "vite preview"
  },
  "dependencies": {
    "@microsoft/signalr": "^8.0.7",
    "react": "^18.3.1",
    "react-dom": "^18.3.1"
  },
  "devDependencies": {
    "@types/react": "^18.3.3",
    "@types/react-dom": "^18.3.0",
    "@vitejs/plugin-react": "^4.3.1",
    "typescript": "^5.5.4",
    "vite": "^5.4.2"
  }
}
```

### `frontend/vite.config.ts`

```ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173
  }
})
```

### `frontend/src/types.ts`

```ts
export type AccountSnapshot = {
  accountId: string
  balance: number
  equity: number
  marginUsed: number
  marginAvailable: number
  unrealizedPnL: number
  dailyPnL: number
  startingBalance: number
  drawdownFraction: number
}

export type PriceTick = {
  instrument: string
  bid: number
  ask: number
  mid: number
  spread: number
  timestamp: string
}

export type OpenTrade = {
  tradeId: string
  instrument: string
  side: string
  units: number
  entryPrice: number
  stopLoss?: number
  takeProfit?: number
  unrealizedPnL: number
  openedAt: string
}

export type Snapshot = {
  account: AccountSnapshot
  prices: PriceTick[]
  trades: OpenTrade[]
  logs: string[]
  timestamp: string
}
```

### `frontend/src/api.ts`

```ts
const API = 'http://localhost:5000/api'

export async function getJson<T>(path: string): Promise<T> {
  const res = await fetch(`${API}${path}`)
  if (!res.ok) throw new Error(`HTTP ${res.status}`)
  return await res.json()
}

export async function postJson<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(`${API}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
  })
  if (!res.ok) throw new Error(`HTTP ${res.status}`)
  return await res.json()
}

export async function deleteApi(path: string): Promise<void> {
  const res = await fetch(`${API}${path}`, { method: 'DELETE' })
  if (!res.ok) throw new Error(`HTTP ${res.status}`)
}
```

### `frontend/src/hooks/useTradingHub.ts`

```ts
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
```

### `frontend/src/components/Header.tsx`

```tsx
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
```

### `frontend/src/components/AccountCard.tsx`

```tsx
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
```

### `frontend/src/components/PriceGrid.tsx`

```tsx
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
```

### `frontend/src/components/OpenTradesTable.tsx`

```tsx
import { OpenTrade } from '../types'
import { deleteApi } from '../api'

export function OpenTradesTable({ trades, onRefresh }: { trades: OpenTrade[]; onRefresh?: () => void }) {
  async function closeTrade(id: string) {
    await deleteApi(`/trades/${id}`)
    onRefresh?.()
  }

  return (
    <div className="card">
      <h3>Open Trades</h3>
      <table>
        <thead>
          <tr>
            <th>Instrument</th>
            <th>Side</th>
            <th>Units</th>
            <th>Entry</th>
            <th>SL</th>
            <th>TP</th>
            <th>UPnL</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {trades.map(t => (
            <tr key={t.tradeId}>
              <td>{t.instrument}</td>
              <td>{t.side}</td>
              <td>{t.units}</td>
              <td>{t.entryPrice}</td>
              <td>{t.stopLoss ?? '-'}</td>
              <td>{t.takeProfit ?? '-'}</td>
              <td>{t.unrealizedPnL}</td>
              <td><button onClick={() => closeTrade(t.tradeId)}>Close</button></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
```

### `frontend/src/components/RiskPanel.tsx`

```tsx
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
```

### `frontend/src/components/KillSwitchPanel.tsx`

```tsx
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
```

### `frontend/src/components/StrategyPanel.tsx`

```tsx
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
      <pre>{result}</pre>
    </div>
  )
}
```

### `frontend/src/components/ConfigEditor.tsx`

```tsx
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
```

### `frontend/src/components/AuditLogPanel.tsx`

```tsx
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
```

### `frontend/src/components/EquityPlaceholderChart.tsx`

```tsx
export function EquityPlaceholderChart() {
  return (
    <div className="card chart">
      <h3>Equity Curve</h3>
      <div className="chart-placeholder">Add Recharts or TradingView Lightweight Charts here.</div>
    </div>
  )
}
```

### `frontend/src/App.tsx`

```tsx
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
```

### `frontend/src/main.tsx`

```tsx
import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './styles.css'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
```

### `frontend/src/styles.css`

```css
:root {
  color-scheme: dark;
  font-family: Inter, system-ui, Arial, sans-serif;
  background: #0b1020;
  color: #e8edf7;
}

body, html, #root {
  margin: 0;
  min-height: 100%;
  background: #0b1020;
}

.app-shell {
  padding: 24px;
  max-width: 1400px;
  margin: 0 auto;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.status {
  padding: 10px 14px;
  border-radius: 999px;
  font-weight: 700;
}

.status.ok { background: #0f5132; }
.status.bad { background: #7a1f1f; }

.grid {
  display: grid;
  gap: 16px;
}

.grid.two { grid-template-columns: repeat(2, minmax(0, 1fr)); }
.grid.three { grid-template-columns: repeat(3, minmax(0, 1fr)); }
.layout-top { grid-template-columns: 2fr 1fr 1fr; }
.layout-bottom { grid-template-columns: 1.5fr 1fr; }

.card {
  background: #151d33;
  border: 1px solid #22304f;
  border-radius: 16px;
  padding: 18px;
  margin-bottom: 16px;
}

.card h3 { margin-top: 0; }
.card.danger { border-color: #8f2d2d; }

.price-box {
  background: #10182c;
  border-radius: 12px;
  padding: 14px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th, td {
  padding: 10px;
  border-bottom: 1px solid #253453;
  text-align: left;
}

button, select {
  background: #243657;
  color: white;
  border: 1px solid #365282;
  border-radius: 10px;
  padding: 10px 12px;
}

button:hover { cursor: pointer; opacity: 0.95; }
.danger-btn { background: #7a1f1f; border-color: #a13535; }
.row { display: flex; gap: 12px; }

.log-box {
  max-height: 280px;
  overflow: auto;
  font-family: ui-monospace, SFMono-Regular, monospace;
  background: #0f1527;
  border-radius: 12px;
  padding: 12px;
}

.chart-placeholder {
  height: 180px;
  display: grid;
  place-items: center;
  background: linear-gradient(180deg, #0f1830, #121e38);
  border-radius: 12px;
  color: #93a6c8;
}

pre {
  white-space: pre-wrap;
  word-break: break-word;
  background: #0f1527;
  border-radius: 12px;
  padding: 12px;
}

label {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
```

---

## 10) README

### `README.md`

````md
# Oanda Trader Advanced

Advanced local trading workstation using React + ASP.NET Core.

## Features
- React dashboard
- ASP.NET API + SignalR live updates
- Paper broker
- OANDA practice/live scaffold
- Strategy runner
- Risk gate
- Kill switch
- Audit log
- Backtest endpoint scaffold

## Safety Defaults
- Default environment is `paper`
- Live mode is blocked unless explicit acceptance is passed
- Kill switch is persisted to disk
- Stop loss required by default
- Max 3 trades/day/instrument
- 35% drawdown hard stop

## Setup

### Backend
```bash
cd backend

dotnet new sln -n OandaTrader
cd src

dotnet new classlib -n OandaTrader.Domain

dotnet new classlib -n OandaTrader.Application

dotnet new classlib -n OandaTrader.Infrastructure

dotnet new webapi -n OandaTrader.Api

dotnet new xunit -n OandaTrader.Tests
````

Replace generated files with the files in this repo, then:

```bash
cd ..
dotnet sln add src/OandaTrader.Domain/OandaTrader.Domain.csproj
dotnet sln add src/OandaTrader.Application/OandaTrader.Application.csproj
dotnet sln add src/OandaTrader.Infrastructure/OandaTrader.Infrastructure.csproj
dotnet sln add src/OandaTrader.Api/OandaTrader.Api.csproj
dotnet sln add src/OandaTrader.Tests/OandaTrader.Tests.csproj

dotnet restore
dotnet build
dotnet test

dotnet run --project src/OandaTrader.Api
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## OANDA Configuration

Use environment variables or user secrets for the token.

Examples:

```bash
setx Oanda__Token "your_token_here"
setx Oanda__AccountId "your_account_id_here"
```

macOS/Linux:

```bash
export Oanda__Token="your_token_here"
export Oanda__AccountId="your_account_id_here"
```

## Recommended Next Upgrades

* persistent database (SQLite/Postgres)
* proper performance analytics
* real order reconciliation by last transaction id
* streaming price ingestion
* editable config persistence
* equity and PnL charts
* auth for dashboard
* deployment via Docker Compose

```

---

## 11) What to build next

This scaffold is a solid base, but it is still a scaffold. The next highest-value upgrades are:

1. **Editable config persistence** instead of read-only config.
2. **Real candle cache and historical store** in SQLite.
3. **SignalR push for order lifecycle events** separate from snapshot refreshes.
4. **Real equity curve and PnL chart** with Recharts.
5. **OANDA streaming prices** instead of polling only.
6. **Transaction-based reconciliation** for production-grade OANDA state sync.
7. **Authentication** before you expose this outside localhost.

---

## 12) Operational safety notes

- Keep default environment set to `paper`.
- Do not let the UI toggle to live without a second explicit confirmation step.
- Require an `AcceptLiveRisk` boolean and a typed confirmation string before enabling live order routes.
- Never store live tokens in source control.
- Log every signal, risk decision, order request, and broker response.
- Do not treat this scaffold as ready for unmanaged live trading without more hardening.

```
