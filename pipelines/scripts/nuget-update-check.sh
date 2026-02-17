#!/bin/bash
set -e

# Workaround for Dependabot SDK-managed package bug:
# https://github.com/dependabot/dependabot-core/issues/12544
#
# Dependabot incorrectly classifies packages like Microsoft.EntityFrameworkCore
# as "SDK managed" and refuses to create PRs for patch/minor updates.
# It also cannot check private feeds for packages like Gebhardt.Core.*.
# This script checks NuGet feeds directly and creates PRs for those packages.

echo "=== NuGet Package Update Check ==="

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

DIRECTORY_PACKAGES_PROPS="Source/Directory.Packages.props"
BRANCH_PREFIX="nuget-update"

# NuGet feed URLs
NUGET_ORG_FLAT="https://api.nuget.org/v3-flatcontainer"
PRIVATE_FEED_BASE="https://pkgs.dev.azure.com/GebhardtGroup/StoreWare/_packaging/AgvDev/nuget/v3"

if [ ! -f "$DIRECTORY_PACKAGES_PROPS" ]; then
  echo "Error: $DIRECTORY_PACKAGES_PROPS not found"
  exit 1
fi

# Configure git
git config user.email "noreply@azure-devops.com"
git config user.name "Azure DevOps Pipeline"

if [ -n "$SYSTEM_ACCESSTOKEN" ]; then
  git config --global credential.helper '!f() { echo "username=PAT"; echo "password=${SYSTEM_ACCESSTOKEN}"; }; f'
fi

# Resolve the private feed's flat container URL from its service index
resolve_private_feed_flat_url() {
  if [ -z "$SYSTEM_ACCESSTOKEN" ]; then
    echo ""
    return
  fi

  local SERVICE_INDEX
  SERVICE_INDEX=$(curl -sf \
    -H "Authorization: Bearer ${SYSTEM_ACCESSTOKEN}" \
    "${PRIVATE_FEED_BASE}/index.json" 2>/dev/null || true)

  if [ -z "$SERVICE_INDEX" ]; then
    echo ""
    return
  fi

  # Extract PackageBaseAddress (flat container) URL
  local FLAT_URL
  FLAT_URL=$(echo "$SERVICE_INDEX" | \
    grep -oP '"@id"\s*:\s*"[^"]*"' | \
    while IFS= read -r line; do echo "$line"; done | \
    grep -B0 -A0 'packageBaseAddress\|flatcontainer\|PackageBaseAddress' | \
    head -1 | \
    grep -oP '"@id"\s*:\s*"\K[^"]+' || true)

  # Fallback: parse JSON more carefully for PackageBaseAddress/3.0.0 resource type
  if [ -z "$FLAT_URL" ]; then
    FLAT_URL=$(echo "$SERVICE_INDEX" | \
      python3 -c "
import sys, json
data = json.load(sys.stdin)
for r in data.get('resources', []):
    if 'PackageBaseAddress' in r.get('@type', ''):
        print(r['@id'])
        break
" 2>/dev/null || true)
  fi

  echo "$FLAT_URL"
}

# Packages that need the private Azure DevOps feed
PRIVATE_FEED_PREFIXES=(
  "Gebhardt.Core."
)

# Packages affected by the SDK-managed bug (checked on nuget.org)
SDK_MANAGED_PREFIXES=(
  "Microsoft.EntityFrameworkCore"
  "Microsoft.Extensions."
  "Microsoft.AspNetCore."
  "System."
)

# Packages to skip entirely (version-locked or manually managed)
SKIP_PACKAGES=(
  "OpenTelemetry.Resources.Container"
  "AutoMapper"
  "MassTransit"
)

# Check if a package should be skipped
should_skip() {
  local PACKAGE_NAME="$1"
  for SKIP in "${SKIP_PACKAGES[@]}"; do
    if [[ "$PACKAGE_NAME" == "$SKIP"* ]]; then
      return 0
    fi
  done
  return 1
}

# Check if a package uses the private feed
is_private_feed() {
  local PACKAGE_NAME="$1"
  for PREFIX in "${PRIVATE_FEED_PREFIXES[@]}"; do
    if [[ "$PACKAGE_NAME" == "$PREFIX"* ]]; then
      return 0
    fi
  done
  return 1
}

# Check if a package is affected by the SDK-managed bug
is_sdk_managed_affected() {
  local PACKAGE_NAME="$1"
  for PREFIX in "${SDK_MANAGED_PREFIXES[@]}"; do
    if [[ "$PACKAGE_NAME" == "$PREFIX"* ]]; then
      return 0
    fi
  done
  return 1
}

# Check if this script should handle the package
should_check() {
  local PACKAGE_NAME="$1"
  if should_skip "$PACKAGE_NAME"; then
    return 1
  fi
  if is_private_feed "$PACKAGE_NAME"; then
    return 0
  fi
  if is_sdk_managed_affected "$PACKAGE_NAME"; then
    return 0
  fi
  return 1
}

# Query a NuGet flat container for the latest version matching major.minor
# Usage: query_feed <flat_container_url> <package_name> <current_version> [auth_header]
# Returns the latest version on stdout, or empty string if no update
query_feed() {
  local FLAT_URL="$1"
  local PACKAGE_NAME="$2"
  local CURRENT_VERSION="$3"
  local AUTH_HEADER="$4"

  # Strip brackets from locked versions
  CURRENT_VERSION="${CURRENT_VERSION//[\[\]]/}"

  # Extract major.minor
  local MAJOR_MINOR
  MAJOR_MINOR=$(echo "$CURRENT_VERSION" | grep -oP '^\d+\.\d+')

  if [ -z "$MAJOR_MINOR" ]; then
    return
  fi

  local LOWER_ID
  LOWER_ID=$(echo "$PACKAGE_NAME" | tr '[:upper:]' '[:lower:]')

  local VERSIONS_JSON
  if [ -n "$AUTH_HEADER" ]; then
    VERSIONS_JSON=$(curl -sf -H "$AUTH_HEADER" "${FLAT_URL}${LOWER_ID}/index.json" 2>/dev/null || true)
  else
    VERSIONS_JSON=$(curl -sf "${FLAT_URL}${LOWER_ID}/index.json" 2>/dev/null || true)
  fi

  if [ -z "$VERSIONS_JSON" ]; then
    return
  fi

  # Find the latest version matching major.minor.* (exclude prerelease with hyphens)
  local LATEST
  LATEST=$(echo "$VERSIONS_JSON" | \
    grep -oP "\"${MAJOR_MINOR}\.\d+\"" | \
    tr -d '"' | \
    sort -t. -k3 -n | \
    tail -1)

  if [ -z "$LATEST" ]; then
    return
  fi

  if [ "$LATEST" != "$CURRENT_VERSION" ]; then
    echo "$LATEST"
  fi
}

# Resolve private feed URL
echo ""
echo "Resolving private NuGet feed..."
PRIVATE_FLAT_URL=$(resolve_private_feed_flat_url)
if [ -n "$PRIVATE_FLAT_URL" ]; then
  # Ensure trailing slash
  [[ "$PRIVATE_FLAT_URL" != */ ]] && PRIVATE_FLAT_URL="${PRIVATE_FLAT_URL}/"
  echo "  Private feed: $PRIVATE_FLAT_URL"
else
  echo "  Warning: Could not resolve private feed. Private packages will be skipped."
fi

# Collect packages already covered by Dependabot branches, to avoid duplicate PRs.
# Dependabot branch names contain the lowercased package name, e.g.:
#   dependabot/nuget/efcore-abc123  (group name)
#   dependabot/nuget/microsoft.entityframeworkcore-10.0.3
echo ""
echo "Checking for existing Dependabot branches..."
DEPENDABOT_BRANCHES=$(git ls-remote --heads origin 'refs/heads/dependabot/nuget/*' 2>/dev/null | awk '{print $2}' | sed 's|refs/heads/||' || true)
if [ -n "$DEPENDABOT_BRANCHES" ]; then
  echo "  Found Dependabot branches:"
  echo "$DEPENDABOT_BRANCHES" | sed 's/^/    /'
else
  echo "  No Dependabot branches found."
fi

# Check if a package is already handled by an existing Dependabot branch
is_covered_by_dependabot() {
  local PACKAGE_NAME="$1"
  local LOWER_PKG
  LOWER_PKG=$(echo "$PACKAGE_NAME" | tr '[:upper:]' '[:lower:]')

  if [ -z "$DEPENDABOT_BRANCHES" ]; then
    return 1
  fi

  # Check if any dependabot branch name contains this package name
  if echo "$DEPENDABOT_BRANCHES" | tr '[:upper:]' '[:lower:]' | grep -q "$LOWER_PKG"; then
    return 0
  fi

  return 1
}

echo ""
echo "Checking for package updates..."
echo ""

# Parse Directory.Packages.props for PackageVersion entries
declare -A UPDATES

while IFS= read -r line; do
  PACKAGE_NAME=$(echo "$line" | grep -oP 'Include="\K[^"]+')
  VERSION=$(echo "$line" | grep -oP 'Version="\K[^"]+')

  if [ -z "$PACKAGE_NAME" ] || [ -z "$VERSION" ]; then
    continue
  fi

  # Check if this script should handle this package
  if ! should_check "$PACKAGE_NAME"; then
    continue
  fi

  # Skip if Dependabot already created a branch for this package
  if is_covered_by_dependabot "$PACKAGE_NAME"; then
    echo "  Skipping $PACKAGE_NAME (already handled by Dependabot branch)"
    continue
  fi

  # Skip version-locked packages (wrapped in brackets like [8.5.7])
  if [[ "$VERSION" == \[*\] ]]; then
    echo "  Skipping $PACKAGE_NAME (version locked at $VERSION)"
    continue
  fi

  # Determine which feed to query
  LATEST=""
  if is_private_feed "$PACKAGE_NAME"; then
    if [ -n "$PRIVATE_FLAT_URL" ]; then
      LATEST=$(query_feed "$PRIVATE_FLAT_URL" "$PACKAGE_NAME" "$VERSION" "Authorization: Bearer ${SYSTEM_ACCESSTOKEN}")
    else
      echo "  Skipping $PACKAGE_NAME (private feed not available)"
      continue
    fi
  else
    LATEST=$(query_feed "${NUGET_ORG_FLAT}/" "$PACKAGE_NAME" "$VERSION")
  fi

  if [ -n "$LATEST" ]; then
    echo "  UPDATE: $PACKAGE_NAME $VERSION -> $LATEST"
    UPDATES["$PACKAGE_NAME"]="$LATEST"
  else
    echo "  OK: $PACKAGE_NAME $VERSION (up to date)"
  fi
done < <(grep '<PackageVersion ' "$DIRECTORY_PACKAGES_PROPS")

if [ ${#UPDATES[@]} -eq 0 ]; then
  echo ""
  echo "All packages are up to date. Nothing to do."
  exit 0
fi

echo ""
echo "Found ${#UPDATES[@]} package(s) with available updates."
echo ""

# Create a single branch with all updates, always based on master
BRANCH_NAME="${BRANCH_PREFIX}/sdk-managed-packages"
BRANCH_EXISTS=false
CREATE_PR=true

# Ensure we have the latest master
git fetch origin master
git checkout origin/master

# Check if branch already exists on remote
if git ls-remote --heads origin "$BRANCH_NAME" | grep -q "$BRANCH_NAME"; then
  BRANCH_EXISTS=true
  CREATE_PR=false
  echo "Branch $BRANCH_NAME already exists on remote. Will update it."
fi

git checkout -b "$BRANCH_NAME"

# Apply updates to Directory.Packages.props
for PACKAGE_NAME in "${!UPDATES[@]}"; do
  NEW_VERSION="${UPDATES[$PACKAGE_NAME]}"
  sed -i "s|\(<PackageVersion Include=\"$PACKAGE_NAME\" Version=\"\)[^\"]*\(\".*\)|\1$NEW_VERSION\2|g" "$DIRECTORY_PACKAGES_PROPS"
  echo "  Updated $PACKAGE_NAME to $NEW_VERSION in $DIRECTORY_PACKAGES_PROPS"
done

# Build commit message
COMMIT_PACKAGES=""
for PACKAGE_NAME in "${!UPDATES[@]}"; do
  COMMIT_PACKAGES="${COMMIT_PACKAGES}\n  - ${PACKAGE_NAME} -> ${UPDATES[$PACKAGE_NAME]}"
done

git add "$DIRECTORY_PACKAGES_PROPS"

if git diff --cached --quiet; then
  echo "No changes to commit. The existing PR already has the latest versions."
  git checkout --detach
  git branch -D "$BRANCH_NAME"
  exit 0
fi

git commit -m "$(cat <<EOF
Bump NuGet packages

Automated update for packages that Dependabot cannot handle:
- SDK-managed packages (dependabot-core#12544)
- Private feed packages

Updated packages:$(echo -e "$COMMIT_PACKAGES")
EOF
)"

echo ""
echo "Pushing branch $BRANCH_NAME to remote..."
if [ "$BRANCH_EXISTS" = true ]; then
  git push --force-with-lease origin "$BRANCH_NAME"
else
  git push -u origin "$BRANCH_NAME"
fi

if [ "$CREATE_PR" = false ]; then
  echo ""
  echo "Existing PR updated with latest versions via force-push."
  git checkout --detach
  echo ""
  echo "Done!"
  exit 0
fi

echo ""
echo "Creating pull request..."

# Build PR body
PR_BODY="## NuGet Package Updates

Automated update for packages that Dependabot cannot handle:
- **SDK-managed packages**: Dependabot incorrectly classifies these as \"SDK managed\" ([dependabot-core#12544](https://github.com/dependabot/dependabot-core/issues/12544))
- **Private feed packages**: Packages from the AgvDev feed that Dependabot ignores

### Updated packages
| Package | New Version |
|---------|-------------|"

for PACKAGE_NAME in "${!UPDATES[@]}"; do
  PR_BODY="${PR_BODY}
| ${PACKAGE_NAME} | ${UPDATES[$PACKAGE_NAME]} |"
done

# Create PR using Azure DevOps REST API
if [ -n "$SYSTEM_ACCESSTOKEN" ] && [ -n "$SYSTEM_TEAMFOUNDATIONCOLLECTIONURI" ] && [ -n "$SYSTEM_TEAMPROJECTID" ] && [ -n "$BUILD_REPOSITORY_ID" ]; then
  ORG_URL="${SYSTEM_TEAMFOUNDATIONCOLLECTIONURI}"
  PROJECT_ID="${SYSTEM_TEAMPROJECTID}"
  REPO_ID="${BUILD_REPOSITORY_ID}"

  TARGET_BRANCH="refs/heads/master"
  SOURCE_BRANCH="refs/heads/${BRANCH_NAME}"

  REVIEWERS='[
    {"id":"d57dc9ca-b7fe-401f-b7f7-c2ec2fcb5b3d"},
    {"id":"a1963800-1848-4394-a6ed-7d0f733b4359"},
    {"id":"42f4ca0f-010d-6b7d-9420-a358bd5d6ed2"},
    {"id":"c01aac72-8af7-4b1d-8393-c787d77add86"}
  ]'

  PR_TITLE="Bump NuGet packages (SDK-managed + private feed)"

  # Build JSON payload via python to avoid shell escaping issues
  PR_JSON_FILE=$(mktemp)
  python3 -c "
import json, sys
body = sys.stdin.read()
pr = {
    'sourceRefName': '${SOURCE_BRANCH}',
    'targetRefName': '${TARGET_BRANCH}',
    'title': '${PR_TITLE}',
    'description': body,
    'reviewers': ${REVIEWERS}
}
json.dump(pr, open('${PR_JSON_FILE}', 'w'))
" <<< "$PR_BODY"

  API_URL="${ORG_URL}${PROJECT_ID}/_apis/git/repositories/${REPO_ID}/pullrequests?api-version=7.1"
  echo "  API URL: $API_URL"

  HTTP_CODE=$(curl -s -o /tmp/pr_response.json -w "%{http_code}" \
    -X POST \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer ${SYSTEM_ACCESSTOKEN}" \
    -d @"$PR_JSON_FILE" \
    "$API_URL") || true

  RESPONSE=$(cat /tmp/pr_response.json 2>/dev/null || true)
  rm -f "$PR_JSON_FILE" /tmp/pr_response.json

  if [ "$HTTP_CODE" = "201" ] && echo "$RESPONSE" | grep -q '"pullRequestId"'; then
    PR_ID=$(echo "$RESPONSE" | grep -oP '"pullRequestId":\s*\K\d+')
    echo ""
    echo "Pull request #${PR_ID} created successfully!"
  else
    echo ""
    echo "Warning: Could not create PR via API (HTTP $HTTP_CODE). Branch was pushed."
    echo "Please create the PR manually from branch: $BRANCH_NAME"
    echo "API response: $RESPONSE"
  fi
else
  echo ""
  echo "Azure DevOps environment variables not available."
  echo "Branch was pushed. Please create the PR manually from branch: $BRANCH_NAME"
fi

# Detach from the update branch (CI doesn't need to stay on it)
git checkout --detach

echo ""
echo "Done!"
