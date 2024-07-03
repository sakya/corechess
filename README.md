# **CoreChess**

CoreChess is an open source chess GUI for chess engines
![image](https://github.com/sakya/corechess/assets/289552/43725612-5c77-4be0-9adf-d5a8b1705f97)
- Human vs Engine mode
- Human vs Human  mode.
- Engine vs Engine mode.
## Supported engines

- UCI (Universal Chess Interface) like [Stockfish](https://stockfishchess.org/), [Komodo/Dragon](https://komodochess.com/) and [Leela Chess Zero](https://lczero.org/)

- CECP (Chess Engine Communication Protocol) like [Crafty](https://craftychess.com/)

- The King (Chessmaster). Supported only on Windows.

### The King (Chessmaster)

  To use The King (the Chessmaster engine) you need the file TheKing333.exe (Chessmaster 10), the "Personalities" folder and the "Opening Books" folder and patch the engine using the [OPK patch](https://web.archive.org/web/20070930221944/http://www.freewebs.com/jakent/) that disables the copy protection.

  You can then set the personalities path and opening books path in the engine settings.

## Supported opening book formats

- Polyglot (BIN)
- Arena opening book (ABK)
- Chessmaster (OBK)

## Screenshots
![image](https://github.com/sakya/corechess/assets/289552/67f43b11-df2a-4716-af74-ca1ce1e584c3)

![image](https://github.com/sakya/corechess/assets/289552/6037c09b-0f77-4930-846c-aeb59c618af4)

## Build from source
### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Build instructions
- Download the source code of the latest [release](https://github.com/sakya/corechess/releases)
- Unzip the source code
- With a terminal/command prompt enter the directory corechess-x.x.x.x
- Execute the build command for your OS

#### Linux
`dotnet publish CoreChess.sln -c Release --runtime linux-x64 -p:PublishReadyToRun=true --self-contained --output ./dist/linux-x64`

#### Windows
`dotnet publish CoreChess.sln -c Release --runtime win-x64 -p:PublishReadyToRun=true --self-contained --output .\dist\win-x64`

#### macOS
`dotnet publish CoreChess.sln -c Release --runtime osx-x64 -p:PublishReadyToRun=true --self-contained --output .\dist\osx-x64`

## Download
<a href="https://flathub.org/apps/details/com.github.sakya.corechess" align="center">
  <img width="200" src="https://flathub.org/assets/badges/flathub-badge-en.png">
</a>
<br/>
<a href="https://github.com/sakya/corechess/releases" align="center">
  <img width="100" src="https://user-images.githubusercontent.com/289552/156829426-1d0a50be-8adf-4c06-bfee-259378b974a3.png">
</a>
