# Blog

## Setup for development

### Clone the repository

```sh
git clone https://github.com/StevanFreeborn/blog.stevanfreeborn.com.git
```

### Generate development SSL certificates

```sh
dotnet dev-certs https -ep ${HOME}/.aspnet/https/Blog.pfx -p <password>
dotnet dev-certs https --trust
```

```powershell
dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\Blog.pfx -p <password> --trust
```

### Developing with Docker

#### Create `.env.development` file

```sh
cp src/Blog/example.env.development src/Blog/.env.development
```

#### Build and run the Docker container

```sh
docker-compose -f docker-compose.dev.yml up --build
```

### Developing without Docker

#### Create `appsettings.Development.json` file

```sh
cp src/Blog/appsettings.example.json src/Blog/appsettings.Development.json
```

#### Install dependencies

```sh
cd src/Blog
dotnet restore
```

#### Run the application

```sh
dotnet run
```
