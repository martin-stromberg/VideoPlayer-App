#!/usr/bin/env bash
# Fuehrt semantic-release mit begrenzten Wiederholversuchen aus.
#
# Hintergrund: semantic-release legt den Versionstag lokal an und pusht
# ihn; schlaegt ein Folgeschritt fehl (bekanntes, sporadisch auftretendes
# Problem: der Push des git-notes-Refs "refs/notes/semantic-release-<tag>"
# wird mit "missing necessary objects" abgelehnt), bleibt remote ein
# verwaister Tag ohne GitHub-Release zurueck. Ein naiver Retry wuerde die
# Version dann als bereits veroeffentlicht erkennen und kein Release
# erzeugen. Daher wird vor jedem weiteren Versuch aufgeraeumt:
#   - Remote-Tags v*, die auf den gepushten HEAD-Commit zeigen und kein
#     GitHub-Release haben (d. h. von diesem Lauf stammen)
#   - zugehoerige remote/lokale Notes-Refs und lokale Tags, damit der
#     erneute Lauf sauber startet
set -u

MAX_ATTEMPTS=3
RETRY_DELAY_S=15

head_sha=$(git rev-parse HEAD)

cleanup_orphaned_tags() {
  git ls-remote --tags origin \
    | awk -v sha="$head_sha" '$1 == sha {print $2}' \
    | sed -n 's|refs/tags/\(v[^^]*\)$|\1|p' \
    | while read -r tag; do
        if gh release view "$tag" >/dev/null 2>&1; then
          echo "Tag $tag hat ein GitHub-Release - bleibt erhalten"
          continue
        fi
        echo "Entferne verwaisten Tag: $tag"
        git push origin --delete "refs/tags/$tag" || true
        git push origin --delete "refs/notes/semantic-release-$tag" || true
        git tag -d "$tag" >/dev/null 2>&1 || true
        git update-ref -d "refs/notes/semantic-release-$tag" || true
      done
}

for attempt in $(seq 1 "$MAX_ATTEMPTS"); do
  if semantic-release; then
    exit 0
  fi
  echo "::warning::semantic-release fehlgeschlagen (Versuch $attempt/$MAX_ATTEMPTS)"
  if [ "$attempt" -lt "$MAX_ATTEMPTS" ]; then
    cleanup_orphaned_tags
    sleep "$RETRY_DELAY_S"
  fi
done

echo "::error::semantic-release nach $MAX_ATTEMPTS Versuchen fehlgeschlagen"
exit 1
