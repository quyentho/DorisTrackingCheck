#!/bin/bash

# Exit on error
set -e

echo "Building frontend with webpack..."
yarn build

echo "Deploying to wesaam-host-1..."
yarn push

echo "✅ Frontend deployment complete!"
