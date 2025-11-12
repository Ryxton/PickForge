#!/bin/bash
# PickForge API - Build and Push to AWS ECR
# Usage: ./scripts/build-and-push-api.sh [tag]

set -e  # Exit on error

# Configuration
AWS_REGION="us-east-2"
ECR_REGISTRY="413211168326.dkr.ecr.us-east-2.amazonaws.com"
ECR_REPO_NAME="pickforge-api"
IMAGE_NAME="pickforge-api"
TAG="${1:-latest}"

echo "🚀 PickForge API - Build and Push to ECR"
echo "========================================"
echo "Region: $AWS_REGION"
echo "Registry: $ECR_REGISTRY"
echo "Repository: $ECR_REPO_NAME"
echo "Tag: $TAG"
echo ""

# Step 0: Sanity check
echo "📋 Step 0: Verifying AWS configuration..."
aws sts get-caller-identity
aws ecr describe-repositories --repository-names $ECR_REPO_NAME --region $AWS_REGION
echo "✅ AWS configuration verified"
echo ""

# Step 1: Authenticate Docker to ECR (registry host only, no repo suffix)
echo "📝 Step 1: Authenticating Docker to ECR..."
aws ecr get-login-password --region $AWS_REGION | \
    docker login --username AWS --password-stdin $ECR_REGISTRY

if [ $? -eq 0 ]; then
    echo "✅ Authentication successful"
else
    echo "❌ Authentication failed"
    exit 1
fi

# Step 2: Build Docker image
echo ""
echo "🔨 Step 2: Building Docker image..."
docker build \
    -t $IMAGE_NAME:$TAG \
    -f src/PickForge.Api/Dockerfile \
    .

if [ $? -eq 0 ]; then
    echo "✅ Build successful"
else
    echo "❌ Build failed"
    exit 1
fi

# Step 3: Tag image with full repo URI
echo ""
echo "🏷️  Step 3: Tagging image for ECR..."
docker tag $IMAGE_NAME:$TAG $ECR_REGISTRY/$ECR_REPO_NAME:$TAG

if [ $TAG != "latest" ]; then
    docker tag $IMAGE_NAME:$TAG $ECR_REGISTRY/$ECR_REPO_NAME:latest
    echo "✅ Tagged as $TAG and latest"
else
    echo "✅ Tagged as latest"
fi

# Step 4: Push to ECR
echo ""
echo "📤 Step 4: Pushing image to ECR..."
docker push $ECR_REGISTRY/$ECR_REPO_NAME:$TAG

if [ $TAG != "latest" ]; then
    docker push $ECR_REGISTRY/$ECR_REPO_NAME:latest
fi

if [ $? -eq 0 ]; then
    echo "✅ Push successful"
else
    echo "❌ Push failed"
    exit 1
fi

# Step 5: Verify tag exists
echo ""
echo "🔍 Step 5: Verifying image in ECR..."
aws ecr list-images --repository-name $ECR_REPO_NAME --region $AWS_REGION
echo "✅ Verification complete"

# Step 6: Display image info
echo ""
echo "✅ Deployment Complete!"
echo "========================================"
echo "Image URI: $ECR_REGISTRY/$ECR_REPO_NAME:$TAG"
echo ""
echo "Next steps:"
echo "1. Deploy to App Runner:"
echo "   aws apprunner start-deployment --service-arn <your-service-arn>"
echo ""
echo "2. Or create new App Runner service:"
echo "   aws apprunner create-service --cli-input-json file://apprunner-service.json"
echo ""
echo "3. Test locally:"
echo "   docker run -p 8080:8080 $IMAGE_NAME:$TAG"
echo "   curl http://localhost:8080/ping"