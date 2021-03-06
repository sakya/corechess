#!/bin/bash
dotnet clean CoreChess.sln -c Release
dotnet publish CoreChess.sln -c Release --runtime linux-x64 -p:PublishReadyToRun=true --self-contained --output ./dist/linux-x64
cp CoreChess/Lib/Bass/linux-x64/libbass.so dist/linux-x64/
cp icon.png dist/linux-x64/corechess.png
