<script lang="ts">
    import { appState } from '$lib/state';
    import { sendMessage } from '$lib/bridge';
    import { Accordion, AccordionItem, AccordionTrigger, AccordionContent } from '$lib/components/ui/accordion';
    import { Switch } from '$lib/components/ui/switch';
    import { Input } from '$lib/components/ui/input';
    import { Button } from '$lib/components/ui/button';
    import Monitor from '@lucide/svelte/icons/monitor';
    import FolderOpen from '@lucide/svelte/icons/folder-open';
    import ExternalLink from '@lucide/svelte/icons/external-link';
    import Settings from '@lucide/svelte/icons/settings';
    import Server from '@lucide/svelte/icons/server';
    import Check from '@lucide/svelte/icons/check';
    import RefreshCw from '@lucide/svelte/icons/refresh-cw';

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
        <AccordionTrigger class="text-lg font-semibold flex items-center gap-2">
            <Settings class="size-5" />
            System & Integration
        </AccordionTrigger>
        <AccordionContent>
            <div class="space-y-6 pt-2">
                <p class="text-sm text-muted-foreground text-balance">Configure system integration settings and debug options.</p>
                <div class="flex items-center gap-3 py-3">
                    <span class="flex-1 text-sm text-muted-foreground flex items-center gap-2">
                        <Monitor class="size-4" />
                        Start with Windows
                    </span>
                    <Switch 
                        id="autostart-toggle"
                        bind:checked={autostartEnabled}
                        semantic="positive"
                        onclick={() => handleSetAutostart(autostartEnabled)} />
                </div>
                
                <div class="flex items-center gap-3 py-3">
                    <span class="flex-1 text-sm text-muted-foreground flex items-center gap-2">
                        <FolderOpen class="size-4" />
                        ClickUp Location
                    </span>
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
                        <ExternalLink class="size-4" />
                        Open
                    </Button>
                    <Button 
                        id="set-clickup-path-btn" 
                        variant="outline"
                        size="sm"
                        onclick={handleSetClickUpPath}>
                        <Settings class="size-4" />
                        Set Path...
                    </Button>
                </div>
                
                <div class="flex items-center gap-3 py-3">
                    <span class="flex-1 text-sm text-muted-foreground flex items-center gap-2">
                        <Server class="size-4" />
                        Debug Port
                    </span>
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
                        <Check class="size-4" />
                        Set
                    </Button>
                </div>
                
                <div class="flex items-center gap-3 py-3">
                    <span class="flex-1 text-sm text-muted-foreground flex items-center gap-2">
                        <RefreshCw class="size-4" />
                        Restart if Running
                    </span>
                    <Switch 
                        id="restart-if-running-toggle"
                        bind:checked={restartIfRunning}
                        onclick={() => handleSetRestartIfRunning(restartIfRunning)} />
                    <span class="text-xs text-muted-foreground">Kill existing ClickUp before launching debug mode</span>
                </div>
            </div>
        </AccordionContent>
    </AccordionItem>
</Accordion>

