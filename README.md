# ClickUp Desktop PowerTools

ClickUp Desktop PowerTools is a native Windows companion for the ClickUp Desktop app.

It provides a central core that connects the running ClickUp Desktop app with the Windows system environment, and a growing set of focused tools built on top of that core.

PowerTools is experimental and intended for power users. Behavior and APIs may change.

---

## The Core

The PowerTools Core is responsible for all ClickUp-related communication and state.

It integrates with:
- the ClickUp Desktop runtime (for live UI state, customization, and interaction)
- the ClickUp API (as a complementary data source where needed)
- the Windows system (idle state, focus, hotkeys, lifecycle)

The Core runs independently of any specific tool UI and exposes a shared ClickUp-centric state.

A dedicated PowerTools window acts as the control center for the Core.  
From there, users configure runtime setup, manage tools, and adjust settings.  
When not in use, this window lives in the system tray.

---

## Tools

Tools are small, focused features built on top of the Core.

Each tool uses the Core’s ClickUp and system integration, but owns its own logic and UI.

### Included tools

**Time Tracking**
- Shows the currently tracked task and elapsed time
- Provides a small desktop overlay for quick visibility
- Enables safe interaction with the running ClickUp timer

**Custom CSS and JavaScript**
- Apply custom CSS to the ClickUp Desktop UI
- Run small scripts to adjust or simplify the interface
- Managed centrally via the PowerTools window

### Potential future tools

- Custom hotkeys for ClickUp actions
- System-triggered behavior (for example based on idle or focus state)
- Additional desktop widgets and panels

---

## Philosophy

PowerTools does not replace ClickUp.  
It augments the ClickUp Desktop experience by reducing friction, surfacing relevant state, and connecting ClickUp more deeply with the Windows desktop.
