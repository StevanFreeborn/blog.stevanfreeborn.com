$NGINX_CONFIG_PATHS = @(
  "/etc/nginx/sites-available/blog.stevanfreeborn.com",
  "/etc/nginx/sites-available/blog.steva.nz"
)
$BLUE_PORT = 5001
$GREEN_PORT = 5002

function StartContainer 
{
  param (
    [string]$containerColor,
    [string]$dockerTag,
    [int]$hostPort
  )

  $containerName = "blog.stevanfreeborn.com.$containerColor"
  $postDirEnv = "FilePostServiceOptions__PostsDirectory=wwwroot/posts"
  docker run -d --restart always -p "${hostPort}:8080" --name $containerName --env $postDirEnv $dockerTag | Out-Null

  if ($LASTEXITCODE -ne 0) 
  {
    Write-Host "Failed to run $containerName container."
    exit 1;
  }

  $running = $false
  $attemptLimit = 12
  $attempts = 0
  $waitTimeInSeconds = 10

  while ($running -eq $false -and $attempts -lt $attemptLimit)
  {
    try 
    {
      $response = Invoke-WebRequest -Uri "http://localhost:$hostPort/rss" -UseBasicParsing
    
      if ($response.StatusCode -eq 200) 
      {
        $running = $true
      }
      else 
      {
        Write-Host "Waiting for $containerName container to start..."
        Start-Sleep -Seconds $waitTimeInSeconds
        $attempts++
      }
    }
    catch 
    {
      Write-Host "Waiting for $containerName container to start..."
      Start-Sleep -Seconds $waitTimeInSeconds
      $attempts++
    }
  }

  if ($running -eq $false) 
  {
    Write-Host "Failed to start $containerName container."
    exit 1
  }
}

function UpdateNginxConfig 
{
  param (
    [string]$filePath,
    [string]$portNumber
  )

  $nginxConfig = Get-Content $filePath
  $pattern = "proxy_pass http://localhost:\d+;"
  $replacement = "proxy_pass http://localhost:$portNumber;"

  $modifiedContent = @()

  foreach ($line in $nginxConfig)
  {
    if ($line -match $pattern)
    {
      $line = $line -replace $pattern, $replacement
    }

    $modifiedContent += $line
  }

  Set-Content -Path $filePath -Value $modifiedContent
}

$version = $args[0];

if ($null -eq $version) 
{
  Write-Host "Please provide a version number."
  exit 1
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) 
{
  Write-Host "Docker is not installed. Please install Docker and try again."
  exit 1
}

foreach ($configPath in $NGINX_CONFIG_PATHS)
{
  if (-not (Test-Path $configPath)) 
  {
    Write-Host "Nginx configuration file not found: $configPath"
    exit 1
  }
}

$dockerTag = "stevanfreeborn/blog.stevanfreeborn.com:$version"
docker pull $dockerTag

if ($LASTEXITCODE -ne 0) 
{
  Write-Host "Failed to pull docker image: $dockerTag"
  exit 1
}

$blueContainerId = docker ps --filter "name=blog.stevanfreeborn.com.blue" --format "{{.ID}}"
$greenContainerId = docker ps --filter "name=blog.stevanfreeborn.com.green" --format "{{.ID}}"

if ($null -ne $blueContainerId) 
{
  $targetColor = "green"
  $targetPort = $GREEN_PORT
  $oldContainerId = $blueContainerId
  $oldColor = "blue"
}
else 
{
  $targetColor = "blue"
  $targetPort = $BLUE_PORT
  $oldContainerId = $greenContainerId
  $oldColor = "green"
}

Write-Host "Deploying to $targetColor container on port $targetPort."

StartContainer -containerColor $targetColor -dockerTag $dockerTag -hostPort $targetPort

Write-Host "$targetColor container is running."

$configBackups = @{}

foreach ($configPath in $NGINX_CONFIG_PATHS)
{
  $configBackups[$configPath] = Get-Content $configPath
}

foreach ($configPath in $NGINX_CONFIG_PATHS)
{
  UpdateNginxConfig -filePath $configPath -portNumber $targetPort
  Write-Host "Nginx configuration updated: $configPath to point to $targetColor container on port $targetPort."
}

nginx -t

if ($LASTEXITCODE -ne 0) 
{
  Write-Host "Nginx configuration test failed. Reverting changes."
  
  foreach ($configPath in $NGINX_CONFIG_PATHS)
  {
    Set-Content -Path $configPath -Value $configBackups[$configPath]
  }

  exit 1
}

nginx -s reload

if ($LASTEXITCODE -ne 0) 
{
  Write-Host "Failed to reload Nginx."
  exit 1
}

Write-Host "Nginx reloaded. Successfully deployed version $version."

if ($null -ne $oldContainerId) 
{
  Write-Host "Stopping and removing $oldColor container."

  docker stop $oldContainerId
  docker rm $oldContainerId

  Write-Host "$oldColor container stopped and removed."
}

exit 0
