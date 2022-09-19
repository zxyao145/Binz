
dotnet pack Binz.Core/Binz.Core.csproj --configuration Release --output "./BinzPackage/"
dotnet pack Binz.Server/Binz.Server.csproj --configuration Release --output "./BinzPackage/"
dotnet pack Binz.Client/Binz.Client.csproj --configuration Release --output "./BinzPackage/"
dotnet pack Binz.Registry.Consul/Binz.Registry.Consul.csproj --configuration Release --output "./BinzPackage/"
dotnet pack Binz.Registry.Etcd/Binz.Registry.Etcd.csproj --configuration Release --output "./BinzPackage/"

