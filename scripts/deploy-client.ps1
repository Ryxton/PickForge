# PickForge Client - Deploy to S3
# Usage: .\scripts\deploy-client.ps1 [-ApiUrl "https://your-api.com"] [-S3Bucket "your-bucket"]

param(
    [Parameter(Mandatory=$false)]
    [string]$ApiUrl,
    
    [Parameter(Mandatory=$false)]
    [string]$S3Bucket = "pickforge-client-ryxton",
    
    [Parameter(Mandatory=$false)]
    [string]$CloudFrontDistributionId
)

$ErrorActionPreference = "Stop"
$AWS_REGION = "us-east-2"

Write-Host "🌐 PickForge Client - Deploy to S3" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "S3 Bucket: $S3Bucket"
Write-Host "Region: $AWS_REGION"
if ($ApiUrl) {
    Write-Host "API URL: $ApiUrl"
}
Write-Host ""

# Step 1: Build Client
Write-Host "🔨 Step 1: Building Blazor WASM client..." -ForegroundColor Yellow
try {
    Push-Location src/PickForge.Client
    dotnet publish -c Release -o ../../publish/client
    Pop-Location
    Write-Host "✅ Build successful" -ForegroundColor Green
}
catch {
    Write-Host "❌ Build failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Update API URL if provided
if ($ApiUrl) {
    Write-Host ""
    Write-Host "📝 Step 2: Updating API URL in configuration..." -ForegroundColor Yellow
    try {
        $configPath = "publish/client/wwwroot/appsettings.json"
        $config = Get-Content $configPath | ConvertFrom-Json
        $config.ApiBaseUrl = $ApiUrl
        $config | ConvertTo-Json | Set-Content $configPath
        Write-Host "✅ API URL updated to: $ApiUrl" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to update API URL: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host ""
    Write-Host "⚠️  Step 2: No API URL provided, using default from appsettings.json" -ForegroundColor Yellow
}

# Step 3: Upload to S3
Write-Host ""
Write-Host "📤 Step 3: Uploading to S3..." -ForegroundColor Yellow
try {
    # Upload all files except index.html and appsettings.json with long cache
    aws s3 sync publish/client/wwwroot/ s3://$S3Bucket/ `
        --delete `
        --cache-control "public, max-age=31536000, immutable" `
        --exclude "index.html" `
        --exclude "appsettings.json" `
        --exclude "appsettings.*.json" `
        --region $AWS_REGION
    
    # Upload index.html with no-cache
    aws s3 cp publish/client/wwwroot/index.html s3://$S3Bucket/ `
        --cache-control "no-cache, no-store, must-revalidate" `
        --region $AWS_REGION
    
    # Upload appsettings.json with no-cache
    aws s3 cp publish/client/wwwroot/appsettings.json s3://$S3Bucket/ `
        --cache-control "no-cache, no-store, must-revalidate" `
        --region $AWS_REGION
    
    Write-Host "✅ Upload successful" -ForegroundColor Green
}
catch {
    Write-Host "❌ Upload failed: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Invalidate CloudFront cache (if distribution ID provided)
if ($CloudFrontDistributionId) {
    Write-Host ""
    Write-Host "🔄 Step 4: Invalidating CloudFront cache..." -ForegroundColor Yellow
    try {
        aws cloudfront create-invalidation `
            --distribution-id $CloudFrontDistributionId `
            --paths "/*" `
            --region $AWS_REGION
        Write-Host "✅ CloudFront cache invalidated" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ CloudFront invalidation failed: $_" -ForegroundColor Red
        exit 1
    }
}

# Step 5: Display URLs
Write-Host ""
Write-Host "✅ Deployment Complete!" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "S3 Website URL: http://$S3Bucket.s3-website.$AWS_REGION.amazonaws.com"
if ($CloudFrontDistributionId) {
    Write-Host "CloudFront URL: (check AWS Console for distribution domain)"
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Test the S3 website URL above"
Write-Host "2. If using CloudFront, test the CloudFront URL"
Write-Host "3. Update DNS if using custom domain"