---
alwaysApply: true
---

# ClickUp Desktop PowerTools – Development Rules

These rules define how this project must be structured and how changes are allowed to happen.  
They are authoritative. If something is not explicitly allowed here, it must not be done.

This file exists to prevent overengineering, architectural drift, and incorrect assumptions by humans or AI.

---

## Architecture Constraints

1. **One Core, many Tools**  
   The application has exactly one Core module and multiple independent Tools.

2. **Tools do not depend on each other**  
   A tool must never reference another tool’s code, types, or state.

3. **Core is platform-level, not feature-level**  
   Core contains ClickUp- and system-level infrastructure and shared state.  
   Feature-specific business logic belongs in Tools, not in Core.

4. **No dynamic plugin loading**  
   All tools are compiled into the application.  
   There is no runtime plugin system.

5. **No framework-style abstractions**  
   Do not introduce base classes, marker interfaces, or generic frameworks for “future extensibility”.  
   Prefer explicit, concrete code.

---

## Dependency Direction

- **Tools may depend on Core**
- **Core must not depend on Tools**
- **All UI binds only to ViewModels**
- Views must not reference Core components or Services directly

This applies to all UI layers, including core UI and tool UIs.

---

## Core Responsibilities (Strictly Limited)

The Core is the authoritative ClickUp and system integration layer.

Core may contain:
- ClickUp Desktop runtime communication (UI state, interaction, customization)
- ClickUp API access (single, shared service)
- Shared ClickUp-centric state and lifecycle management
- Application lifecycle management
- System integration (idle state, focus, hotkeys)
- Secure token storage

Core may additionally contain the following if required at platform level:
- Configuration persistence
- Logging
- System tray integration
- Autostart handling

Core must never contain:
- Tool-specific feature logic
- Tool-specific UI components
- Speculative abstractions

The number of tools using a Core capability is not a criterion.  
What matters is whether the capability represents platform-level ClickUp or system integration.

---

## Tool Model

A Tool is a self-contained feature module built on top of the Core.

### A Tool may:
- Consume ClickUp runtime or API state exposed by the Core
- Provide optional UI surfaces (overlay widgets, panels, windows)
- Run background logic
- Trigger notifications

### A Tool must:
- Own its own logic and state
- Be removable without breaking the application
- Not depend on other tools
- Not leak feature logic into Core

---

## UI Strategy

### Core UI (Mandatory)

- A dedicated PowerTools window used to configure and control the Core
- Manages runtime setup, tool activation, and global settings
- Lives in the system tray when not actively used

### Tool UIs (Optional)

- Overlays
- Widgets
- Panels
- Background-only tools with no UI

Tool UIs consume Core state and may be enabled or disabled independently.
Tool UIs must be limited to their specific feature scope and must not
duplicate Core UI responsibilities.

### UI Technology Constraint (Mandatory)

Primary control and configuration UI is implemented as a WebUI rendered
via WebView2.

System-bound UI surfaces such as overlays and widgets are implemented
natively using WPF where required.

The Core must not depend on any specific UI technology.


---

## ClickUp Integration Rules

- ClickUp Desktop runtime communication is a first-class integration
- Runtime integration is preferred for UI-related state and interaction
- ClickUp API access is complementary or used as a fallback where needed
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
   Code must be readable and obvious.

3. **Features drive architecture**  
   Architecture exists to support real features, not hypothetical ones.

4. **Platform logic belongs in Core, feature logic does not**

5. **Reuse existing infrastructure first**  
   If infrastructure already exists in the codebase, it must be reused.  
   Introducing parallel solutions for the same concern is not allowed.

---

## Change Discipline (Critical)

- Do not refactor existing code unless explicitly instructed
- Do not rename files, folders, or classes “for clarity”
- Do not move code between Core and Tools without explicit direction
- Do not introduce new abstractions to “clean things up”
- Do not change behavior unless explicitly requested

If something looks imperfect but works and respects the rules, leave it alone.

---

## Anti-Patterns (Hard No)

Do NOT build:
- Plugin frameworks
- Dynamic plugin loading
- Abstraction layers for hypothetical future tools
- Event buses or mediator patterns
- OAuth flows
- Enterprise-style configuration systems

Do NOT put in Core:
- Feature-specific business logic
- Tool-specific UI components
- Speculative abstractions

---

## Project Boundaries

This project is:
- A ClickUp-centric desktop companion
- Modular and intentionally simple
- Power-user focused and experimental
- Optimized for speed, visibility, and low friction

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
