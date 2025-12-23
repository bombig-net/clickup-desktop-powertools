import { writable, type Writable } from 'svelte/store';

export interface Tool {
	id: string;
	name: string;
	description: string;
	enabled: boolean;
}

export interface AppState {
	version: string;
	dotNetVersion: string;
	webView2Version: string;
	logFilePath: string;
	uptime: string;
	clickUpDesktopStatus: string;
	clickUpDebugPortAvailable: boolean | null;
	hasApiToken: boolean;
	tokenValid: boolean | null;
	clickUpInstallPath: string | null;
	clickUpInstallPathOverride: string | null;
	debugPort: number;
	restartIfRunning: boolean;
	autostartEnabled: boolean;
	tools: Tool[];
}

// Initial state matching the structure from app.js
const initialState: AppState = {
	version: '',
	dotNetVersion: '',
	webView2Version: '',
	logFilePath: '',
	uptime: '',
	clickUpDesktopStatus: '',
	clickUpDebugPortAvailable: null,
	hasApiToken: false,
	tokenValid: null,
	clickUpInstallPath: null,
	clickUpInstallPathOverride: null,
	debugPort: 9222,
	restartIfRunning: false,
	autostartEnabled: false,
	tools: []
};

// Create writable store
export const appState: Writable<AppState> = writable(initialState);

// Helper to update state
export function updateState(newState: Partial<AppState> | Record<string, unknown>): void {
	appState.set(newState as AppState);
}

