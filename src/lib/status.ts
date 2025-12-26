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
				text: 'Running (Connected)',
				variant: 'valid',
				title: 'ClickUp Desktop is running and PowerTools connection is available'
			};
		}
		if (state.clickUpDebugPortAvailable === false) {
			return {
				text: 'Running (Not Connected)',
				variant: 'untested',
				title: 'ClickUp Desktop is running but PowerTools connection is not available'
			};
		}
		return {
			text: 'Running (Unknown)',
			variant: 'untested',
			title: 'ClickUp Desktop is running, connection status not checked'
		};
	}
	return {
		text: 'Not Running',
		variant: 'none',
		title: 'ClickUp Desktop is not running'
	};
}

export function getClickUpStatus(state: AppState): StatusInfo {
	if (state.clickUpDesktopStatus === 'Running') {
		if (state.clickUpDebugPortAvailable === true) {
			return {
				text: 'Running (Connected)',
				variant: 'valid'
			};
		}
		if (state.clickUpDebugPortAvailable === false) {
			return {
				text: 'Running (Not Connected)',
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
