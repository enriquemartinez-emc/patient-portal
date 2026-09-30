#!/usr/bin/env bash
# One command for local development: Postgres and Keycloak run in Docker, the API and the web app
# run on this machine with hot reload.
#
#   ./dev.sh          start everything; Ctrl+C stops the API and the web app
#   ./dev.sh down     also stop Postgres and Keycloak
#   ./dev.sh reset    stop everything and delete the database
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"

case "${1:-up}" in
  up) ;;
  down) docker compose down; exit 0 ;;
  reset) docker compose down -v; exit 0 ;;
  *) echo "Usage: ./dev.sh [down|reset]" >&2; exit 1 ;;
esac

for tool in docker dotnet pnpm; do
  command -v "$tool" >/dev/null || { echo "$tool is required but was not found." >&2; exit 1; }
done

docker info >/dev/null 2>&1 || { echo "Docker is not running. Start Docker Desktop (or the docker service) and run ./dev.sh again." >&2; exit 1; }

[ -f .env ] || { cp .env.example .env; echo "Created .env from .env.example"; }
[ -f web/.env.local ] || { cp web/.env.example web/.env.local; echo "Created web/.env.local from web/.env.example"; }

port_in_use() { (exec 3<>"/dev/tcp/127.0.0.1/$1") 2>/dev/null; }
for port in 3000 5246; do
  if port_in_use "$port"; then
    echo "Port $port is already in use. Is another ./dev.sh (or a dev server) still running?" >&2
    exit 1
  fi
done

echo "Starting Postgres, Keycloak and the database migrations..."
docker compose stop web api >/dev/null 2>&1 || true # the containerised copies use the same ports
docker compose up -d --wait postgres keycloak
migration_log=$(docker compose run --rm --quiet-pull migrations 2>&1) || { echo "$migration_log" >&2; exit 1; }

[ -d web/node_modules ] || (cd web && pnpm install)

# Job control gives each app its own process group, so it can be stopped as a whole.
set -m
prefix() { awk -v tag="[$1]" '{ print tag, $0; fflush() }'; }
cleanup() {
  trap - INT TERM EXIT
  local pids
  pids=$(jobs -p)
  for pid in $pids; do kill -TERM -- "-$pid" 2>/dev/null || true; done
  for _ in 1 2 3 4 5 6 7 8 9 10; do
    [ -z "$(jobs -r)" ] && return
    sleep 1
  done
  for pid in $pids; do kill -KILL -- "-$pid" 2>/dev/null || true; done
}
trap cleanup INT TERM EXIT

# Job control puts each app in a background process group, and the terminal stops any background
# process that reads from it. dotnet watch does (it listens for Ctrl+R), so without `< /dev/null` it
# is suspended before it starts and the web app runs with no API behind it.
(cd api && exec dotnet watch run --project PatientPortal.Api --non-interactive) < /dev/null 2>&1 | prefix api &
api_pid=$!
(cd web && exec pnpm dev) < /dev/null 2>&1 | prefix web &
web_pid=$!

# Announce once the API answers, so nobody signs in while it is still building.
(
  api_ready=
  for _ in $(seq 120); do
    if curl -fs -o /dev/null http://localhost:5246/health; then api_ready=1; break; fi
    sleep 1
  done
  if [ -z "$api_ready" ]; then
    echo "The API did not answer on http://localhost:5246/health within 2 minutes. Look for errors in the [api] output above." >&2
    exit 0
  fi
  cat <<'EOF2'

  Ready
  Web       http://localhost:3000   (sign in with a demo account listed on the page)
  API       http://localhost:5246
  Keycloak  http://localhost:8080

  Ctrl+C stops the API and the web app. Postgres and Keycloak keep running (./dev.sh down stops them).

EOF2
) &

# If either one exits on its own, stop the other and report it.
wait -n "$api_pid" "$web_pid"
