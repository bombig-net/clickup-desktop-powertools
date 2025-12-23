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

This applies to all UI layers, including Core UI and Tool UIs.

---

## Core Responsibilities (Strictly Limited)

The Core is the authoritative ClickUp and system integration layer.

Core may contain:
- ClickUp Desktop runtime communication
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
- Tool-specific business rules
- Tool-specific UI components
- Interpretation of tool configuration
- Speculative abstractions

**Important clarification:**  
Core may store, render, and pass through tool configuration,  
but must not interpret, validate, or act on tool-specific configuration semantics.

---

## Tool Model

A Tool is a self-contained feature module built on top of the Core.

### A Tool may:
- Consume ClickUp runtime or API state exposed by the Core
- Receive its configuration data from Core
- Provide optional UI surfaces (overlay widgets, panels, windows)
- Run background logic
- Trigger notifications

### A Tool must:
- Own its own logic and state
- Own the meaning and interpretation of its configuration
- Be removable without breaking the application
- Not depend on other tools
- Not leak feature logic into Core

---

## UI Strategy

### Core UI (Mandatory)

- A dedicated PowerTools control window
- Used to configure and control the Core and manage tools
- Hosts tool activation and tool configuration surfaces
- Lives in the system tray when not actively used

The Core UI may:
- Enable or disable tools
- Render tool configuration controls
- Persist tool configuration
- Pass configuration data to tools

The Core UI must not:
- Implement tool behavior
- Contain tool-specific business logic
- Make assumptions about what tool configuration means

### Tool UIs (Optional)

Tools may provide their own UI surfaces, such as:
- Overlays
- Widgets
- Panels
- Background-only tools with no UI

Tool UIs are optional and feature-driven.  
A tool is not required to have its own UI.

---

## UI Technology Constraint (Mandatory)

- The primary Core control and configuration UI is implemented as a **Web UI rendered via WebView2**
- Native UI (WPF) is used only where tight system integration is required (e.g. overlays)
- Core logic must remain UI-technology-agnostic

---

## WebUI Structure & Styling Rules (Mandatory)

The Web UI is part of Core UI and must remain predictable, readable, and tool-agnostic.

### JavaScript (WebUI)

- No inline styles may be set via JavaScript.
- JavaScript may only manipulate:
  - DOM structure
  - Text content
  - Data attributes
  - CSS class lists
- UI state must be expressed via CSS classes, not style mutations.
- Large functions must be split by responsibility (rendering, state sync, event wiring).

### CSS Strategy

- CSS is split by responsibility:
  - `base.css` – resets, typography, globals
  - `utilities.css` – layout and state utilities only
  - `components.css` – reusable UI components
  - `tools.css` – tool-specific styling
- Utility classes must be generic, composable, and stateless.
- Component styles must not encode application logic.
- Tool styles must be scoped to tool containers.

### Class Naming Discipline

- Utility classes must use a `u-` prefix (e.g. `u-flex`, `u-gap-sm`).
- State classes must use an `is-` or `has-` prefix (e.g. `is-disabled`, `has-error`).
- Component classes must not be reused as utilities.

### Prohibited

- No CSS frameworks that require a build step.
- No runtime-generated CSS rules.
- No style mutations via JavaScript.
- No global CSS overrides for tool-specific behavior.

---

## ClickUp Integration Rules

- ClickUp Desktop runtime communication is first-class
- Runtime integration is preferred for UI-related state and interaction
- ClickUp API access is complementary or used as fallback
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
- Tool-specific behavior
- Tool-specific interpretation of configuration
- Speculative abstractions

---

## Project Boundaries

This project is:
- A ClickUp-centric desktop companion
- Modular and intentionally simple
- Power-user focused and experimental

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
