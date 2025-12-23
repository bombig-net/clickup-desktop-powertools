<script lang="ts">
    import { appState } from '$lib/state';
    import { sendMessage } from '$lib/bridge';
    import { Accordion, AccordionItem, AccordionTrigger, AccordionContent } from '$lib/components/ui/accordion';
    import { Switch } from '$lib/components/ui/switch';
    import { Input } from '$lib/components/ui/input';
    import { Button } from '$lib/components/ui/button';

    let autostartEnabled = $appState.autostartEnabled;
    let restartIfRunning = $appState.restartIfRunning;
    let debugPort: string = String($appState.debugPort || 9222);
    let accordionValue: string | undefined = undefined;

    // Sync with appState changes
    $: autostartEnabled = $appState.autostartEnabled;
    $: restartIfRunning = $appState.restartIfRunning;
    $: debugPort = String($appState.debugPort || 9222);

    function handleSetAutostart(enabled: boolean): void {
        sendMessage('set-autostart', { enabled });
    }

    function handleSetRestartIfRunning(enabled: boolean): void {
        sendMessage('set-restart-if-running', { enabled });
    }

    function handleSetDebugPort(): void {
        const port = parseInt(String(debugPort || 9222), 10);
        if (port >= 1024 && port <= 65535) {
            sendMessage('set-debug-port', { port });
        }
    }

    function handleSetClickUpPath(): void {
        const path = prompt('Enter ClickUp executable path:', $appState.clickUpInstallPathOverride || '');
        if (path !== null) {
            sendMessage('set-clickup-path-override', { path: path.trim() || null });
        }
    }

    function handleDebugPortInput(e: Event): void {
        const target = e.target as HTMLInputElement;
        const port = parseInt(target.value || '9222', 10);
        if (!isNaN(port) && port >= 1024 && port <= 65535) {
            debugPort = String(port);
            appState.update(s => ({ ...s, debugPort: port }));
        }
    }
</script>

<Accordion type="single" bind:value={accordionValue}>
    <AccordionItem value="system-settings">
        <AccordionTrigger class="font-semibold">
            System & Integration
        </AccordionTrigger>
        <AccordionContent>
            <div class="flex items-center gap-3 py-3 border-b border-border">
                <span class="flex-1 text-sm text-muted-foreground">Start with Windows</span>
                <Switch 
                    id="autostart-toggle"
                    bind:checked={autostartEnabled}
                    onclick={() => handleSetAutostart(autostartEnabled)} />
            </div>
            
            <div class="flex items-center gap-3 py-3 border-b border-border">
                <span class="flex-1 text-sm text-muted-foreground">ClickUp Location</span>
                <span id="clickup-path" 
                      class="text-xs font-mono text-muted-foreground max-w-[200px] truncate"
                      title={$appState.clickUpInstallPathOverride 
                          ? `Configured: ${$appState.clickUpInstallPathOverride}` 
                          : ($appState.clickUpInstallPath ? 'Auto-detected' : 'Not found')}>
                    {$appState.clickUpInstallPath || '-'}
                </span>
                <Button 
                    id="open-clickup-btn" 
                    variant="outline"
                    size="sm"
                    onclick={() => sendMessage('open-clickup-location')}>
                    Open
                </Button>
                <Button 
                    id="set-clickup-path-btn" 
                    variant="outline"
                    size="sm"
                    onclick={handleSetClickUpPath}>
                    Set Path...
                </Button>
            </div>
            
            <div class="flex items-center gap-3 py-3 border-b border-border">
                <span class="flex-1 text-sm text-muted-foreground">Debug Port</span>
                <Input 
                    type="number"
                    id="debug-port-input"
                    bind:value={debugPort}
                    oninput={handleDebugPortInput}
                    min="1024" 
                    max="65535" 
                    class="w-24 text-center" />
                <Button 
                    id="set-debug-port-btn" 
                    variant="outline"
                    size="sm"
                    onclick={handleSetDebugPort}>
                    Set
                </Button>
            </div>
            
            <div class="flex items-center gap-3 py-3">
                <span class="flex-1 text-sm text-muted-foreground">Restart if Running</span>
                <Switch 
                    id="restart-if-running-toggle"
                    bind:checked={restartIfRunning}
                    onclick={() => handleSetRestartIfRunning(restartIfRunning)} />
                <span class="text-xs text-muted-foreground">Kill existing ClickUp before launching debug mode</span>
            </div>
        </AccordionContent>
    </AccordionItem>
</Accordion>

