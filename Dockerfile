FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Enterprise-RAG-Platform.csproj ./
COPY src/Enterprise.Rag.Domain/Enterprise.Rag.Domain.csproj src/Enterprise.Rag.Domain/
COPY src/Enterprise.Rag.Application/Enterprise.Rag.Application.csproj src/Enterprise.Rag.Application/
COPY src/Enterprise.Rag.Infrastructure/Enterprise.Rag.Infrastructure.csproj src/Enterprise.Rag.Infrastructure/
RUN dotnet restore Enterprise-RAG-Platform.csproj
COPY . .
RUN dotnet publish Enterprise-RAG-Platform.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Enterprise-RAG-Platform.dll"]
