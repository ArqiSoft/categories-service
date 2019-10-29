# Use SDK image to build solution
FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build-env

ARG RID=linux-x64

WORKDIR /build

COPY Leanda.Categories.Processing Leanda.Categories.Processing/

RUN dotnet restore Leanda.Categories.Processing/Leanda.Categories.Processing.csproj

RUN dotnet publish Leanda.Categories.Processing/Leanda.Categories.Processing.csproj -r $RID -o /dist

# RUN apt-get update && apt-get install -y --no-install-recommends apt-utils
# RUN apt-get install -y libunwind8

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.0

LABEL maintainer="rick.zakharov@gmail.com"

WORKDIR /app

RUN apt-get update && apt-get install -y curl
RUN curl https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh > /app/wait-for-it.sh && chmod 777 /app/wait-for-it.sh

COPY --from=build-env /dist ./

ENTRYPOINT ["./Leanda.Categories.Processing"]
