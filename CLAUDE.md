# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Status

As of 2026-09-04 this directory is empty: no source, no README, no git repository, no build
files. The stack and the scope of the project have not been chosen yet. The sections below that
depend on a stack are placeholders. Fill them in as soon as the first code lands, in the same
session that lands it, not afterwards.

## Standing rules for this project

- All deliverables are written in English: code, comments, UI strings, docs, commit messages.
  French is only for talking with the owner.
- Everything lives on D:. Never install programs, models, caches or data on C:.
- Do not write source files through PowerShell `Get-Content`/`Set-Content`: it re-encodes
  accented characters and adds a BOM without any compiler noticing. Use the Read/Edit/Write tools
  or Git Bash heredocs.
- Anything finished gets committed and pushed right away on `main`, without waiting to be asked.
  There is no repository yet; creating it is the first thing to do once the stack is picked.
- Findings, gotchas and decisions go into this file during the work, not at the end.

## Toolchains verified on this machine (2026-09-04)

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 10.0.400 | SDK at `D:\Program Files\dotnet\sdk`; bare `dotnet` resolves it correctly |
| Node | 24.17.0 | pnpm 11.24.0 shims in `DevTools\bin` |
| Python | 3.13.15 | |

No C++ compiler is installed (Build Tools 2022 without MSVC or Windows SDK).

## Build, run, test

Not defined yet. Once the stack exists, record here: the one command to build, the one to run,
the one to run the full test suite, and how to run a single test.

## Architecture

Not defined yet. Once there is more than one module, describe here what a reader would need to
open several files to understand: the simulation loop, how the environment and the vacuum agent
are separated, where rendering sits relative to the simulation, and any data formats.
