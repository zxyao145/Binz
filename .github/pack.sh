
dotnet pack src/Binz.Server/Binz.Server.csproj --configuration Release --output "./.packages/"
dotnet pack src/Binz.Client/Binz.Client.csproj --configuration Release --output "./.packages/"
dotnet pack src/Binz.Registry.Consul/Binz.Registry.Consul.csproj --configuration Release --output "./.packages/"
dotnet pack src/Binz.Registry.Etcd/Binz.Registry.Etcd.csproj --configuration Release --output "./.packages/"

