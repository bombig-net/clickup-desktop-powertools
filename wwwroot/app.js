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
        onMessage('custom-css-js-state', handleCustomCssJsState);
        onMessage('debug-inspector-state', handleDebugInspectorState);

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
        
        // Update Custom CSS/JS UI state if tool exists
        const customCssJsTool = state.tools?.find(t => t.id === 'custom-css-js');
        if (customCssJsTool) {
            updateCustomCssJsUIState(customCssJsTool);
        }

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

            // Enable/disable toggle (skip for custom-css-js, it has its own inside the section)
            if (tool.id !== 'custom-css-js') {
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
                    // Update tool.enabled immediately for UI responsiveness
                    tool.enabled = checkbox.checked;
                    // Request tool-specific state when enabled
                    if (checkbox.checked) {
                        if (tool.id === 'debug-inspector') {
                            setTimeout(() => sendMessage('get-debug-inspector-state'), 100);
                        }
                    }
                });

                const slider = document.createElement('span');
                slider.className = 'toggle-slider';

                toggle.appendChild(checkbox);
                toggle.appendChild(slider);

                optionRow.appendChild(label);
                optionRow.appendChild(toggle);
                details.appendChild(optionRow);
            }

            // Tool-specific UI
            if (tool.id === 'custom-css-js') {
                addCustomCssJsUI(details, tool);
            } else if (tool.id === 'debug-inspector') {
                addDebugInspectorUI(details, tool);
            }

            toolsContainer.appendChild(details);
        });
    }

    // Add Custom CSS/JS UI
    function addCustomCssJsUI(details, tool) {
        // Enable/disable toggle as first option row
        const enableOptionRow = document.createElement('div');
        enableOptionRow.className = 'option-row';
        enableOptionRow.style.marginTop = '1rem';
        enableOptionRow.style.paddingTop = '0.75rem';
        enableOptionRow.style.paddingBottom = '1rem';

        const enableLabel = document.createElement('span');
        enableLabel.textContent = 'Enable Tool';

        const toggle = document.createElement('label');
        toggle.className = 'toggle';

        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.checked = tool.enabled;
        checkbox.addEventListener('change', () => {
            handleToolToggle(tool.id, checkbox.checked);
            // Update tool.enabled immediately for UI responsiveness
            tool.enabled = checkbox.checked;
            // Request tool-specific state when enabled
            if (checkbox.checked) {
                setTimeout(() => {
                    sendMessage('get-custom-css-js');
                    // Update UI state
                    updateCustomCssJsUIState(tool);
                }, 100);
            } else {
                // Tool disabled - update UI immediately
                updateCustomCssJsUIState(tool);
            }
        });

        const slider = document.createElement('span');
        slider.className = 'toggle-slider';

        toggle.appendChild(checkbox);
        toggle.appendChild(slider);

        enableOptionRow.appendChild(enableLabel);
        enableOptionRow.appendChild(toggle);
        details.appendChild(enableOptionRow);

        const cssContainer = document.createElement('div');
        cssContainer.className = 'form-group';
        cssContainer.style.marginTop = '1rem';
        
        const cssLabel = document.createElement('label');
        cssLabel.textContent = 'Custom CSS';
        cssLabel.setAttribute('for', `css-input-${tool.id}`);
        cssContainer.appendChild(cssLabel);
        
        const cssTextarea = document.createElement('textarea');
        cssTextarea.id = `css-input-${tool.id}`;
        cssTextarea.className = 'code-input';
        cssTextarea.placeholder = '/* Enter custom CSS here */';
        cssTextarea.rows = 5;
        cssTextarea.disabled = !tool.enabled;
        cssContainer.appendChild(cssTextarea);
        
        const jsContainer = document.createElement('div');
        jsContainer.className = 'form-group';
        
        const jsLabel = document.createElement('label');
        jsLabel.textContent = 'Custom JavaScript';
        jsLabel.setAttribute('for', `js-input-${tool.id}`);
        jsContainer.appendChild(jsLabel);
        
        const jsTextarea = document.createElement('textarea');
        jsTextarea.id = `js-input-${tool.id}`;
        jsTextarea.className = 'code-input';
        jsTextarea.placeholder = '// Enter custom JavaScript here';
        jsTextarea.rows = 5;
        jsTextarea.disabled = !tool.enabled;
        jsContainer.appendChild(jsTextarea);
        
        const disabledNote = document.createElement('p');
        disabledNote.className = 'help-text';
        disabledNote.textContent = 'Enable tool to edit and apply custom CSS/JS';
        disabledNote.style.display = tool.enabled ? 'none' : 'block';
        disabledNote.style.marginTop = '0.5rem';
        
        // Footer container grouping Save & Apply button and status
        const footerContainer = document.createElement('div');
        footerContainer.className = 'tool-footer';
        footerContainer.style.display = 'flex';
        footerContainer.style.justifyContent = 'space-between';
        footerContainer.style.alignItems = 'center';
        footerContainer.style.gap = '1rem';
        footerContainer.style.marginTop = '1rem';
        footerContainer.style.paddingTop = '1rem';
        footerContainer.style.paddingBottom = '1rem';
        footerContainer.style.borderTop = '1px solid rgba(255, 255, 255, 0.05)';
        
        const saveBtn = document.createElement('button');
        saveBtn.className = 'btn btn-secondary';
        saveBtn.textContent = 'Save & Apply';
        saveBtn.disabled = !tool.enabled;
        
        // Track if apply has been attempted
        let applyAttempted = false;
        
        // Status display (initially hidden)
        const statusContainer = document.createElement('div');
        statusContainer.className = 'status-row';
        statusContainer.style.display = 'none';
        statusContainer.style.margin = '0';
        statusContainer.style.border = 'none';
        statusContainer.style.padding = '0';
        const statusLabel = document.createElement('span');
        statusLabel.className = 'status-label';
        statusLabel.textContent = 'Last Injection';
        statusContainer.appendChild(statusLabel);
        const statusValue = document.createElement('span');
        statusValue.className = 'status-value';
        statusValue.id = `css-js-status-${tool.id}`;
        statusValue.textContent = '-';
        statusContainer.appendChild(statusValue);
        
        const errorContainer = document.createElement('div');
        errorContainer.className = 'error-message';
        errorContainer.id = `css-js-error-${tool.id}`;
        errorContainer.style.display = 'none';
        
        // Clear status when content changes
        const clearStatus = () => {
            if (applyAttempted) {
                applyAttempted = false;
                statusContainer.style.display = 'none';
                statusContainer.removeAttribute('data-apply-attempted');
                statusValue.textContent = '-';
                statusValue.className = 'status-value';
                errorContainer.style.display = 'none';
            }
        };
        
        cssTextarea.addEventListener('input', clearStatus);
        jsTextarea.addEventListener('input', clearStatus);
        
        saveBtn.addEventListener('click', () => {
            // Double-check enabled state before sending
            if (!tool.enabled) {
                return;
            }
            applyAttempted = true;
            statusContainer.style.display = 'flex';
            // Mark status container as having attempted apply
            statusContainer.setAttribute('data-apply-attempted', 'true');
            const css = cssTextarea.value;
            const js = jsTextarea.value;
            sendMessage('set-custom-css-js', { css, javascript: js });
        });
        
        // Store reference for clearStatus function to access
        statusContainer._clearStatus = clearStatus;
        
        footerContainer.appendChild(saveBtn);
        footerContainer.appendChild(statusContainer);
        
        details.appendChild(cssContainer);
        details.appendChild(jsContainer);
        details.appendChild(disabledNote);
        details.appendChild(footerContainer);
        // Error container should have margin-top for spacing from footer
        errorContainer.style.marginTop = '0.5rem';
        details.appendChild(errorContainer);
        
        // Request initial state only if enabled
        if (tool.enabled) {
            sendMessage('get-custom-css-js');
        }
    }

    // Add Debug Inspector UI
    function addDebugInspectorUI(details, tool) {
        const infoContainer = document.createElement('div');
        infoContainer.className = 'debug-info';
        
        const connectionRow = document.createElement('div');
        connectionRow.className = 'status-row';
        connectionRow.innerHTML = '<span class="status-label">Connection State</span><span class="status-value" id="debug-connection-state">-</span>';
        
        const urlRow = document.createElement('div');
        urlRow.className = 'status-row';
        urlRow.innerHTML = '<span class="status-label">Last Known URL</span><span class="status-value status-path" id="debug-last-url">-</span>';
        
        const portRow = document.createElement('div');
        portRow.className = 'status-row';
        portRow.innerHTML = '<span class="status-label">Debug Port</span><span class="status-value" id="debug-port">-</span>';
        
        const portAvailableRow = document.createElement('div');
        portAvailableRow.className = 'status-row';
        portAvailableRow.innerHTML = '<span class="status-label">Port Available</span><span class="status-value" id="debug-port-available">-</span>';
        
        const clickupStatusRow = document.createElement('div');
        clickupStatusRow.className = 'status-row';
        clickupStatusRow.innerHTML = '<span class="status-label">ClickUp Status</span><span class="status-value" id="debug-clickup-status">-</span>';
        
        const navHeader = document.createElement('div');
        navHeader.className = 'status-label';
        navHeader.style.marginTop = '1rem';
        navHeader.textContent = 'Recent Navigations';
        
        const navList = document.createElement('div');
        navList.id = 'debug-navigations';
        navList.className = 'debug-list';
        
        const refreshBtn = document.createElement('button');
        refreshBtn.className = 'btn btn-secondary';
        refreshBtn.textContent = 'Refresh';
        refreshBtn.addEventListener('click', () => {
            sendMessage('get-debug-inspector-state');
        });
        
        infoContainer.appendChild(connectionRow);
        infoContainer.appendChild(urlRow);
        infoContainer.appendChild(portRow);
        infoContainer.appendChild(portAvailableRow);
        infoContainer.appendChild(clickupStatusRow);
        infoContainer.appendChild(navHeader);
        infoContainer.appendChild(navList);
        infoContainer.appendChild(refreshBtn);
        
        details.appendChild(infoContainer);
        
        // Request initial state
        sendMessage('get-debug-inspector-state');
    }

    // Update Custom CSS/JS UI state based on tool enabled status
    function updateCustomCssJsUIState(tool) {
        const cssInput = document.getElementById('css-input-custom-css-js');
        const jsInput = document.getElementById('js-input-custom-css-js');
        const statusContainer = cssInput?.closest('.section')?.querySelector('.status-row');
        const disabledNote = cssInput?.closest('.section')?.querySelector('.help-text');
        const saveBtn = cssInput?.closest('.section')?.querySelector('.tool-footer')?.querySelector('.btn');
        
        if (cssInput) cssInput.disabled = !tool.enabled;
        if (jsInput) jsInput.disabled = !tool.enabled;
        // Status container visibility is controlled by apply attempt, not just enabled state
        // But if disabled, hide it
        if (statusContainer && !tool.enabled) {
            statusContainer.style.display = 'none';
            statusContainer.removeAttribute('data-apply-attempted');
        }
        if (disabledNote) disabledNote.style.display = tool.enabled ? 'none' : 'block';
        if (saveBtn) saveBtn.disabled = !tool.enabled;
    }

    // Handle Custom CSS/JS state
    function handleCustomCssJsState(payload) {
        const cssInput = document.getElementById('css-input-custom-css-js');
        const jsInput = document.getElementById('js-input-custom-css-js');
        const statusEl = document.getElementById('css-js-status-custom-css-js');
        const errorEl = document.getElementById('css-js-error-custom-css-js');
        
        // Find status container to check if apply was attempted
        const statusContainer = statusEl?.closest('.status-row');
        const applyAttempted = statusContainer?.getAttribute('data-apply-attempted') === 'true';
        
        // Find tool enabled state from current state
        const tool = state.tools?.find(t => t.id === 'custom-css-js');
        const isEnabled = tool?.enabled ?? false;
        
        if (cssInput && payload.css !== undefined) {
            cssInput.value = payload.css || '';
        }
        if (jsInput && payload.js !== undefined) {
            jsInput.value = payload.js || '';
        }
        
        // Only show status/error if tool is enabled AND apply was attempted
        if (statusEl && statusContainer) {
            if (!isEnabled || !applyAttempted) {
                // Hide status if disabled or no apply attempted
                statusContainer.style.display = 'none';
                statusEl.textContent = '-';
                statusEl.className = 'status-value';
                if (errorEl) {
                    errorEl.style.display = 'none';
                }
            } else if (payload.lastError) {
                // Show error if apply was attempted and error exists
                statusContainer.style.display = 'flex';
                statusEl.textContent = 'Error';
                statusEl.className = 'status-value status-invalid';
                if (errorEl) {
                    errorEl.textContent = payload.lastError;
                    errorEl.style.display = 'block';
                }
            } else if (payload.lastResult) {
                // Show success if apply was attempted and result exists
                statusContainer.style.display = 'flex';
                statusEl.textContent = payload.lastResult;
                statusEl.className = 'status-value status-valid';
                if (errorEl) {
                    errorEl.style.display = 'none';
                }
            } else {
                // No result yet, but apply was attempted - show placeholder
                statusContainer.style.display = 'flex';
                statusEl.textContent = '-';
                statusEl.className = 'status-value';
                if (errorEl) {
                    errorEl.style.display = 'none';
                }
            }
        }
    }

    // Handle Debug Inspector state
    function handleDebugInspectorState(payload) {
        const connectionEl = document.getElementById('debug-connection-state');
        const urlEl = document.getElementById('debug-last-url');
        const portEl = document.getElementById('debug-port');
        const portAvailableEl = document.getElementById('debug-port-available');
        const clickupStatusEl = document.getElementById('debug-clickup-status');
        const navListEl = document.getElementById('debug-navigations');
        
        if (connectionEl) {
            connectionEl.textContent = payload.connectionState || '-';
            const stateClass = payload.connectionState === 'Connected' ? 'status-valid' : 
                              payload.connectionState === 'Failed' ? 'status-invalid' : 'status-untested';
            connectionEl.className = `status-value ${stateClass}`;
        }
        
        if (urlEl) {
            urlEl.textContent = payload.lastKnownUrl || '-';
        }
        
        if (portEl) {
            portEl.textContent = payload.debugPort?.toString() || '-';
        }
        
        if (portAvailableEl) {
            if (payload.debugPortAvailable === true) {
                portAvailableEl.textContent = 'Yes';
                portAvailableEl.className = 'status-value status-valid';
            } else if (payload.debugPortAvailable === false) {
                portAvailableEl.textContent = 'No';
                portAvailableEl.className = 'status-value status-invalid';
            } else {
                portAvailableEl.textContent = 'Unknown';
                portAvailableEl.className = 'status-value status-untested';
            }
        }
        
        if (clickupStatusEl) {
            clickupStatusEl.textContent = payload.clickUpDesktopStatus || '-';
        }
        
        if (navListEl && payload.recentNavigations) {
            if (payload.recentNavigations.length === 0) {
                navListEl.innerHTML = '<div class="help-text">No navigation events yet</div>';
            } else {
                navListEl.innerHTML = payload.recentNavigations.map(nav => 
                    `<div class="debug-list-item">${nav}</div>`
                ).join('');
            }
        }
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

