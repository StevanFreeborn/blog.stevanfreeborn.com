FROM mcr.microsoft.com/dotnet/sdk:10.0 AS setup-stage
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . .

FROM setup-stage as development-stage
RUN apt-get update
RUN apt-get install -y unzip
RUN curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l ~/vsdbg
CMD ["dotnet", "watch"]

FROM setup-stage as build-stage
RUN dotnet publish -c Release -o dist

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production-stage
WORKDIR /app
COPY --from=build-stage /app/dist ./
ENTRYPOINT ["dotnet", "Blog.dll"]
