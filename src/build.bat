dotnet publish --self-contained true --runtime win-x64 --configuration Release -p:PublishSingleFile=true --output TokenPay\bin\Release\win-x64\
dotnet publish --self-contained true --runtime win-x86 --configuration Release -p:PublishSingleFile=true --output TokenPay\bin\Release\win-x86\
dotnet publish --self-contained true --runtime win-arm --configuration Release -p:PublishSingleFile=true --output TokenPay\bin\Release\win-arm\
dotnet publish --self-contained true --runtime win-arm64 --configuration Release -p:PublishSingleFile=true --output TokenPay\bin\Release\win-arm64\
dotnet publish --self-contained true --runtime linux-x64 --configuration Release -p:PublishSingleFile=true --output TokenPay\bin\Release\linux-x64\
dotnet publish --self-contained true --runtime linux-arm --configuration Release -p:PublishSingleFile=true --output TokenPay\bin\Release\linux-arm\
dotnet publish --self-contained true --runtime linux-arm64 --configuration Release -p:PublishSingleFile=true --output TokenPay\bin\Release\linux-arm64\
dotnet publish --self-contained true --runtime osx-x64 --configuration Release -p:PublishSingleFile=true --output TokenPay\bin\Release\osx-x64\
dotnet publish --self-contained true --runtime osx-arm64 --configuration Release -p:PublishSingleFile=true --output TokenPay\bin\Release\osx-arm64\
rmdir /s /q TokenPay\bin\Release\win-x64\dist
del TokenPay\bin\Release\win-x64\appsettings.Example.json
del TokenPay\bin\Release\win-x64\appsettings.json
del TokenPay\bin\Release\win-x64\EVMChains.Example.json
del TokenPay\bin\Release\win-x64\EVMChains.json
del TokenPay\bin\Release\win-x64\TokenPay.pdb
del TokenPay\bin\Release\win-x64\TokenPay.xml
rmdir /s /q TokenPay\bin\Release\win-x86\dist
del TokenPay\bin\Release\win-x86\appsettings.Example.json
del TokenPay\bin\Release\win-x86\appsettings.json
del TokenPay\bin\Release\win-x86\EVMChains.Example.json
del TokenPay\bin\Release\win-x86\EVMChains.json
del TokenPay\bin\Release\win-x86\TokenPay.pdb
del TokenPay\bin\Release\win-x86\TokenPay.xml
rmdir /s /q TokenPay\bin\Release\win-arm\dist
del TokenPay\bin\Release\win-arm\appsettings.Example.json
del TokenPay\bin\Release\win-arm\appsettings.json
del TokenPay\bin\Release\win-arm\EVMChains.Example.json
del TokenPay\bin\Release\win-arm\EVMChains.json
del TokenPay\bin\Release\win-arm\TokenPay.pdb
del TokenPay\bin\Release\win-arm\TokenPay.xml
rmdir /s /q TokenPay\bin\Release\win-arm64\dist
del TokenPay\bin\Release\win-arm64\appsettings.Example.json
del TokenPay\bin\Release\win-arm64\appsettings.json
del TokenPay\bin\Release\win-arm64\EVMChains.Example.json
del TokenPay\bin\Release\win-arm64\EVMChains.json
del TokenPay\bin\Release\win-arm64\TokenPay.pdb
del TokenPay\bin\Release\win-arm64\TokenPay.xml
rmdir /s /q TokenPay\bin\Release\linux-x64\dist
del TokenPay\bin\Release\linux-x64\appsettings.Example.json
del TokenPay\bin\Release\linux-x64\appsettings.json
del TokenPay\bin\Release\linux-x64\EVMChains.Example.json
del TokenPay\bin\Release\linux-x64\EVMChains.json
del TokenPay\bin\Release\linux-x64\TokenPay.pdb
del TokenPay\bin\Release\linux-x64\TokenPay.xml
rmdir /s /q TokenPay\bin\Release\linux-arm\dist
del TokenPay\bin\Release\linux-arm\appsettings.Example.json
del TokenPay\bin\Release\linux-arm\appsettings.json
del TokenPay\bin\Release\linux-arm\EVMChains.Example.json
del TokenPay\bin\Release\linux-arm\EVMChains.json
del TokenPay\bin\Release\linux-arm\TokenPay.pdb
del TokenPay\bin\Release\linux-arm\TokenPay.xml
rmdir /s /q TokenPay\bin\Release\linux-arm64\dist
del TokenPay\bin\Release\linux-arm64\appsettings.Example.json
del TokenPay\bin\Release\linux-arm64\appsettings.json
del TokenPay\bin\Release\linux-arm64\EVMChains.Example.json
del TokenPay\bin\Release\linux-arm64\EVMChains.json
del TokenPay\bin\Release\linux-arm64\TokenPay.pdb
del TokenPay\bin\Release\linux-arm64\TokenPay.xml
rmdir /s /q TokenPay\bin\Release\osx-x64\dist
del TokenPay\bin\Release\osx-x64\appsettings.Example.json
del TokenPay\bin\Release\osx-x64\appsettings.json
del TokenPay\bin\Release\osx-x64\EVMChains.Example.json
del TokenPay\bin\Release\osx-x64\EVMChains.json
del TokenPay\bin\Release\osx-x64\TokenPay.pdb
del TokenPay\bin\Release\osx-x64\TokenPay.xml
rmdir /s /q TokenPay\bin\Release\osx-arm64\dist
del TokenPay\bin\Release\osx-arm64\appsettings.Example.json
del TokenPay\bin\Release\osx-arm64\appsettings.json
del TokenPay\bin\Release\osx-arm64\EVMChains.Example.json
del TokenPay\bin\Release\osx-arm64\EVMChains.json
del TokenPay\bin\Release\osx-arm64\TokenPay.pdb
del TokenPay\bin\Release\osx-arm64\TokenPay.xml
del TokenPay\bin\Release\TokenPay_win-x64.zip
del TokenPay\bin\Release\TokenPay_win-x86.zip
del TokenPay\bin\Release\TokenPay_win-arm.zip
del TokenPay\bin\Release\TokenPay_win-arm64.zip
del TokenPay\bin\Release\TokenPay_linux-x64.zip
del TokenPay\bin\Release\TokenPay_linux-arm.zip
del TokenPay\bin\Release\TokenPay_linux-arm64.zip
del TokenPay\bin\Release\TokenPay_osx-x64.zip
del TokenPay\bin\Release\TokenPay_osx-arm64.zip
powershell Compress-Archive TokenPay\bin\Release\win-x64\ TokenPay\bin\Release\TokenPay_win-x64.zip
powershell Compress-Archive TokenPay\bin\Release\win-x86\ TokenPay\bin\Release\TokenPay_win-x86.zip
powershell Compress-Archive TokenPay\bin\Release\win-arm\ TokenPay\bin\Release\TokenPay_win-arm.zip
powershell Compress-Archive TokenPay\bin\Release\win-arm64\ TokenPay\bin\Release\TokenPay_win-arm64.zip
powershell Compress-Archive TokenPay\bin\Release\linux-x64\ TokenPay\bin\Release\TokenPay_linux-x64.zip
powershell Compress-Archive TokenPay\bin\Release\linux-arm\ TokenPay\bin\Release\TokenPay_linux-arm.zip
powershell Compress-Archive TokenPay\bin\Release\linux-arm64\ TokenPay\bin\Release\TokenPay_linux-arm64.zip
powershell Compress-Archive TokenPay\bin\Release\osx-x64\ TokenPay\bin\Release\TokenPay_osx-x64.zip
powershell Compress-Archive TokenPay\bin\Release\osx-arm64\ TokenPay\bin\Release\TokenPay_osx-arm64.zip
rmdir /s /q TokenPay\bin\Release\win-x64\
rmdir /s /q TokenPay\bin\Release\win-x86\
rmdir /s /q TokenPay\bin\Release\win-arm\
rmdir /s /q TokenPay\bin\Release\win-arm64\
rmdir /s /q TokenPay\bin\Release\linux-x64\
rmdir /s /q TokenPay\bin\Release\linux-arm\
rmdir /s /q TokenPay\bin\Release\linux-arm64\
rmdir /s /q TokenPay\bin\Release\osx-x64\
rmdir /s /q TokenPay\bin\Release\osx-arm64\
