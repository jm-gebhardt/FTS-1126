#!/bin/bash

# update_dependencies.sh
# Adapted from Gebhardt.Platform.CI/scripts/update_dependencies.sh for the ACC
# repository structure, where two solutions share a single Directory.Packages.props:
#
#   SOURCE_DIR/Agv/NuGet.Config + CompileAgv.sln
#   SOURCE_DIR/Agv.IntegrationTest/NuGet.Config + IntegrationTest.sln
#   SOURCE_DIR/Directory.Packages.props  (shared - updated by both runs)
#
# Required environment variables:
#   AZURE_ARTIFACTS_TOKEN  - Azure DevOps access token
#   SOURCE_DIR             - Absolute path to the Source/ directory

#########################################################################
# Initialize variables
#########################################################################
echo "##[section]Initializing variables."

if [[ -z "${AZURE_ARTIFACTS_TOKEN}" ]]; then
    echo "##[error]Environment Variable 'AZURE_ARTIFACTS_TOKEN' not set."
    exit 1
fi

if [[ -z "${SOURCE_DIR}" ]]; then
    echo "##[error]Environment Variable 'SOURCE_DIR' not set."
    exit 1
fi

AZURE_TOKEN="${AZURE_ARTIFACTS_TOKEN}"
CURRENT_DATE=$(date +%Y-%m-%d)
BRANCH_PREFIX="feature/bump-dependencies"

SOLUTION_DIRS=("${SOURCE_DIR}/Agv" "${SOURCE_DIR}/Agv.IntegrationTest")
DIRECTORY_PACKAGES_PROPS_FILE="${SOURCE_DIR}/Directory.Packages.props"
NUGET_CONFIG_FILE="${SOURCE_DIR}/Agv/NuGet.Config"


#########################################################################
# Functions
#########################################################################

function get_nuget_feed_url() {
  local nuget_config=$1
  local nuget_feed_url
  # Select the private AgvDev feed specifically (NuGet.Config also lists nuget.org)
  nuget_feed_url=$(xmlstarlet sel -t -v "/configuration/packageSources/add[@key='AgvDev']/@value" "$nuget_config")
  echo "$nuget_feed_url"
}

function get_repository_url() {
  local azure_token=$1
  local build_repository_url=$2
  local selector="https:\/\/.*@"
  local replacement="https://${azure_token}@"
  local repository_url
  repository_url=$(echo "$build_repository_url" | sed -e "s|$selector|${replacement}|g")
  echo "$repository_url"
}

function configure_git() {
  echo "##[debug]Configuring git."
  git config user.email "dependabot@gebhardt-group.com"
  git config user.name "Donnie Dependabot"
}

function switch_to_new_git_branch() {
  local branch_name=$1
  echo "##[debug]Switching to new branch $1."
  echo "##[command]git switch -c ${branch_name}"
  git switch -c "${branch_name}"
}

function authenticate_to_nuget_feed() {
  local nuget_feed_url=$1
  local azure_token=$2
  echo "##[command]Authenticate to Nuget Feed $1."
  sh -c "$(curl -fsSL https://aka.ms/install-artifacts-credprovider.sh)"

  NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED=true
  VSS_NUGET_EXTERNAL_FEED_ENDPOINTS="{\"endpointCredentials\":[{\"endpoint\":\"${nuget_feed_url}\",\"username\":\"unused\",\"password\":\"${azure_token}\"}]}"

  export NUGET_CREDENTIALPROVIDER_SESSIONTOKENCACHE_ENABLED
  export VSS_NUGET_EXTERNAL_FEED_ENDPOINTS
}

function install_xmlstarlet() {
  if ! command -v xmlstarlet &> /dev/null; then
    echo "xmlstarlet could not be found, installing..."
    sudo apt-get update
    sudo apt-get install -y xmlstarlet
  fi
}

function install_dotnet_outdated() {
  echo "##[command]dotnet tool install --global dotnet-outdated-tool --version 4.7.1"
  dotnet tool install --global dotnet-outdated-tool --version 4.7.1
}

#
# Run dotnet restore and dotnet outdated in a solution directory.
# dotnet-outdated traverses up to find and update the shared Directory.Packages.props.
# Parameters:
#   solution_dir    - Directory containing the .sln and NuGet.Config
#   report_suffix   - Suffix for report file names (e.g. "-agv", "-integration")
#
function run_outdated_in_dir() {
  local solution_dir=$1
  local report_suffix=$2

  echo "##[section]Updating dependencies in $solution_dir"
  pushd "$solution_dir" > /dev/null

  echo "##[debug]Restoring nuget packages."
  local sln_file
  sln_file=$(ls -1 *.sln 2>/dev/null | head -1)
  if [[ -z "$sln_file" ]]; then
    echo "##[error]No solution file found in $solution_dir."
    popd > /dev/null
    exit 1
  fi
  dotnet restore "$sln_file"

  local excludes=""
  if [[ -n "${EXCLUDED_DEPENDENCIES:-}" ]]; then
    excludes=$(printf " --exclude %s" ${EXCLUDED_DEPENDENCIES})
    echo "Excluding packages matching one of [${EXCLUDED_DEPENDENCIES}]."
  fi
  echo "##[command]dotnet outdated -vl major -u -o ${SYSTEM_ARTIFACTSDIRECTORY}/report${report_suffix}.md -of Markdown ${excludes}"
  dotnet outdated -vl major -u -o "${SYSTEM_ARTIFACTSDIRECTORY}/report${report_suffix}.md" -of Markdown ${excludes}

  if [[ -n "${INCLUDED_DEPENDENCIES:-}" ]]; then
    local includes
    includes=$(printf " --include %s" ${INCLUDED_DEPENDENCIES})
    echo "##[command]dotnet outdated -vl major -u -o ${SYSTEM_ARTIFACTSDIRECTORY}/report-include${report_suffix}.md -of Markdown ${includes}"
    dotnet outdated -vl major -u -o "${SYSTEM_ARTIFACTSDIRECTORY}/report-include${report_suffix}.md" -of Markdown ${includes}
  fi

  popd > /dev/null
}


#########################################################################
# Create a new feature branch
#########################################################################
echo "##[section]Create a new feature branch."
configure_git
switch_to_new_git_branch "${BRANCH_PREFIX}-${CURRENT_DATE}"

#########################################################################
# Setup
#########################################################################
install_xmlstarlet

NUGET_FEED_URL=$(get_nuget_feed_url "$NUGET_CONFIG_FILE")
echo "##[debug]Nuget Feed Url: ${NUGET_FEED_URL}"
REPOSITORY_URL=$(get_repository_url "$AZURE_TOKEN" "$BUILD_REPOSITORY_URI")
echo "##[debug]Repository Url: ${REPOSITORY_URL}"

authenticate_to_nuget_feed "${NUGET_FEED_URL}" "${AZURE_TOKEN}"
install_dotnet_outdated

#########################################################################
# Update dependencies in both solutions
# Both update the shared Source/Directory.Packages.props
#########################################################################
run_outdated_in_dir "${SOURCE_DIR}/Agv" "-agv"
run_outdated_in_dir "${SOURCE_DIR}/Agv.IntegrationTest" "-integration"

# Exit early if nothing changed
if [[ -z "$(git status --porcelain)" ]]; then
  echo "##[debug]Working directory clean. No dependency updates found."
  exit 0
fi

#########################################################################
# Log in to Azure DevOps
#########################################################################
echo "##[debug]Setting defaults for azure cli."
az devops configure --defaults organization="${SYSTEM_COLLECTIONURI}"
az devops configure --defaults project="${SYSTEM_TEAMPROJECT}"

echo "##[debug]Logging in to Azure DevOps."
echo "${AZURE_TOKEN}" | az devops login

#########################################################################
# Abandon open pull requests without external contributions and delete
# their source branches so we start fresh each run
#########################################################################
git remote set-url "origin" "${REPOSITORY_URL}"

echo "##[debug]Looking for existing pull requests and branches to clean up."
EXISTING_PRS=$(az repos pr list --status active --query "[?contains(sourceRefName, '${BRANCH_PREFIX}')]" -o json)

for PR in $(echo "${EXISTING_PRS}" | jq -r '.[] | @base64'); do
    _jq() {
        echo "${PR}" | base64 --decode | jq -r "${1}"
    }
    PR_ID=$(_jq '.pullRequestId')
    SOURCE_BRANCH=$(_jq '.sourceRefName')
    CREATOR="dependabot@gebhardt-group.com"
    REPO_ID=$(_jq '.repository.id')

    echo "##[debug]Fetching commit history for PR #${PR_ID}."
    ENCODED_TEAMPROJECT=$(echo "${SYSTEM_TEAMPROJECT}" | jq -Rr @uri)
    COMMITS=$(curl -s -u ":${AZURE_TOKEN}" \
        "${SYSTEM_COLLECTIONURI}${ENCODED_TEAMPROJECT}/_apis/git/repositories/${REPO_ID}/pullrequests/${PR_ID}/commits?api-version=7.1" \
        | jq -r '.value[].author.email')

    OTHER_CONTRIBUTORS=$(echo "${COMMITS}" | grep -F -v "${CREATOR}" | sort -u || true)

    if [[ -z "${OTHER_CONTRIBUTORS}" ]]; then
        CONTRIBUTOR_COUNT=0
    else
        CONTRIBUTOR_COUNT=$(echo "${OTHER_CONTRIBUTORS}" | wc -l)
    fi

    echo "##[debug]Other contributors count: ${CONTRIBUTOR_COUNT}"

    if [[ "${CONTRIBUTOR_COUNT}" -eq 0 ]]; then
        echo "##[debug]No additional contributors other than ${CREATOR}. Abandoning PR #${PR_ID}."
        az repos pr update --id "${PR_ID}" --status abandoned

        BRANCH_TO_DELETE=$(echo "${SOURCE_BRANCH}" | sed 's|refs/heads/||')
        echo "##[debug]Deleting branch ${BRANCH_TO_DELETE}."
        git push --delete origin "${BRANCH_TO_DELETE}"
    else
        echo "##[debug]PR #${PR_ID} has ${CONTRIBUTOR_COUNT} additional contributors:"
        echo "${OTHER_CONTRIBUTORS}"
    fi
done

#########################################################################
# Commit and push
#########################################################################
echo "##[debug]Creating a commit and pushing the branch to the server."
git add -A
git commit -m "Bump dependencies"

GIT_PUSH_RESPONSE=$(git push --porcelain -u origin "HEAD:${BRANCH_PREFIX}-${CURRENT_DATE}")
echo "$GIT_PUSH_RESPONSE"
if [[ "$GIT_PUSH_RESPONSE" == *"rejected"* ]]; then
  echo "##[error]Git push was rejected. Please abandon the old pull request first."
  exit 1
fi

#########################################################################
# Create a pull request
#########################################################################
echo "##[section]Create a pull request."

clean_report() {
  sed -E 's/\$(\{\\textsf\{([0-9.]*)\}\})*\\textcolor\{[a-z]*\}\{\\textsf\{([0-9.]*)\}\}\$/\2\3/g' "$1"
}

PR_DESCRIPTION=""
for report in \
  "${SYSTEM_ARTIFACTSDIRECTORY}/report-agv.md" \
  "${SYSTEM_ARTIFACTSDIRECTORY}/report-integration.md" \
  "${SYSTEM_ARTIFACTSDIRECTORY}/report-include-agv.md" \
  "${SYSTEM_ARTIFACTSDIRECTORY}/report-include-integration.md"; do
  if [[ -f "$report" ]]; then
    CLEANED=$(clean_report "$report")
    if [[ -n "$PR_DESCRIPTION" ]]; then
      PR_DESCRIPTION="${PR_DESCRIPTION}"$'\n\n'"${CLEANED}"
    else
      PR_DESCRIPTION="${CLEANED}"
    fi
  fi
done

PR_DESCRIPTION=$(head -c 3998 <<< "$PR_DESCRIPTION")

PR_JSON_FILE=$(mktemp)
python3 -c "
import json, sys
body = sys.stdin.read()
ids = '${REVIEWER_IDS:-}'.split()
pr = {
    'sourceRefName': 'refs/heads/${BRANCH_PREFIX}-${CURRENT_DATE}',
    'targetRefName': 'refs/heads/master',
    'title': 'Bump dependencies - ${CURRENT_DATE}',
    'description': body,
    'reviewers': [{'id': i} for i in ids]
}
json.dump(pr, open('${PR_JSON_FILE}', 'w'))
" <<< "$PR_DESCRIPTION"

API_URL="${SYSTEM_COLLECTIONURI}${SYSTEM_TEAMPROJECTID}/_apis/git/repositories/${BUILD_REPOSITORY_ID}/pullrequests?api-version=7.1"
echo "##[command]POST $API_URL"
HTTP_CODE=$(curl -s -o /tmp/pr_response.json -w "%{http_code}" \
  -X POST \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ${AZURE_TOKEN}" \
  -d @"$PR_JSON_FILE" \
  "$API_URL") || true

RESPONSE=$(cat /tmp/pr_response.json 2>/dev/null || true)
rm -f "$PR_JSON_FILE" /tmp/pr_response.json

if [[ "$HTTP_CODE" == "201" ]] && echo "$RESPONSE" | grep -q '"pullRequestId"'; then
  PR_ID=$(echo "$RESPONSE" | jq -r '.pullRequestId')
  echo "##[debug]Pull request #${PR_ID} created successfully."
  if [[ -n "${LINKED_WORK_ITEM:-}" ]]; then
    echo "##[debug]Linking work item ${LINKED_WORK_ITEM}."
    az repos pr work-item add --id "$PR_ID" --work-item "${LINKED_WORK_ITEM}"
  fi
  echo "##[debug]Setting auto-complete on PR #${PR_ID}."
  az repos pr update --id "$PR_ID" --auto-complete true
else
  echo "##[error]Failed to create pull request (HTTP $HTTP_CODE):"
  echo "$RESPONSE"
  exit 1
fi

echo "##[debug]Logging out of Azure DevOps."
az devops logout
