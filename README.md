# ClickUp Desktop PowerTools

ClickUp Desktop PowerTools is a Windows companion for the ClickUp Desktop app.

It provides a small platform that connects ClickUp Desktop with the Windows system and runs focused tools on top of that connection.

PowerTools is experimental and intended for power users.

> ⚠️ **Experimental**
>  
> PowerTools is under active development.  
> Features, behavior, and configuration may change.

---

## What it is

PowerTools runs alongside the ClickUp Desktop app and operates at the system level.

This allows it to combine:
- live state from the running ClickUp Desktop app
- information from the ClickUp API where useful
- signals and capabilities from the Windows system

The goal is to support small, practical utilities that are useful during daily work
but are outside the scope of ClickUp itself.

This project exists because these are tools we need ourselves in day-to-day work.

---

## Included tools

**Time Tracking**
- Shows the currently tracked task and elapsed time
- Provides a small on-screen overlay for quick visibility

**Custom CSS and JavaScript**
- Apply custom styling to the ClickUp Desktop app
- Run small scripts to simplify or adjust the interface

More tools may be added over time.

---

## What it is not

PowerTools does not replace ClickUp.  
It does not modify ClickUp servers or accounts.  
It is not an official ClickUp product.

---

## Development

### Prerequisites

- .NET 8.0 SDK
- Node.js LTS
- npm

### Building

1. **Install frontend dependencies:**
   ```bash
   npm install
   ```

2. **Build the frontend:**
   ```bash
   npm run build
   ```
   This builds the Svelte + Tailwind UI and outputs to `wwwroot/`.

3. **Build the .NET application:**
   ```bash
   dotnet build
   ```

### Project Structure

- `src/` - Source files for the Core Control Window UI (Svelte + Tailwind)
- `wwwroot/` - **Build output only** - Generated files from frontend build. Do not edit files here directly.
- `Core/` - Core platform functionality
- `Tools/` - Individual tool implementations
- `UI/` - WPF UI components

### Frontend Development

The Core Control Window UI is built with:
- **Svelte** for reactive UI components
- **Tailwind CSS** for utility-first styling
- **Vite** as the build tool

Source files are in `src/`. After running `npm run build`, the built files are output to `wwwroot/`, which the .NET application serves via WebView2.

**Important:** `wwwroot/` is build output only. All source files are in `src/`. The build process will replace files in `wwwroot/` when you run `npm run build`.

---

## Origin

PowerTools is developed and used internally at **bombig.net**  
https://bombig.net