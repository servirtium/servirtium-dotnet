FROM mcr.microsoft.com/dotnet/aspnet:3.1
COPY Servirtium.StandaloneServer/bin/Release/netcoreapp3.1/ Servirtium/
WORKDIR /Servirtium
ENTRYPOINT ["dotnet", "Servirtium.StandaloneServer.dll"]
CMD ["record", "http://todo-backend-sinatra.herokuapp.com"]