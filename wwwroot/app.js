// Core <-> WebUI Message Bridge
(function() {
    'use strict';

    // Message handlers registry
    const handlers = {};

    // Current state
    let state = {
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

    // Register a handler for a message type
    function onMessage(type, handler) {
        if (!handlers[type]) {
            handlers[type] = [];
        }
        handlers[type].push(handler);
    }

    // Send a message to Core
    function sendMessage(type, payload) {
        if (window.chrome && window.chrome.webview) {
            const message = { type, payload: payload || {} };
            window.chrome.webview.postMessage(message);
        } else {
            console.warn('WebView2 bridge not available');
        }
    }

    // Handle incoming messages from Core
    function handleMessage(event) {
        try {
            const message = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
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
    function init() {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.addEventListener('message', handleMessage);
        }

        // Register core message handlers
        onMessage('state-changed', handleStateChanged);
        onMessage('test-result', handleTestResult);
        onMessage('launch-result', handleLaunchResult);

        // Request initial state
        sendMessage('get-state');
    }

    // Handle state updates from Core
    function handleStateChanged(payload) {
        state = payload;
        updateUI();
    }

    // Handle launch result
    function handleLaunchResult(payload) {
        if (!payload.success) {
            showToast(payload.error || 'Failed to launch ClickUp', 'error');
        }
    }

    // Simple toast notification
    function showToast(message, type) {
        // Create toast element
        const toast = document.createElement('div');
        toast.className = `toast toast-${type || 'info'}`;
        toast.textContent = message;
        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 0.75rem 1rem;
            background: ${type === 'error' ? 'rgba(248, 113, 113, 0.9)' : 'rgba(125, 211, 252, 0.9)'};
            color: ${type === 'error' ? '#fff' : '#0f172a'};
            border-radius: 6px;
            font-size: 0.875rem;
            z-index: 10000;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.3);
        `;
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transition = 'opacity 0.3s';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    // Update badge
    function updateBadge(id, statusClass, text) {
        const badge = document.getElementById(id);
        if (badge) {
            badge.textContent = text;
            badge.className = `badge ${statusClass ? 'status-' + statusClass : ''}`;
        }
    }

    // Handle API token test result
    function handleTestResult(payload) {
        const statusEl = document.getElementById('token-status');
        const testBtn = document.getElementById('test-btn');
        
        if (testBtn) {
            testBtn.disabled = false;
            testBtn.textContent = 'Test Connection';
        }

        if (payload.success) {
            if (statusEl) {
                statusEl.textContent = 'Valid';
                statusEl.className = 'status-value status-valid';
            }
        } else {
            if (statusEl) {
                statusEl.textContent = 'Invalid';
                statusEl.className = 'status-value status-invalid';
            }
            // Show error briefly
            const errorEl = document.getElementById('token-error');
            if (errorEl && payload.error) {
                errorEl.textContent = payload.error;
                errorEl.style.display = 'block';
                setTimeout(() => {
                    errorEl.style.display = 'none';
                }, 5000);
            }
        }
    }

    // Update UI based on current state
    function updateUI() {
        // Header version
        const headerVersionEl = document.getElementById('header-version');
        if (headerVersionEl) {
            headerVersionEl.textContent = state.version || '1.0.0';
        }

        // Version and diagnostics
        const versionEl = document.getElementById('version');
        if (versionEl) {
            versionEl.textContent = state.version || '1.0.0';
        }

        const dotNetEl = document.getElementById('dotnet-version');
        if (dotNetEl) {
            dotNetEl.textContent = state.dotNetVersion || '-';
        }

        const webView2El = document.getElementById('webview2-version');
        if (webView2El) {
            webView2El.textContent = state.webView2Version || '-';
        }

        const clickUpStatusEl = document.getElementById('clickup-desktop-status');
        if (clickUpStatusEl) {
            // Use same logic as badge for consistency
            let statusText = '';
            let statusClass = 'status-value';
            
            if (state.clickUpDesktopStatus === 'Running') {
                if (state.clickUpDebugPortAvailable === true) {
                    statusText = 'Running (Debug Ready)';
                    statusClass += ' status-valid';
                } else if (state.clickUpDebugPortAvailable === false) {
                    statusText = 'Running (No Debug)';
                    statusClass += ' status-untested';
                } else {
                    statusText = 'Running (Unknown)';
                    statusClass += ' status-untested';
                }
            } else {
                statusText = 'Not Running';
                statusClass += ' status-none';
            }
            
            clickUpStatusEl.textContent = statusText;
            clickUpStatusEl.className = statusClass;
        }

        const uptimeEl = document.getElementById('uptime');
        if (uptimeEl) {
            uptimeEl.textContent = state.uptime || '-';
        }

        // Update status badges
        // ClickUp badge: show both process and debug status
        let clickUpBadgeText = '';
        let clickUpBadgeClass = 'none';
        let clickUpBadgeTitle = '';
        
        if (state.clickUpDesktopStatus === 'Running') {
            if (state.clickUpDebugPortAvailable === true) {
                clickUpBadgeText = 'Running (Debug Ready)';
                clickUpBadgeClass = 'valid';
                clickUpBadgeTitle = 'ClickUp Desktop is running and debug port is available';
            } else if (state.clickUpDebugPortAvailable === false) {
                clickUpBadgeText = 'Running (No Debug)';
                clickUpBadgeClass = 'untested';
                clickUpBadgeTitle = 'ClickUp Desktop is running but debug port is not available';
            } else {
                clickUpBadgeText = 'Running (Unknown)';
                clickUpBadgeClass = 'untested';
                clickUpBadgeTitle = 'ClickUp Desktop is running, debug status not checked';
            }
        } else {
            clickUpBadgeText = 'Not Running';
            clickUpBadgeClass = 'none';
            clickUpBadgeTitle = 'ClickUp Desktop is not running';
        }
        
        const clickUpBadge = document.getElementById('badge-clickup');
        if (clickUpBadge) {
            clickUpBadge.textContent = clickUpBadgeText;
            clickUpBadge.className = `badge status-${clickUpBadgeClass}`;
            clickUpBadge.title = clickUpBadgeTitle;
        }

        // API badge: clearer labels
        let apiBadgeText = '';
        let apiBadgeClass = 'none';
        let apiBadgeTitle = '';
        
        if (!state.hasApiToken) {
            apiBadgeText = 'API: Not Set';
            apiBadgeClass = 'none';
            apiBadgeTitle = 'No ClickUp API token configured';
        } else if (state.tokenValid === true) {
            apiBadgeText = 'API: Valid';
            apiBadgeClass = 'valid';
            apiBadgeTitle = 'ClickUp API token is valid';
        } else if (state.tokenValid === false) {
            apiBadgeText = 'API: Invalid';
            apiBadgeClass = 'invalid';
            apiBadgeTitle = 'ClickUp API token is invalid';
        } else {
            apiBadgeText = 'API: Untested';
            apiBadgeClass = 'untested';
            apiBadgeTitle = 'ClickUp API token is configured but not tested';
        }
        
        const apiBadge = document.getElementById('badge-api');
        if (apiBadge) {
            apiBadge.textContent = apiBadgeText;
            apiBadge.className = `badge status-${apiBadgeClass}`;
            apiBadge.title = apiBadgeTitle;
        }

        // Uptime badge
        const uptimeBadge = document.getElementById('badge-uptime');
        if (uptimeBadge) {
            uptimeBadge.textContent = `Uptime: ${state.uptime || '0m'}`;
            uptimeBadge.title = `Application uptime: ${state.uptime || '0m'}`;
        }

        const logPathEl = document.getElementById('log-path');
        if (logPathEl) {
            logPathEl.textContent = state.logFilePath || '-';
            logPathEl.title = state.logFilePath || '';
        }

        // Update tools list
        updateToolsList();

        // Token status
        const statusEl = document.getElementById('token-status');
        if (statusEl) {
            if (!state.hasApiToken) {
                statusEl.textContent = 'Not configured';
                statusEl.className = 'status-value status-none';
            } else if (state.tokenValid === null) {
                statusEl.textContent = 'Configured (untested)';
                statusEl.className = 'status-value status-untested';
            } else if (state.tokenValid === true) {
                statusEl.textContent = 'Valid';
                statusEl.className = 'status-value status-valid';
            } else {
                statusEl.textContent = 'Invalid';
                statusEl.className = 'status-value status-invalid';
            }
        }

        // Token input - show masked value if token exists
        const tokenInput = document.getElementById('token-input');
        if (tokenInput) {
            if (state.hasApiToken && !tokenInput.dataset.userEditing) {
                tokenInput.placeholder = '••••••••••••••••';
                tokenInput.value = '';
            } else if (!state.hasApiToken) {
                tokenInput.placeholder = 'Enter your ClickUp API token';
            }
        }

        // Button states
        const saveBtn = document.getElementById('save-btn');
        const testBtn = document.getElementById('test-btn');
        const clearBtn = document.getElementById('clear-btn');

        if (saveBtn) {
            // Save is enabled if there's text in the input
            const hasInput = tokenInput && tokenInput.value.trim().length > 0;
            saveBtn.disabled = !hasInput;
        }

        if (testBtn) {
            testBtn.disabled = !state.hasApiToken;
        }

        if (clearBtn) {
            clearBtn.disabled = !state.hasApiToken;
        }

        // Launch button - disabled if no ClickUp path
        const launchBtn = document.getElementById('launch-debug-btn');
        if (launchBtn) {
            launchBtn.disabled = !state.clickUpInstallPath;
            launchBtn.title = state.clickUpInstallPath 
                ? 'Launch ClickUp with remote debugging enabled'
                : 'ClickUp Desktop not found';
        }

        // ClickUp path display
        const pathEl = document.getElementById('clickup-path');
        if (pathEl) {
            pathEl.textContent = state.clickUpInstallPath || '-';
            pathEl.title = state.clickUpInstallPathOverride 
                ? `Configured: ${state.clickUpInstallPathOverride}` 
                : (state.clickUpInstallPath ? 'Auto-detected' : 'Not found');
        }

        // Debug port
        const portInput = document.getElementById('debug-port-input');
        if (portInput) {
            portInput.value = state.debugPort || 9222;
        }

        // Restart if running
        const restartToggle = document.getElementById('restart-if-running-toggle');
        if (restartToggle) {
            restartToggle.checked = state.restartIfRunning || false;
        }

        // Autostart toggle
        const autostartToggle = document.getElementById('autostart-toggle');
        if (autostartToggle) {
            autostartToggle.checked = state.autostartEnabled || false;
        }
    }

    // Event handlers for UI actions
    function handleSaveToken() {
        const tokenInput = document.getElementById('token-input');
        if (tokenInput && tokenInput.value.trim()) {
            sendMessage('set-api-token', { token: tokenInput.value.trim() });
            tokenInput.value = '';
            tokenInput.dataset.userEditing = '';
        }
    }

    function handleTestToken() {
        const testBtn = document.getElementById('test-btn');
        if (testBtn) {
            testBtn.disabled = true;
            testBtn.textContent = 'Testing...';
        }
        sendMessage('test-api-token');
    }

    function handleClearToken() {
        if (confirm('Are you sure you want to remove the API token?')) {
            sendMessage('clear-api-token');
        }
    }

    function handleTokenInput() {
        const tokenInput = document.getElementById('token-input');
        if (tokenInput) {
            tokenInput.dataset.userEditing = tokenInput.value.length > 0 ? 'true' : '';
        }
        updateUI();
    }

    function handleOpenLogFolder() {
        sendMessage('open-log-folder');
    }

    function handleRefreshRuntimeStatus() {
        sendMessage('refresh-runtime-status');
        // Also test API token if one is configured
        if (state.hasApiToken) {
            sendMessage('test-api-token');
        }
    }

    function handleToolToggle(toolId, enabled) {
        sendMessage('set-tool-enabled', { toolId, enabled });
    }

    // Update tools list UI - create individual collapsible sections per tool
    function updateToolsList() {
        const toolsContainer = document.getElementById('tools-container');
        if (!toolsContainer) return;

        // Clear existing
        toolsContainer.innerHTML = '';

        if (!state.tools || state.tools.length === 0) {
            toolsContainer.innerHTML = '<p class="help-text">No tools available.</p>';
            return;
        }

        state.tools.forEach(tool => {
            // Create details element (collapsible section)
            const details = document.createElement('details');
            details.className = 'section';
            
            // Summary (tool name)
            const summary = document.createElement('summary');
            summary.textContent = tool.name;
            details.appendChild(summary);

            // Description
            const description = document.createElement('p');
            description.className = 'card-description';
            description.textContent = tool.description;
            details.appendChild(description);

            // Enable/disable toggle
            const optionRow = document.createElement('div');
            optionRow.className = 'option-row';

            const label = document.createElement('span');
            label.textContent = 'Enable Tool';

            const toggle = document.createElement('label');
            toggle.className = 'toggle';

            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.checked = tool.enabled;
            checkbox.addEventListener('change', () => {
                handleToolToggle(tool.id, checkbox.checked);
            });

            const slider = document.createElement('span');
            slider.className = 'toggle-slider';

            toggle.appendChild(checkbox);
            toggle.appendChild(slider);

            optionRow.appendChild(label);
            optionRow.appendChild(toggle);
            details.appendChild(optionRow);

            // Future: tool-specific settings can be added here

            toolsContainer.appendChild(details);
        });
    }

    // Setup UI event listeners
    function setupEventListeners() {
        const saveBtn = document.getElementById('save-btn');
        const testBtn = document.getElementById('test-btn');
        const clearBtn = document.getElementById('clear-btn');
        const tokenInput = document.getElementById('token-input');

        if (saveBtn) {
            saveBtn.addEventListener('click', handleSaveToken);
        }

        if (testBtn) {
            testBtn.addEventListener('click', handleTestToken);
        }

        if (clearBtn) {
            clearBtn.addEventListener('click', handleClearToken);
        }

        if (tokenInput) {
            tokenInput.addEventListener('input', handleTokenInput);
            tokenInput.addEventListener('keydown', (e) => {
                if (e.key === 'Enter') {
                    handleSaveToken();
                }
            });
        }

        const openLogsBtn = document.getElementById('open-logs-btn');
        if (openLogsBtn) {
            openLogsBtn.addEventListener('click', handleOpenLogFolder);
        }

        const refreshRuntimeBtn = document.getElementById('refresh-runtime-btn');
        if (refreshRuntimeBtn) {
            refreshRuntimeBtn.addEventListener('click', handleRefreshRuntimeStatus);
        }

        // Header refresh button
        const headerRefreshBtn = document.getElementById('header-refresh-btn');
        if (headerRefreshBtn) {
            headerRefreshBtn.addEventListener('click', handleRefreshRuntimeStatus);
        }

        // Launch debug button
        const launchBtn = document.getElementById('launch-debug-btn');
        if (launchBtn) {
            launchBtn.addEventListener('click', () => {
                sendMessage('launch-clickup-debug');
            });
        }

        // Autostart toggle
        const autostartToggle = document.getElementById('autostart-toggle');
        if (autostartToggle) {
            autostartToggle.addEventListener('change', (e) => {
                sendMessage('set-autostart', { enabled: e.target.checked });
            });
        }

        // Open ClickUp location
        const openClickUpBtn = document.getElementById('open-clickup-btn');
        if (openClickUpBtn) {
            openClickUpBtn.addEventListener('click', () => {
                sendMessage('open-clickup-location');
            });
        }

        // Set ClickUp path override
        const setClickUpPathBtn = document.getElementById('set-clickup-path-btn');
        if (setClickUpPathBtn) {
            setClickUpPathBtn.addEventListener('click', () => {
                const path = prompt('Enter ClickUp executable path:', state.clickUpInstallPathOverride || '');
                if (path !== null) {
                    sendMessage('set-clickup-path-override', { path: path.trim() || null });
                }
            });
        }

        // Set debug port
        const setDebugPortBtn = document.getElementById('set-debug-port-btn');
        if (setDebugPortBtn) {
            setDebugPortBtn.addEventListener('click', () => {
                const portInput = document.getElementById('debug-port-input');
                const port = parseInt(portInput?.value || '9222', 10);
                if (port >= 1024 && port <= 65535) {
                    sendMessage('set-debug-port', { port });
                }
            });
        }

        // Restart if running toggle
        const restartToggle = document.getElementById('restart-if-running-toggle');
        if (restartToggle) {
            restartToggle.addEventListener('change', (e) => {
                sendMessage('set-restart-if-running', { enabled: e.target.checked });
            });
        }
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            setupEventListeners();
            init();
        });
    } else {
        setupEventListeners();
        init();
    }

    // Expose for debugging
    window.PowerTools = {
        sendMessage,
        onMessage,
        getState: () => state
    };
})();

