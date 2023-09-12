EXE = Chess-Challenge
ifeq ($(OS),Windows_NT)
	EXE = $(EXE).exe
endif

all:
	dotnet publish -c Release . -o Build -p:PublishSingleFile=true --self-contained true -r $(OS) -p:DefineConstants="UCI"
	cp Build/$(EXE) ../$(EXE)