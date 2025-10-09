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

foreach ($configPath in $NGINX_CONFIG_PATHS)
{
  if (-not (Test-Path $configPath)) 
  {
    Write-Host "Nginx configuration file not found: $configPath"
    exit 1
  }
}

# attempt to pull docker image
$dockerTag = "stevanfreeborn/blog.stevanfreeborn.com:$version"
docker pull $dockerTag

if ($LASTEXITCODE -ne 0) 
{
  Write-Host "Failed to pull docker image: $dockerTag"
  exit 1
}

# Check if blue container is running
$blueContainerId = docker ps --filter "name=blog.stevanfreeborn.com.blue" --format "{{.ID}}"

if ($null -eq $blueContainerId) 
{
  Write-Host "Blue container is not running. Starting blue container."

  StartContainer -containerColor "blue" -dockerTag $dockerTag -hostPort $BLUE_PORT

  Write-Host "Blue container is running."
  
  # Backup original configs before making changes
  $configBackups = @{}

  foreach ($configPath in $NGINX_CONFIG_PATHS)
  {
    $configBackups[$configPath] = Get-Content $configPath
  }

  foreach ($configPath in $NGINX_CONFIG_PATHS)
  {
    UpdateNginxConfig -filePath $configPath -portNumber $BLUE_PORT
    Write-Host "Nginx configuration updated: $configPath to point to blue container on port $BLUE_PORT."
  }

  Write-Host "All Nginx configurations updated to point to blue container on port $BLUE_PORT."

  nginx -t

  if ($LASTEXITCODE -ne 0) 
  {
    Write-Host "Nginx configuration test failed. Reverting changes."
    foreach ($configPath in $NGINX_CONFIG_PATHS)
    {
      Set-Content -Path $configPath -Value $configBackups[$configPath]
      Write-Host "Reverted: $configPath"
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
  exit 0
}
else 
{
  Write-Host "Blue container is running. Starting green container."

  StartContainer -containerColor "green" -dockerTag $dockerTag -hostPort $GREEN_PORT

  Write-Host "Green container is running."

  # Backup original configs before making changes
  $configBackups = @{}

  foreach ($configPath in $NGINX_CONFIG_PATHS)
  {
    $configBackups[$configPath] = Get-Content $configPath
  }

  foreach ($configPath in $NGINX_CONFIG_PATHS)
  {
    UpdateNginxConfig -filePath $configPath -portNumber $GREEN_PORT
    Write-Host "Nginx configuration updated: $configPath to point to green container on port $GREEN_PORT."
  }

  Write-Host "All Nginx configurations updated to point to green container on port $GREEN_PORT."

  nginx -t

  if ($LASTEXITCODE -ne 0) 
  {
    Write-Host "Nginx configuration test failed. Reverting changes."
    foreach ($configPath in $NGINX_CONFIG_PATHS)
    {
      Set-Content -Path $configPath -Value $configBackups[$configPath]
      Write-Host "Reverted: $configPath"
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
