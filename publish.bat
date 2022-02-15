rd /s /q .\dist\win-x64
rd /s /q .\ChessLib\bin
rd /s /q .\ChessLib\obj
rd /s /q .\CoreChess\bin
rd /s /q .\CoreChess\obj

dotnet clean CoreChess.sln -c Release
dotnet publish CoreChess.sln -c Release --runtime win-x64 -p:PublishReadyToRun=true --self-contained --output .\dist\win-x64
copy CoreChess\Lib\Bass\bass.dll dist\win-x64\