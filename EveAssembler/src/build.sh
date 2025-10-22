#!/bin/bash
dotnet run -c Release --project ../EveAssembler.csproj -- --sourceFile memtest.S --destFile build/memtest.o
