---
title: "Fix dotnet test on .NET 10 SDK (Microsoft.Testing.Platform VSTest error)"
kind: problem
status: pending
created: 2026-11-02
---

AGENTS.md test command `dotnet test src/BluOsNadRemote.Blu4Net.Tests` fails on SDK 10.0.402: 'Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later' (microsoft.testing.platform.msbuild 2.3.3); /p:TestingPlatformDotnetTestSupport=true did not help. Workaround: run the built test dll directly (dotnet BluOsNadRemote.Blu4Net.Tests.dll -> 81/81 pass). Make dotnet test work again (newer xunit.v3 / MTP or correct opt-in) and update AGENTS.md if needed. Where: src/BluOsNadRemote.Blu4Net.Tests/BluOsNadRemote.Blu4Net.Tests.csproj
