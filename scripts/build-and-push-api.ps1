# PickForge API - Build and Push to AWS ECR (PowerShell)
# Usage: .\scripts\build-and-push-api.ps1 [-Tag "latest"]

param(
    [string]$Tag = "latest"
)

$ErrorActionPreference = "Stop"

# Configuration
$AWS_REGION = "us-east-2"
$ECR_REGISTRY = "413211168326.dkr.ecr.us-east-2.amazonaws.com"
$ECR_REPO_NAME = "pickforge-api"
$IMAGE_NAME = "pickforge-api"

Write-Host "🚀 PickForge API - Build and Push to ECR" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Region: $AWS_REGION"
Write-Host "Registry: $ECR_REGISTRY"
Write-Host "Repository: $ECR_REPO_NAME"
Write-Host "Tag: $Tag"
Write-Host ""

# Step 0: Sanity check
Write-Host "📋 Step 0: Verifying AWS configuration..." -ForegroundColor Yellow
try {
    aws sts get-caller-identity
    aws ecr describe-repositories --repository-names $ECR_REPO_NAME --region $AWS_REGION
    Write-Host "✅ AWS configuration verified" -ForegroundColor Green
}
catch {
    Write-Host "❌ AWS verification failed: $_" -ForegroundColor Red
    exit 1
}

# Step 1: Authenticate Docker to ECR (registry host only, no repo suffix)
Write-Host ""
Write-Host "📝 Step 1: Authenticating Docker to ECR..." -ForegroundColor Yellow
try {
    $password = aws ecr get-login-password --region $AWS_REGION
    $password | docker login --username AWS --password-stdin $ECR_REGISTRY
    Write-Host "✅ Authentication successful" -ForegroundColor Green
}
catch {
    Write-Host "❌ Authentication failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Build Docker image
Write-Host ""
Write-Host "🔨 Step 2: Building Docker image..." -ForegroundColor Yellow
try {
    docker build `
        -t "${IMAGE_NAME}:${Tag}" `
        -f src/PickForge.Api/Dockerfile `
        .
    Write-Host "✅ Build successful" -ForegroundColor Green
}
catch {
    Write-Host "❌ Build failed: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Tag image with full repo URI
Write-Host ""
Write-Host "🏷️  Step 3: Tagging image for ECR..." -ForegroundColor Yellow
try {
    docker tag "${IMAGE_NAME}:${Tag}" "${ECR_REGISTRY}/${ECR_REPO_NAME}:${Tag}"
    
    if ($Tag -ne "latest") {
        docker tag "${IMAGE_NAME}:${Tag}" "${ECR_REGISTRY}/${ECR_REPO_NAME}:latest"
        Write-Host "✅ Tagged as $Tag and latest" -ForegroundColor Green
    }
    else {
        Write-Host "✅ Tagged as latest" -ForegroundColor Green
    }
}
catch {
    Write-Host "❌ Tagging failed: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Push to ECR
Write-Host ""
Write-Host "📤 Step 4: Pushing image to ECR..." -ForegroundColor Yellow
try {
    docker push "${ECR_REGISTRY}/${ECR_REPO_NAME}:${Tag}"
    
    if ($Tag -ne "latest") {
        docker push "${ECR_REGISTRY}/${ECR_REPO_NAME}:latest"
    }
    Write-Host "✅ Push successful" -ForegroundColor Green
}
catch {
    Write-Host "❌ Push failed: $_" -ForegroundColor Red
    exit 1
}

# Step 5: Verify tag exists
Write-Host ""
Write-Host "🔍 Step 5: Verifying image in ECR..." -ForegroundColor Yellow
try {
    aws ecr list-images --repository-name $ECR_REPO_NAME --region $AWS_REGION
    Write-Host "✅ Verification complete" -ForegroundColor Green
}
catch {
    Write-Host "❌ Verification failed: $_" -ForegroundColor Red
}

# Step 6: Display image info
Write-Host ""
Write-Host "✅ Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Image URI: ${ECR_REGISTRY}/${ECR_REPO_NAME}:${Tag}"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Deploy to App Runner:"
Write-Host "   aws apprunner start-deployment --service-arn YOUR_SERVICE_ARN"
Write-Host ""
Write-Host "2. Or create new App Runner service:"
Write-Host "   aws apprunner create-service --cli-input-json file://apprunner-service.json"
Write-Host ""
Write-Host "3. Test locally:"
Write-Host "   docker run -p 8080:8080 ${IMAGE_NAME}:${Tag}"
Write-Host "   curl http://localhost:8080/ping"
