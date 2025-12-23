---
alwaysApply: true
---

# ClickUp Desktop PowerTools – Development Rules

This document defines the authoritative architectural and development rules for
ClickUp Desktop PowerTools.

If a decision or implementation is not supported by this document, it must not be made.

The rules are written to prevent architectural drift, accidental coupling,
and ambiguous decisions under AI-assisted development.

---

## 1. Global Architectural Invariants (Apply Everywhere)

These rules apply to the entire project, without exception.

### 1.1 Core and Tools

1. **Exactly one Core, many Tools**  
   The application has exactly one Core module and multiple independent Tools.

2. **Tools do not depend on each other**  
   A Tool must never reference another Tool’s code, types, or state.

3. **Core is platform-level, not feature-level**  
   Core contains system integration, ClickUp integration, lifecycle management,
   and shared infrastructure only.  
   Feature-specific logic belongs in Tools.

4. **No dynamic plugin loading**  
   All Tools are compiled into the application.  
   There is no runtime plugin system.

5. **No speculative frameworks or abstractions**  
   Do not introduce generic frameworks, base classes, or extensibility layers
   for hypothetical future use.  
   Prefer explicit, concrete code.

---

### 1.2 Dependency Direction

- Tools may depend on Core.
- Core must not depend on Tools.
- UI layers must not reference Core services directly.
- UI layers bind only to exposed state or message contracts.

---

## 2. Core Responsibilities (Strict)

The Core is the authoritative platform layer.

### 2.1 Core may contain

- ClickUp Desktop runtime communication
- ClickUp API access (exactly one service, raw HTTP)
- Shared ClickUp-centric state and lifecycle management
- Application lifecycle management
- System integration (idle state, focus, hotkeys)
- Secure token storage
- Configuration persistence
- Logging
- System tray integration
- Autostart handling

### 2.2 Core must never contain

- Tool-specific feature logic
- Tool-specific business rules
- Tool-specific UI components
- Interpretation or validation of tool configuration
- Speculative abstractions

Core may store and pass through tool configuration data,
but must not interpret its semantics.

---

## 3. Tool Model

A Tool is a self-contained feature module built on top of the Core.

### 3.1 A Tool may

- Consume runtime or API state exposed by Core
- Receive configuration data from Core
- Provide optional UI surfaces (WPF, WebView2, overlays, widgets)
- Run background logic
- Trigger notifications

### 3.2 A Tool must

- Own its own logic and state
- Own the meaning and interpretation of its configuration
- Be removable without breaking the application
- Not depend on other Tools
- Not leak feature logic into Core

Tools are free in their internal UI and implementation choices,
as long as global invariants are respected.

---

## 4. UI Surfaces Overview

PowerTools may host multiple UI surfaces:

- Core Control Window
- Tool-specific windows or overlays
- Background-only tools with no UI

Each UI surface must respect Core and Tool boundaries,
but UI technology choices may differ per surface.

---

## 5. Core Control Window (Mandatory UI Surface)

This section applies **only** to the Core control and configuration window
opened from the system tray.

### 5.1 Technology Commitment

The Core Control Window is implemented as:

- WebView2
- Svelte for UI rendering
- Utility-first CSS using Tailwind
- A build step is mandatory for this UI surface

This commitment applies only to the Core Control Window.
It does not apply to Tool UIs.

---

### 5.2 Rendering and State Model

- The UI is declarative and reactive.
- UI updates are driven by state changes.
- Manual DOM manipulation is not allowed.
- UI components must not call Core services directly.
- Communication with Core happens only through a defined message or state bridge.

---

### 5.3 Styling Rules

- Utility-first CSS is the default.
- Styling is static and determined at build time.
- Runtime-generated CSS rules are not allowed.
- JavaScript must not set inline styles.
- Component-scoped styles are allowed where supported.
- Global CSS must be minimal and intentional.

---

## 6. Tool UI Surfaces (Optional)

Tool UIs may use any of the following, depending on need:

- WPF
- WebView2
- Minimal or static HTML
- No UI at all

Tool UI choices are not standardized,
as long as they do not violate global architectural invariants
or Core dependency rules.

---

## 7. ClickUp Integration Rules

- ClickUp Desktop runtime integration is first-class.
- Runtime integration is preferred for UI-related state and interaction.
- ClickUp API access is complementary or fallback.
- Exactly one ClickUp API service exists in Core.
- Raw HTTP access only.
- No feature-specific methods in the API layer.
- No business interpretation inside the API service.
- Personal ClickUp API tokens only.
- Tokens are stored via Windows Credential Manager.
- OAuth is explicitly out of scope.

---

## 8. Development Discipline

- Do not refactor code unless explicitly instructed.
- Do not rename files, folders, or classes “for clarity”.
- Do not move code between Core and Tools without explicit direction.
- Do not introduce abstractions to “clean things up”.
- Do not change behavior unless explicitly requested.

If something works and respects these rules, it must be left alone.

---

## 9. Anti-Patterns (Hard No)

Do not build:

- Plugin frameworks
- Dynamic plugin loading
- Event buses or mediator patterns
- Abstraction layers for hypothetical future tools
- OAuth flows
- Enterprise-style configuration systems

Do not put in Core:

- Feature-specific business logic
- Tool-specific behavior
- Tool-specific interpretation of configuration
- Speculative abstractions

---

## 10. Project Boundaries

This project is:

- A ClickUp-centric desktop companion
- Modular and intentionally simple
- Power-user focused
- Experimental and internally driven

This project is not:

- A Windows shell or taskbar extension
- An official ClickUp client
- A plugin framework
- A commercial product

---

## Source of Truth

This document is the single source of truth for architecture and development behavior.

If an implementation decision is not supported by this document,
it must not be made.
