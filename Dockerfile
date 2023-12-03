#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-image
WORKDIR /home/app
# Make port 80 available for links and/or publish
COPY ./*.sln ./
COPY ./*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done
RUN dotnet restore
COPY . .
RUN dotnet test ./Tests/Tests.csproj
RUN dotnet publish ./SleekCompanyEmployees.API/SleekCompanyEmployees.API.csproj -o /publish/
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /publish
COPY --from=build-image /publish .
ENV ASPNETCORE_URLS=http://+:5005
ENTRYPOINT ["dotnet", "SleekCompanyEmployees.API.dll"]
#god is good all the time



