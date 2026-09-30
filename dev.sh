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

[ -f .env ] || { cp .env.example .env; echo "Created .env from .env.example"; }
[ -f web/.env.local ] || { cp web/.env.example web/.env.local; echo "Created web/.env.local from web/.env.example"; }

echo "Starting Postgres, Keycloak and the database migrations..."
docker compose stop web api >/dev/null 2>&1 || true # the containerised copies use the same ports
docker compose up -d --wait postgres keycloak
docker compose run --rm --quiet-pull migrations >/dev/null || { docker compose logs migrations >&2; exit 1; }

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

cat <<'EOF2'

  Web       http://localhost:3000   (sign in with a demo account listed on the page)
  API       http://localhost:5246
  Keycloak  http://localhost:8080

  Ctrl+C stops the API and the web app. Postgres and Keycloak keep running (./dev.sh down stops them).

EOF2

(cd api && exec dotnet watch run --project PatientPortal.Api --non-interactive) 2>&1 | prefix api &
(cd web && exec pnpm dev) 2>&1 | prefix web &

# If either one exits on its own, stop the other and report it.
wait -n
