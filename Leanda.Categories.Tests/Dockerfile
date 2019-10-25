FROM microsoft/dotnet:2.1-sdk AS build-env

ARG RID=linux-x64

WORKDIR /build

COPY Leanda.Categories.Tests/Leanda.Categories.Tests.csproj Leanda.Categories.Tests/

RUN dotnet restore Leanda.Categories.Tests/Leanda.Categories.Tests.csproj

COPY Leanda.Categories.Tests Leanda.Categories.Tests

RUN dotnet publish Leanda.Categories.Tests/Leanda.Categories.Tests.csproj -r $RID -c Release -o /dist

# Build runtime image
FROM microsoft/dotnet:2.1-sdk

LABEL maintainer="rick.zakharov@arqisoft.com"

WORKDIR /app

RUN apt-get update && apt-get install -y curl
RUN curl https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh > /app/wait-for-it.sh && chmod 777 /app/wait-for-it.sh

COPY --from=build-env /dist ./

ENTRYPOINT ["dotnet", "vstest", "./Leanda.Categories.Tests.dll"]
