@echo off
cls

dotnet ".\Src\fake\fake.dll" run ".\Src\build.fsx" %*
