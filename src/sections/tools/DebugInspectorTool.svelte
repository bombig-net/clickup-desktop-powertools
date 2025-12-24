<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { sendMessage, onMessage, offMessage } from '$lib/bridge';
    import { Button } from '$lib/components/ui/button';
    import { getStatusDotClass } from '$lib/utils';
    import RefreshCw from '@lucide/svelte/icons/refresh-cw';
    import Compass from '@lucide/svelte/icons/compass';
    import Server from '@lucide/svelte/icons/server';

    let connectionState = '-';
    let lastKnownUrl = '-';
    let debugPort = '-';
    let debugPortAvailable: boolean | null = null;
    let clickUpStatus = '-';
    let recentNavigations: string[] = [];

    function getConnectionStateVariant(state: string): 'valid' | 'invalid' | 'untested' {
        if (state === 'Connected') return 'valid';
        if (state === 'Failed') return 'invalid';
        return 'untested';
    }

    function getPortAvailableText(available: boolean | null): string {
        if (available === true) return 'Yes';
        if (available === false) return 'No';
        return 'Unknown';
    }

    function getPortAvailableVariant(available: boolean | null): 'valid' | 'invalid' | 'untested' {
        if (available === true) return 'valid';
        if (available === false) return 'invalid';
        return 'untested';
    }

    function handleDebugInspectorState(payload: unknown): void {
        const state = payload as {
            connectionState?: string;
            lastKnownUrl?: string;
            debugPort?: number;
            debugPortAvailable?: boolean | null;
            clickUpDesktopStatus?: string;
            recentNavigations?: string[];
        };
        connectionState = state.connectionState || '-';
        lastKnownUrl = state.lastKnownUrl || '-';
        debugPort = state.debugPort?.toString() || '-';
        debugPortAvailable = state.debugPortAvailable ?? null;
        clickUpStatus = state.clickUpDesktopStatus || '-';
        recentNavigations = state.recentNavigations || [];
    }

    function handleRefresh(): void {
        sendMessage('get-debug-inspector-state');
    }

    // Store handler reference for cleanup
    const debugInspectorStateHandler = handleDebugInspectorState;

    onMount(() => {
        onMessage('debug-inspector-state', debugInspectorStateHandler);
        sendMessage('get-debug-inspector-state');
    });

    onDestroy(() => {
        offMessage('debug-inspector-state', debugInspectorStateHandler);
    });
</script>

<div class="space-y-4">
    <div class="flex justify-between items-center py-3">
        <span class="text-sm text-muted-foreground">Connection State</span>
        <span id="debug-connection-state" 
              class="text-sm font-medium text-foreground flex items-center gap-1.5">
            <span class={getStatusDotClass(getConnectionStateVariant(connectionState))}></span>
            {connectionState}
        </span>
    </div>
    
    <div class="flex justify-between items-center py-3">
        <span class="text-sm text-muted-foreground">Last Known URL</span>
        <span id="debug-last-url" 
              class="text-xs font-mono text-muted-foreground max-w-[200px] truncate">
            {lastKnownUrl}
        </span>
    </div>
    
    <div class="flex justify-between items-center py-3">
        <span class="text-sm text-muted-foreground flex items-center gap-2">
            <Server class="size-4" />
            Debug Port
        </span>
        <span id="debug-port" class="text-sm font-medium">{debugPort}</span>
    </div>
    
    <div class="flex justify-between items-center py-3">
        <span class="text-sm text-muted-foreground">Port Available</span>
        <span id="debug-port-available" 
              class="text-sm font-medium text-foreground flex items-center gap-1.5">
            <span class={getStatusDotClass(getPortAvailableVariant(debugPortAvailable))}></span>
            {getPortAvailableText(debugPortAvailable)}
        </span>
    </div>
    
    <div class="flex justify-between items-center py-3">
        <span class="text-sm text-muted-foreground">ClickUp Status</span>
        <span id="debug-clickup-status" class="text-sm font-medium">{clickUpStatus}</span>
    </div>
    
    <div class="pt-2">
        <div class="text-sm text-muted-foreground mb-3 flex items-center gap-2">
            <Compass class="size-4" />
            Recent Navigations
        </div>
        <div id="debug-navigations" 
             class="max-h-[200px] overflow-y-auto bg-muted rounded p-3">
            {#if recentNavigations.length === 0}
                <div class="text-xs text-muted-foreground">No navigation events yet</div>
            {:else}
                <div class="space-y-2">
                    {#each recentNavigations as nav}
                        <div class="text-sm font-mono text-muted-foreground">
                            {nav}
                        </div>
                    {/each}
                </div>
            {/if}
        </div>
    </div>
    
    <Button 
        variant="outline"
        class="mt-2"
        onclick={handleRefresh}>
        <RefreshCw class="size-4" />
        Refresh
    </Button>
</div>

