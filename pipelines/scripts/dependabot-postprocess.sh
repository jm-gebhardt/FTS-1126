#!/bin/bash
set -e

echo "=== Dependabot Post-Processing Script ==="

# Configure git for pushing (uses SYSTEM_ACCESSTOKEN from environment)
git config user.email "noreply@azure-devops.com"
git config user.name "Azure DevOps Pipeline"

# Set up authentication using the system access token
if [ -n "$SYSTEM_ACCESSTOKEN" ]; then
  # Configure git to use the token for authentication
  # Username can be anything for Azure DevOps - the token is what matters
  git config --global credential.helper '!f() { echo "username=PAT"; echo "password=${SYSTEM_ACCESSTOKEN}"; }; f'
fi

# Ensure we're in the repository root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

echo "Working directory: $(pwd)"
echo "Current branch: $(git branch --show-current)"

HELPER_CSPROJ="Source/dependabot-helper.csproj"
DIRECTORY_PACKAGES_PROPS="Source/Directory.Packages.props"

# Function to process a single branch
process_branch() {
  local BRANCH_NAME="$1"

  echo ""
  echo "========================================"
  echo "Processing branch: $BRANCH_NAME"
  echo "========================================"

  # Checkout the branch
  git checkout "$BRANCH_NAME"

  # Step 1: Fix line endings in dependabot-helper.csproj
  echo ""
  echo "Step 1: Normalizing line endings..."
  git add --renormalize "$HELPER_CSPROJ"

  # Check if there are changes after renormalization
  if ! git diff --cached --quiet "$HELPER_CSPROJ"; then
    echo "✓ Line endings normalized in $HELPER_CSPROJ"
  else
    echo "No line ending changes needed"
  fi

  # Step 2: Extract version overrides from dependabot-helper.csproj
  echo ""
  echo "Step 2: Checking for package version updates..."

  # Extract package names and versions using grep
  # This finds lines like: <PackageReference Include="PackageName" VersionOverride="1.2.3" />
  PACKAGE_LINES=$(grep -oP '<PackageReference\s+Include="[^"]+"\s+VersionOverride="[^"]+"' "$HELPER_CSPROJ" || true)

  if [ -z "$PACKAGE_LINES" ]; then
    echo "No package version updates found in $HELPER_CSPROJ"

    # If no updates but file was modified (line endings only), commit that
    if ! git diff --cached --quiet; then
      echo "Committing line ending fixes..."
      git commit --amend --no-edit
      echo "✓ Line endings fixed"

      echo ""
      echo "Pushing changes to remote..."
      git push --force-with-lease origin "$BRANCH_NAME"
      echo "✓ Changes pushed to origin/$BRANCH_NAME"
    fi

    return 0
  fi

  echo "Found package updates in $HELPER_CSPROJ"

  # Initialize arrays
  declare -a PACKAGE_ARRAY
  declare -a VERSION_ARRAY

  # Parse each line to extract package name and version
  while IFS= read -r line; do
    if [ -n "$line" ]; then
      PACKAGE_NAME=$(echo "$line" | grep -oP 'Include="\K[^"]+')
      VERSION=$(echo "$line" | grep -oP 'VersionOverride="\K[^"]+')
      PACKAGE_ARRAY+=("$PACKAGE_NAME")
      VERSION_ARRAY+=("$VERSION")
    fi
  done <<< "$PACKAGE_LINES"

  echo ""
  echo "Packages to update:"
  for i in "${!PACKAGE_ARRAY[@]}"; do
    echo "  - ${PACKAGE_ARRAY[$i]} → ${VERSION_ARRAY[$i]}"
  done

  # Step 3: Update Directory.Packages.props
  echo ""
  echo "Step 3: Updating $DIRECTORY_PACKAGES_PROPS..."

  for i in "${!PACKAGE_ARRAY[@]}"; do
    PACKAGE_NAME="${PACKAGE_ARRAY[$i]}"
    NEW_VERSION="${VERSION_ARRAY[$i]}"

    if grep -q "Include=\"$PACKAGE_NAME\"" "$DIRECTORY_PACKAGES_PROPS"; then
      # This handles both locked versions [x.x.x] and regular versions
      sed -i "s|\(<PackageVersion Include=\"$PACKAGE_NAME\" Version=\"\)\([^\"]*\)\(\".*\)|\1$NEW_VERSION\3|g" "$DIRECTORY_PACKAGES_PROPS"
      echo "  ✓ Updated $PACKAGE_NAME to $NEW_VERSION"
    else
      echo "  ⚠ Warning: $PACKAGE_NAME not found in $DIRECTORY_PACKAGES_PROPS"
    fi
  done

  # Step 4: Restore dependabot-helper.csproj to its original state
  echo ""
  echo "Step 4: Restoring $HELPER_CSPROJ to original state..."

  # Get the original content (before dependabot's changes)
  # HEAD is the dependabot commit, so we need HEAD~1 (parent commit)
  git show HEAD~1:"$HELPER_CSPROJ" > "$HELPER_CSPROJ"

  echo "✓ $HELPER_CSPROJ restored"

  # Step 5: Stage and commit all changes
  echo ""
  echo "Step 5: Committing changes..."

  git add "$DIRECTORY_PACKAGES_PROPS" "$HELPER_CSPROJ"

  # Check if there are actually changes to commit
  if git diff --cached --quiet; then
    echo "No changes to commit"
    return 0
  fi

  # Get package names for commit message
  UPDATED_PACKAGES=$(printf ", %s" "${PACKAGE_ARRAY[@]}")
  UPDATED_PACKAGES=${UPDATED_PACKAGES:2}  # Remove leading ", "

  # Amend the dependabot commit with our changes
  git commit --amend --no-edit

  # Step 6: Push the amended commit back to the remote branch
  echo ""
  echo "Step 6: Pushing changes to remote..."

  git push --force-with-lease origin "$BRANCH_NAME"

  echo "✓ Changes pushed to origin/$BRANCH_NAME"

  echo ""
  echo "✓ Post-processing complete for $BRANCH_NAME!"
  echo ""
  echo "Changes made:"
  echo "  - Fixed line endings in $HELPER_CSPROJ"
  echo "  - Updated package versions in $DIRECTORY_PACKAGES_PROPS"
  echo "  - Restored $HELPER_CSPROJ to original state"
  echo "  - Pushed amended commit to remote"
  echo ""
  echo "Updated packages: $UPDATED_PACKAGES"
}

# Main script execution
echo ""
echo "========================================"
echo "Fetching all remote branches..."
echo "========================================"

# Fetch all branches from remote
git fetch origin

# Find all dependabot branches
DEPENDABOT_BRANCHES=$(git branch -r | grep 'origin/dependabot/' | sed 's|origin/||' | sed 's/^[[:space:]]*//' || true)

if [ -z "$DEPENDABOT_BRANCHES" ]; then
  echo ""
  echo "No dependabot branches found. Nothing to process."
  exit 0
fi

echo ""
echo "Found dependabot branches:"
echo "$DEPENDABOT_BRANCHES"

# Process each dependabot branch
PROCESSED_COUNT=0
FAILED_COUNT=0

while IFS= read -r BRANCH; do
  if [ -n "$BRANCH" ]; then
    if process_branch "$BRANCH"; then
      PROCESSED_COUNT=$((PROCESSED_COUNT + 1))
    else
      echo "⚠ Failed to process branch: $BRANCH"
      FAILED_COUNT=$((FAILED_COUNT + 1))
    fi
  fi
done <<< "$DEPENDABOT_BRANCHES"

echo ""
echo "========================================"
echo "Summary"
echo "========================================"
echo "Branches processed: $PROCESSED_COUNT"
echo "Branches failed: $FAILED_COUNT"
echo ""

if [ $FAILED_COUNT -gt 0 ]; then
  echo "⚠ Some branches failed to process. Check the logs above for details."
  exit 1
fi

echo "✓ All dependabot branches processed successfully!"
