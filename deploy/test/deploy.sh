#!/bin/bash
set -e

# Test Environment Deployment Script

echo "================================"
echo "Deploying Fraud Detection (TEST)"
echo "================================"

# Required environment variables
: ${IMAGE_TAG:?"IMAGE_TAG is required (e.g., v2.0.0)"}
: ${SETTINGS_VERSION:?"SETTINGS_VERSION is required (e.g., v1)"}
: ${SECRETS_VERSION:?"SECRETS_VERSION is required (e.g., v1)"}

# Optional AppDynamics variables (set defaults if not provided)
export CORECLR_PROFILER=${CORECLR_PROFILER:-""}
export CORECLR_ENABLE_PROFILING=${CORECLR_ENABLE_PROFILING:-"0"}
export CORECLR_PROFILER_PATH=${CORECLR_PROFILER_PATH:-""}
export APPDYNAMICS_CONTROLLER_HOST_NAME=${APPDYNAMICS_CONTROLLER_HOST_NAME:-""}
export APPDYNAMICS_CONTROLLER_PORT=${APPDYNAMICS_CONTROLLER_PORT:-"443"}
export APPDYNAMICS_AGENT_ACCOUNT_ACCESS_KEY=${APPDYNAMICS_AGENT_ACCOUNT_ACCESS_KEY:-""}
export APPDYNAMICS_AGENT_ACCOUNT_NAME=${APPDYNAMICS_AGENT_ACCOUNT_NAME:-""}
export APPDYNAMICS_AGENT_APPLICATION_NAME=${APPDYNAMICS_AGENT_APPLICATION_NAME:-"FraudDetection"}
export APPDYNAMICS_AGENT_TIER_NAME=${APPDYNAMICS_AGENT_TIER_NAME:-"fraud-detection"}
export APPDYNAMICS_AGENT_REUSE_NODE_NAME_PREFIX=${APPDYNAMICS_AGENT_REUSE_NODE_NAME_PREFIX:-"fraud-detection"}

echo "Image Tag: ${IMAGE_TAG}"
echo "Settings Version: ${SETTINGS_VERSION}"
echo "Secrets Version: ${SECRETS_VERSION}"
echo ""

# Create Docker config from settings.json if needed
echo "Creating/Updating Docker Config..."
docker config create fraud-detection-settings-test-${SETTINGS_VERSION} settings.json 2>/dev/null || true

echo ""
echo "Deploying stack..."
docker stack deploy -c stack.template.yml fraud-detection-test

echo ""
echo "Deployment initiated!"
echo ""
echo "Monitor deployment:"
echo "  docker service ls | grep fraud-detection"
echo ""
echo "View logs:"
echo "  docker service logs -f fraud-detection-test_fraud-detection-api"
echo "  docker service logs -f fraud-detection-test_fraud-detection-background"
