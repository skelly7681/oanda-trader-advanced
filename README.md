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