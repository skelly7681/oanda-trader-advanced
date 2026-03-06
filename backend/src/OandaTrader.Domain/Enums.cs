namespace OandaTrader.Domain;

public enum TradeSide { Buy, Sell }
public enum SignalAction { Hold, Buy, Sell }
public enum OrderType { Market, Limit, Stop }
public enum OrderState { Created, Submitted, Filled, Rejected, Managed, Closed, Cancelled }
public enum TradingMode { Paper, Practice, Live }
public enum KillSwitchState { Disengaged, Engaged }