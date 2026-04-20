#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
API_DIR="$REPO_DIR/src/EffectHub.Api"
GUI_DIR="$REPO_DIR/src/EffectHub.Desktop"

API_PID=""
GUI_PID=""

cleanup() {
  echo ""
  echo "⏹  Shutting down..."
  [[ -n "$GUI_PID" ]] && kill "$GUI_PID" 2>/dev/null && echo "   GUI stopped"
  [[ -n "$API_PID" ]] && kill "$API_PID" 2>/dev/null && echo "   API stopped"
  wait 2>/dev/null
  echo "✅ Done"
}
trap cleanup EXIT INT TERM

echo "🚀 Starting EffectHub"
echo ""

# --- API ---
echo "🔧 Starting API (http://localhost:5120)..."
dotnet run --project "$API_DIR" &
API_PID=$!

# Wait a bit for the API to be ready
for i in {1..20}; do
  if curl -sf http://localhost:5120/ >/dev/null 2>&1 || curl -sf http://localhost:5120/health >/dev/null 2>&1; then
    echo "✅ API ready"
    break
  fi
  if ! kill -0 "$API_PID" 2>/dev/null; then
    echo "❌ API failed to start"
    exit 1
  fi
  sleep 0.5
done

# --- GUI ---
echo "🖥  Starting GUI..."
dotnet run --project "$GUI_DIR" &
GUI_PID=$!

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  API  → http://localhost:5120"
echo "  GUI  → running (PID $GUI_PID)"
echo "  Press Ctrl+C to stop both"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

wait
