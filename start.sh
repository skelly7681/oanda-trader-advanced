#!/bin/bash

echo "Starting Oanda Trader Advanced..."

ROOT=~/Documents/Oanda-trader-advanced

echo ""
echo "Starting backend (.NET API)..."

cd "$ROOT/backend/src/OandaTrader.Api" || exit

dotnet run --urls http://localhost:5000 > "$ROOT/backend.log" 2>&1 &

BACKEND_PID=$!

echo "Backend running on http://localhost:5000"
echo "Swagger: http://localhost:5000/swagger"

echo ""
echo "Starting frontend (React + Vite)..."

cd "$ROOT/frontend" || exit

npm run dev > "$ROOT/frontend.log" 2>&1 &

FRONTEND_PID=$!

echo "Frontend running on http://localhost:5173"

echo ""
echo "SYSTEM STARTED"
echo "-----------------------------"
echo "Backend PID: $BACKEND_PID"
echo "Frontend PID: $FRONTEND_PID"
echo ""
echo "Open:"
echo "http://localhost:5173"
echo ""
echo "Logs:"
echo "backend.log"
echo "frontend.log"