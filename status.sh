#!/bin/bash

echo "Oanda Trader Advanced Status"
echo "--------------------------------"

BACKEND_PID=$(pgrep -f "OandaTrader.Api")
FRONTEND_PID=$(pgrep -f "vite")

if [ ! -z "$BACKEND_PID" ]; then
  echo "Backend: RUNNING (PID $BACKEND_PID)"
else
  echo "Backend: STOPPED"
fi

if [ ! -z "$FRONTEND_PID" ]; then
  echo "Frontend: RUNNING (PID $FRONTEND_PID)"
else
  echo "Frontend: STOPPED"
fi

echo ""
echo "Endpoints:"
echo "Frontend: http://localhost:5173"
echo "API:      http://localhost:5000"
echo "Swagger:  http://localhost:5000/swagger"