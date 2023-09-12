
all:
	dotnet publish -c Release . -o Build -p:PublishSingleFile=true --self-contained true -r $(OS) -p:DefineConstants="UCI"