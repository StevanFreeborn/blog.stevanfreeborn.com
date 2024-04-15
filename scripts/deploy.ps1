$NGINX_CONFIG_PATH = "/etc/nginx/sites-available/blog.stevanfreeborn.com"

function StartContainer 
{
  param (
    [string]$containerColor,
    [string]$dockerTag
  )

  $containerName = "blog.stevanfreeborn.com.$containerColor"
  $postDirEnv = "FilePostServiceOptions__PostsDirectory=wwwroot/posts"
  $containerId = docker run -d -p 8080 --name $containerName --env $postDirEnv $dockerTag

  if ($LASTEXITCODE -ne 0) 
  {
    Write-Host "Failed to run $containerName container."
    exit 1;
  }

  $containerHostPort = $(docker port $containerId).Split(":")[1]
  $running = $false
  $attemptLimit = 12
  $attempts = 0
  $waitTimeInSeconds = 10

  while ($running -eq $false -and $attempts -lt $attemptLimit)
  {
    try 
    {
      $response = Invoke-WebRequest -Uri "http://localhost:$containerHostPort/rss" -UseBasicParsing
    
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

  return $containerHostPort
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

  foreach ($line in $nginxConfig) {
    if ($line -match $pattern) {
        $line = $line -replace $pattern, $replacement
    }

    $modifiedContent += $line
  }

  Set-Content -Path $filePath -Value $modifiedContent
}

# Get the version number from the command line
$version = $args[0];

if ($null -eq $version) 
{
  Write-Host "Please provide a version number."
  exit 1
}

# check if docker is installed
$dockerVersion = docker --version

if ($null -eq $dockerVersion) 
{
  Write-Host "Docker is not installed. Please install Docker and try again."
  exit 1
}

if (-not (Test-Path $NGINX_CONFIG_PATH)) 
{
  Write-Host "Nginx configuration file not found: $NGINX_CONFIG_PATH"
  exit 1
}

# attempt to pull docker image
$dockerTag = "stevanfreeborn/blog.stevanfreeborn.com:$version"
docker pull $dockerTag

if ($LASTEXITCODE -ne 0) 
{
  Write-Host "Failed to pull docker image: $dockerTag"
  exit 1
}

# Checkif green container is running
$blueContainerId = docker ps --filter "name=blog.stevanfreeborn.com.blue" --format "{{.ID}}"

if ($null -eq $blueContainerId) 
{
  Write-Host "Blue container is not running. Starting blue container."

  $blueContainerHostPort = StartContainer -containerColor "blue" -dockerTag $dockerTag

  Write-Host "Blue container is running."
  
  UpdateNginxConfig -filePath $NGINX_CONFIG_PATH -portNumber $blueContainerHostPort

  Write-Host "Nginx configuration updated to point to blue container on port $blueContainerHostPort."

  nginx -t

  if ($LASTEXITCODE -ne 0) 
  {
    Write-Host "Nginx configuration test failed. Reverting changes."
    Set-Content -Path $filePath -Value $nginxConfig
    exit 1
  }

  nginx -s reload

  if ($LASTEXITCODE -ne 0) 
  {
    Write-Host "Failed to reload Nginx."
    exit 1
  }

  Write-Host "Nginx reloaded. Successfully deployed version $version."
  exit 0
}
else 
{
  Write-Host "Blue container is running. Starting green container."

  $greenContainerHostPort = StartContainer -containerColor "green" -dockerTag $dockerTag

  Write-Host "Green container is running."

  UpdateNginxConfig -filePath $NGINX_CONFIG_PATH -portNumber $greenContainerHostPort

  Write-Host "Nginx configuration updated to point to green container on port $greenContainerHostPort."

  nginx -t

  if ($LASTEXITCODE -ne 0) 
  {
    Write-Host "Nginx configuration test failed. Reverting changes."
    Set-Content -Path $filePath -Value $nginxConfig
    exit 1
  }

  nginx -s reload

  if ($LASTEXITCODE -ne 0) 
  {
    Write-Host "Failed to reload Nginx."
    exit 1
  }

  Write-Host "Nginx reloaded. Successfully deployed version $version."

  Write-Host "Stopping and removing blue container."
  
  docker stop $blueContainerId

  if ($LASTEXITCODE -ne 0) 
  {
    Write-Host "Failed to stop blue container."
    exit 1
  }

  docker rm $blueContainerId

  if ($LASTEXITCODE -ne 0) 
  {
    Write-Host "Failed to remove blue container."
    exit 1
  }

  Write-Host "Blue container stopped and removed."

  Write-Host "Switching green container to blue container."

  docker rename "blog.stevanfreeborn.com.green" "blog.stevanfreeborn.com.blue"

  if ($LASTEXITCODE -ne 0) 
  {
    Write-Host "Failed to rename green container to blue container."
    exit 1
  }

  Write-Host "Successfully deployed version $version."

  exit 0
}
