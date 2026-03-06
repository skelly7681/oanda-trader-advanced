#!/bin/bash

echo "Stopping Oanda Trader Advanced..."

# Kill backend
BACKEND_PID=$(pgrep -f "OandaTrader.Api")

if [ ! -z "$BACKEND_PID" ]; then
  echo "Stopping backend (PID $BACKEND_PID)..."
  kill $BACKEND_PID
else
  echo "Backend not running"
fi

# Kill frontend (Vite)
FRONTEND_PID=$(pgrep -f "vite")

if [ ! -z "$FRONTEND_PID" ]; then
  echo "Stopping frontend (PID $FRONTEND_PID)..."
  kill $FRONTEND_PID
else
  echo "Frontend not running"
fi

echo "System stopped."