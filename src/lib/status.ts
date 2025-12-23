// Badge computation utilities
// Returns objects with { text, variant, title } for consistent badge rendering

import type { AppState } from './state';

export type BadgeVariant = 'valid' | 'invalid' | 'untested' | 'none' | 'info';

export interface BadgeInfo {
	text: string;
	variant: BadgeVariant;
	title: string;
}

export interface StatusInfo {
	text: string;
	variant: BadgeVariant;
}

export function getClickUpBadge(state: AppState): BadgeInfo {
	if (state.clickUpDesktopStatus === 'Running') {
		if (state.clickUpDebugPortAvailable === true) {
			return {
				text: 'Running (Debug Ready)',
				variant: 'valid',
				title: 'ClickUp Desktop is running and debug port is available'
			};
		}
		if (state.clickUpDebugPortAvailable === false) {
			return {
				text: 'Running (No Debug)',
				variant: 'untested',
				title: 'ClickUp Desktop is running but debug port is not available'
			};
		}
		return {
			text: 'Running (Unknown)',
			variant: 'untested',
			title: 'ClickUp Desktop is running, debug status not checked'
		};
	}
	return {
		text: 'Not Running',
		variant: 'none',
		title: 'ClickUp Desktop is not running'
	};
}

export function getApiBadge(state: AppState): BadgeInfo {
	if (!state.hasApiToken) {
		return {
			text: 'API: Not Set',
			variant: 'none',
			title: 'No ClickUp API token configured'
		};
	}
	if (state.tokenValid === true) {
		return {
			text: 'API: Valid',
			variant: 'valid',
			title: 'ClickUp API token is valid'
		};
	}
	if (state.tokenValid === false) {
		return {
			text: 'API: Invalid',
			variant: 'invalid',
			title: 'ClickUp API token is invalid'
		};
	}
	return {
		text: 'API: Untested',
		variant: 'untested',
		title: 'ClickUp API token is configured but not tested'
	};
}

export function getUptimeBadge(state: AppState): BadgeInfo {
	return {
		text: `Uptime: ${state.uptime || '0m'}`,
		variant: 'info',
		title: `Application uptime: ${state.uptime || '0m'}`
	};
}

export function getClickUpStatus(state: AppState): StatusInfo {
	if (state.clickUpDesktopStatus === 'Running') {
		if (state.clickUpDebugPortAvailable === true) {
			return {
				text: 'Running (Debug Ready)',
				variant: 'valid'
			};
		}
		if (state.clickUpDebugPortAvailable === false) {
			return {
				text: 'Running (No Debug)',
				variant: 'untested'
			};
		}
		return {
			text: 'Running (Unknown)',
			variant: 'untested'
		};
	}
	return {
		text: 'Not Running',
		variant: 'none'
	};
}

export function getTokenStatus(state: AppState): StatusInfo {
	if (!state.hasApiToken) {
		return {
			text: 'Not configured',
			variant: 'none'
		};
	}
	if (state.tokenValid === null) {
		return {
			text: 'Configured (untested)',
			variant: 'untested'
		};
	}
	if (state.tokenValid === true) {
		return {
			text: 'Valid',
			variant: 'valid'
		};
	}
	return {
		text: 'Invalid',
		variant: 'invalid'
	};
}

