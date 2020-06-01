#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${MYGET_JBOGARD_CI_API_KEY}" ]]
then
  echo "${0##*/}: MYGET_JBOGARD_CI_API_KEY is empty or not set. Skipped pushing package(s)."
else
  for package in $(find -name "*.nupkg" | grep "test" -v); do
    echo "${0##*/}: Pushing $package..."
    dotnet nuget push $package --source https://www.myget.org/F/jbogard-ci/api/v2/package --api-key ${MYGET_JBOGARD_CI_API_KEY}
  done
fi
