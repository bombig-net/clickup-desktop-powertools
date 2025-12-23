// Core <-> WebUI Message Bridge

type MessageHandler = (payload: unknown) => void;
type Message = { type: string; payload: unknown };

interface WebView2Window extends Window {
	chrome?: {
		webview?: {
			postMessage: (message: unknown) => void;
			addEventListener: (event: 'message', handler: (event: MessageEvent) => void) => void;
		};
	};
	PowerTools?: {
		sendMessage: typeof sendMessage;
		onMessage: typeof onMessage;
		getState: () => Record<string, unknown>;
	};
}

const handlers: Record<string, MessageHandler[]> = {};
let isInitialized = false;

// Send a message to Core
export function sendMessage(type: string, payload?: unknown): void {
	const win = window as WebView2Window;
	if (win.chrome?.webview) {
		const message: Message = { type, payload: payload || {} };
		win.chrome.webview.postMessage(message);
	} else {
		console.warn('WebView2 bridge not available');
	}
}

// Register a handler for a message type
export function onMessage(type: string, handler: MessageHandler): void {
	if (!handlers[type]) {
		handlers[type] = [];
	}
	handlers[type].push(handler);
}

// Unregister a handler for a message type
export function offMessage(type: string, handler: MessageHandler): void {
	if (!handlers[type]) {
		return;
	}
	
	const index = handlers[type].indexOf(handler);
	if (index > -1) {
		handlers[type].splice(index, 1);
	}
	
	// Clean up empty handler arrays
	if (handlers[type].length === 0) {
		delete handlers[type];
	}
}

// Handle incoming messages from Core
function handleMessage(event: MessageEvent): void {
	try {
		const message = (typeof event.data === 'string' ? JSON.parse(event.data) : event.data) as Message;
		const type = message.type;
		const payload = message.payload;

		if (handlers[type]) {
			handlers[type].forEach(handler => {
				try {
					handler(payload);
				} catch (err) {
					console.error('Handler error for', type, err);
				}
			});
		}
	} catch (err) {
		console.error('Failed to handle message:', err);
	}
}

// Initialize the bridge
export function init(): void {
	// Prevent duplicate initialization
	if (isInitialized) {
		// Already initialized, just request state
		sendMessage('get-state');
		return;
	}
	
	const win = window as WebView2Window;
	if (win.chrome?.webview) {
		win.chrome.webview.addEventListener('message', handleMessage);
		isInitialized = true;
	}

	// Request initial state
	sendMessage('get-state');
}

// Expose for debugging
if (typeof window !== 'undefined') {
	const win = window as WebView2Window;
	win.PowerTools = {
		sendMessage,
		onMessage,
		getState: () => ({}) // State is now in Svelte store
	};
}

