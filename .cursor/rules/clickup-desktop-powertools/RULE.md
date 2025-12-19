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

3. **Core contains only shared infrastructure**  
   Core provides infrastructure that is needed by multiple tools.  
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
- **UI binds only to ViewModels**
- Views must not reference Services or Core components directly.

---

## Core Responsibilities (Strictly Limited)

Core may contain infrastructure that is demonstrably shared by multiple tools, such as:

- Application lifecycle management
- Overlay window creation and positioning
- Hosting and layout of tool UI blocks
- ClickUp API access (single, shared service)
- Secure token storage

Core may only contain the following **if multiple tools require them**:
- Configuration persistence
- Logging
- System tray integration
- Autostart handling

Core must never contain feature-specific business logic.

If logic is only used by one tool, it does not belong in Core.

---

## Tool Model

A Tool is a self-contained feature module.

### A Tool may:
- Contribute a UI block to the overlay
- Run background logic
- Trigger notifications
- Use Core infrastructure services

### A Tool must:
- Own its own logic and state
- Be removable without breaking the application
- Not depend on other tools
- Not leak feature logic into Core

---

## UI Strategy

- **Primary surface**:  
  A borderless, always-on-top overlay window positioned above the Windows taskbar.  
  It displays compact UI blocks provided by tools.

- **Secondary surfaces** (optional and feature-driven):  
  System tray icon, settings window, notifications, background-only tools.

Not every feature must render UI in the overlay.

---

## ClickUp Integration Rules

- Exactly one ClickUp API service in Core
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

4. **If fewer than two tools need it, it does not belong in Core**

---

## Change Discipline (Critical)

- Do not refactor existing code unless explicitly instructed.
- Do not rename files, folders, or classes “for clarity”.
- Do not move code between Core and Tools without explicit direction.
- Do not introduce new abstractions to “clean things up”.
- Do not change behavior unless explicitly requested.

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
- Code used by only one tool
- Tool-specific UI components
- Speculative abstractions

---

## Project Boundaries

This project is:
- A collection of desktop power tools for ClickUp
- Modular and intentionally simple
- Internal and open-source friendly
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
