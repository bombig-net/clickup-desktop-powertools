# ClickUp Desktop PowerTools

## Purpose
ClickUp Desktop PowerTools is a Windows desktop application that provides power-user utilities for ClickUp.

It augments ClickUp regardless of whether ClickUp is used in a browser or elsewhere. The application runs locally, stays visible, and reduces context switching during daily work.

This is not a commercial product. It is an internal-first, open-source toolset built for real daily usage.

## Core Idea
- One Windows application
- One persistent, always-visible overlay near the taskbar
- Multiple independent power tools sharing a small, stable core
- Features can exist in the overlay, the background, or as notifications

The overlay is the primary UI anchor. Not every feature must render UI in the overlay.

## What This Is
- A collection of desktop power tools for ClickUp
- Modular and easy to extend
- Focused on speed, visibility, and low friction

## What This Is Not
- Not a real Windows taskbar extension
- Not an Explorer hack
- Not a plugin framework
- Not an official ClickUp client

## UI Strategy

### Primary Surface
- Borderless, always-on-top window
- Positioned directly above the Windows taskbar
- Visually aligned with taskbar size and color
- Displays compact UI blocks provided by tools

### Secondary Surfaces
- System tray icon
- Settings window
- Notifications
- Background-only services

The overlay defines the identity of the application. Other surfaces support it.

## Architecture Rules
1. One Core, many tools
2. Tools do not depend on each other
3. Core contains only shared infrastructure
4. No dynamic plugin loading
5. No framework-style abstractions

## Project Structure

- **/src**
  - **/Core**
    - AppStartup
    - OverlayHost
    - WindowPositioning
    - ClickUpApi
    - TokenStorage
    - Configuration

  - **/Tools**
    - **/TimeTracking**
      - TimeTrackingView
      - TimeTrackingViewModel
      - TimeTrackingService

  - **/UI**
    - **/Overlay**
    - **/Settings**

  - **/Infrastructure**
    - Logging
    - Tray
    - Autostart



### Dependency Direction
- Tools depend on Core
- Core does not depend on tools
- UI binds only to ViewModels

## Core Responsibilities
The Core is responsible for:
- Application lifecycle
- Overlay window creation and positioning
- Hosting and layout of tool UI blocks
- ClickUp API access
- Secure token storage
- Configuration persistence
- Logging
- System tray integration

The Core does not contain feature-specific business logic.

## Tool Model
A tool is a self-contained feature.

A tool may:
- Contribute a UI block to the overlay
- Run background logic
- Trigger notifications
- Use ClickUp API access provided by the Core

A tool must:
- Own its logic and state
- Be removable without breaking the application

## ClickUp Integration
- Single ClickUp API service in the Core
- Raw API access only
- Personal ClickUp API tokens
- Tokens stored using Windows-native secure storage
- OAuth is out of scope for the initial version

## First Tool: Time Tracking
The first implemented tool provides:
- Display of the currently tracked ClickUp task
- Live elapsed time
- Basic start and stop controls

This tool validates:
- Overlay layout
- Tool registration
- ClickUp API integration
- End-to-end architecture

Perfect synchronization and rare edge cases are intentionally out of scope.

## Future Tools (Out of Initial Scope)
- Quick task switching
- Task search
- Clipboard helpers for ClickUp links
- Timer reminders
- Lightweight notifications

All future tools must fit into the existing structure without requiring Core changes.

## Initial Development Scope
The first milestone delivers:
- New repository
- Clean, documented structure
- Overlay visible above the taskbar
- One working tool
- Readable and maintainable code

Explicitly excluded:
- Auto-update system
- Perfect auto-hide handling
- Full multi-monitor edge cases
- Dynamic plugin loading

## Development Principles
- Structure over abstraction
- Explicit code over clever code
- Features drive architecture
- If fewer than two tools need it, it does not belong in Core
