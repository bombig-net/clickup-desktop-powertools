# ClickUp Desktop PowerTools

An unofficial, experimental Windows desktop application for internal ClickUp power-user utilities.  
Built for daily use at bombig.net.

---

## Purpose

ClickUp Desktop PowerTools is a lightweight WPF (.NET 8) desktop companion for ClickUp.

It exists to experiment with fast, low-friction desktop utilities that complement ClickUp without replacing it.

This project is **not user-facing**, **not polished**, and **not stable**.  
It is a working codebase for building and validating ideas.

---

## Concept

The application provides:

- A small, always-on-top overlay window positioned above the Windows taskbar
- A simple platform that hosts independent tools
- Shared infrastructure for ClickUp API access and system integration

The application itself is just the platform.  
All real functionality lives in tools.

---

## Core Capabilities

The core platform is responsible for:

- Creating and positioning the overlay window
- Hosting compact UI blocks provided by tools
- Providing shared access to the ClickUp API
- Securely storing a ClickUp API token via Windows Credential Manager
- Enforcing a strict, rule-driven architecture

The core does **not** contain feature logic.

---

## Tools

### Time Tracking

A tool focused on working with the currently active ClickUp time entry.

Current focus:
- Reading the active time entry
- Displaying task name and elapsed time
- Stopping a running time entry

This tool exists primarily to validate the overlay concept and overall architecture.

---

## Scope and Status

This repository is in an early, exploratory phase.

- APIs may change
- Features may break or disappear
- Documentation favors intent over completeness

Architecture rules are authoritative.  
Documentation exists to orient contributors, not users.

---

## Requirements

- Windows 10 or 11
- .NET 8 SDK
- A personal ClickUp API token

---

## Build and Run

From the repository root:

```bash
dotnet build
dotnet run
```

## Notes

This project is intentionally architecture-driven.

Before making changes, read the authoritative development rules:

`.cursor/rules/clickup-desktop-powertools/RULE.md`

Those rules define what is allowed, what is forbidden, and how this codebase is expected to evolve.
