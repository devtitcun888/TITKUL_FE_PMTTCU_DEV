#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

fail=0

while IFS= read -r -d '' file; do
  rel="${file#"$root"/}"

  case "$rel" in
    *.dll|*.exe|*.pfx|*.p12|*.pem|*.snk|*.pdb)
      echo "FAIL disallowed artifact: $rel"
      fail=1
      continue
      ;;
    *.ico|*.png|*.jpg|*.jpeg|*.gif|*.webp|*.woff|*.woff2)
      continue
      ;;
  esac

  if grep -E -n -I \
      -e '-----BEGIN [A-Z ]*PRIVATE KEY-----' \
      -e 'ghp_[A-Za-z0-9]{20,}' \
      -e 'github_pat_[A-Za-z0-9_]{20,}' \
      -e 'AKIA[0-9A-Z]{16}' \
      -e 'xox[baprs]-[A-Za-z0-9-]{10,}' \
      -e 'sk_live_[A-Za-z0-9]{16,}' \
      "$file"; then
    echo "FAIL token pattern: $rel"
    fail=1
  fi

  if grep -E -n -I '"[Pp]assword"[[:space:]]*:[[:space:]]*"[^"]+"' "$file" \
      | grep -E -v 'SET_VIA_ENVIRONMENT'; then
    echo "FAIL password value: $rel"
    fail=1
  fi

  if grep -E -n -I -e '[Pp]assword=[^;[:space:]]+' -e '[Pp]wd=[^;[:space:]]+' "$file" \
      | grep -E -v 'SET_VIA_ENVIRONMENT'; then
    echo "FAIL connection string secret: $rel"
    fail=1
  fi
done < <(find "$root" \
  \( -path '*/.git' -o -path '*/bin' -o -path '*/obj' -o -path '*/.vs' -o -path '*/TestResults' \) -prune \
  -o -type f -print0)

if [[ "$fail" -ne 0 ]]; then
  echo "Secret scan failed."
  exit 1
fi

echo "Secret scan passed."
