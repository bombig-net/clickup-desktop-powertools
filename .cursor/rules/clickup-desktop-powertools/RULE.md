---
alwaysApply: true
---

# ClickUp Desktop PowerTools – Development Rules

These rules define how this project must be structured and how changes are allowed to happen.  
They are authoritative. If something is not explicitly allowed here, it must not be done.

This file exists to prevent architectural drift, feature leakage into Core, and incorrect assumptions by humans or AI.

---

## Architecture Constraints

1. **One Core, many Tools**  
   The application has exactly one Core module and multiple independent Tools.

2. **Tools do not depend on each other**  
   A Tool must never reference another Tool’s code, types, state, or UI.

3. **Core is platform-level, not feature-level**  
   Core provides raw ClickUp and system integration capabilities.  
   Core exposes what is possible, not what a feature intends to do.

4. **No dynamic plugin loading**  
   All Tools are compiled into the application.  
   There is no runtime plugin system.

5. **No framework-style abstractions**  
   Do not introduce base classes, marker interfaces, generic frameworks, or extensibility layers for hypothetical future use.  
   Prefer explicit, concrete implementations.

---

## Dependency Direction

- Tools may depend on Core
- Core must not depend on Tools
- All UI binds only to ViewModels
- Views must not reference Core components or Services directly

This applies to all UI layers, including Core UI and Tool UIs.

---

## Core Responsibilities (Strictly Limited)

The Core is the authoritative ClickUp and system integration layer.

Core may contain:
- ClickUp Desktop runtime communication
- ClickUp API access (single shared service)
- Shared ClickUp-centric state and lifecycle management
- Application lifecycle management
- System integration (tray, focus, idle state, hotkeys)
- Secure token storage

Core may additionally contain platform-level infrastructure if required:
- Configuration persistence
- Logging
- Autostart handling

Core must never contain:
- Feature intent or workflows
- Tool-specific business logic
- Tool-specific UI components
- Feature configuration logic
- Speculative abstractions

Whether one or many Tools use a capability is irrelevant.  
What matters is whether the capability represents **platform-level integration**.

---

## Tool Model

A Tool is a self-contained feature module built on top of the Core.

### A Tool may:
- Consume state and capabilities exposed by Core
- Provide its own UI surfaces
- Run background logic
- Trigger notifications

### A Tool must:
- Own its own logic and state
- Be removable without breaking the application
- Not depend on other Tools
- Not leak feature logic into Core

---

## UI Strategy

### Core UI (Mandatory)

- A dedicated PowerTools control window
- Used to configure and control Core behavior
- Manages runtime setup, global settings, and Tool activation
- Lives in the system tray when not in active use

The Core UI exposes **platform-level state only**.

It must not contain:
- Tool-specific configuration
- Feature behavior controls
- Detailed feature UIs

### Tool UIs (Optional)

Tools may provide their own UI surfaces, such as:
- Overlays
- Widgets
- Panels
- Background-only tools with no UI

Tool UIs must be strictly limited to their feature scope.

---

## UI Technology Constraint (Hard Rule)

The primary Core control window is implemented as a **WebUI**.

- Rendering is currently done via WebView2
- This is an implementation detail of the UI layer
- Core must not depend on WebView2 APIs or Web concepts

System-bound UI surfaces such as overlays or widgets may be implemented natively using WPF where required.

Core must remain UI-technology agnostic.

---

## ClickUp Integration Rules

- ClickUp Desktop runtime integration is first-class
- Runtime integration is preferred for UI-related state and interaction
- ClickUp API access is complementary or a fallback
- Exactly one ClickUp API service exists in Core
- Raw HTTP access only
- No feature-specific methods in the API layer
- No business interpretation inside the API service
- Personal ClickUp API tokens only
- Tokens stored via Windows Credential Manager
- OAuth is explicitly out of scope

---

## Development Principles

1. **Structure over abstraction**  
   Clear folders and explicit wiring beat clever patterns.

2. **Explicit over clever**  
   Code must be obvious and readable.

3. **Features drive architecture**  
   Architecture exists to support real features, not hypothetical ones.

4. **Platform logic belongs in Core, feature logic does not**

5. **Reuse existing infrastructure first**  
   If infrastructure already exists, it must be reused.  
   Parallel solutions for the same concern are not allowed.

---

## Change Discipline (Critical)

- Do not refactor existing code unless explicitly instructed
- Do not rename files, folders, or classes “for clarity”
- Do not move code between Core and Tools without explicit direction
- Do not introduce new abstractions to “clean things up”
- Do not change behavior unless explicitly requested

If something looks imperfect but respects the rules, leave it alone.

---

## Anti-Patterns (Hard No)

Do NOT build:
- Plugin frameworks
- Dynamic plugin loading
- Abstraction layers for future tools
- Event buses or mediator patterns
- OAuth flows
- Enterprise-style configuration systems

Do NOT put in Core:
- Feature-specific business logic
- Tool-specific UI components
- Feature configuration logic
- Speculative abstractions

---

## Project Boundaries

This project is:
- A ClickUp-centric desktop companion
- Modular and intentionally simple
- Power-user focused and experimental
- Optimized for speed and low friction

This project is NOT:
- A real Windows taskbar extension
- A shell or Explorer integration
- An official ClickUp client
- A plugin framework
- A commercial product

---

## Source of Truth

This file is the source of truth for architecture and development behavior.

If an implementation decision is not supported by this document, it must not be made.
