<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { sendMessage, onMessage, offMessage } from '../../lib/bridge';
    import { Button } from '$lib/components/ui/button';

    let connectionState = '-';
    let lastKnownUrl = '-';
    let debugPort = '-';
    let debugPortAvailable: boolean | null = null;
    let clickUpStatus = '-';
    let recentNavigations: string[] = [];

    function getConnectionStateClass(state: string): string {
        if (state === 'Connected') return 'text-green-400';
        if (state === 'Failed') return 'text-red-400';
        return 'text-yellow-400';
    }

    function getPortAvailableText(available: boolean | null): string {
        if (available === true) return 'Yes';
        if (available === false) return 'No';
        return 'Unknown';
    }

    function getPortAvailableClass(available: boolean | null): string {
        if (available === true) return 'text-green-400';
        if (available === false) return 'text-red-400';
        return 'text-yellow-400';
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

<div class="mt-4">
    <div class="flex justify-between items-center py-2 border-b border-border">
        <span class="text-sm text-muted-foreground">Connection State</span>
        <span id="debug-connection-state" 
              class="text-sm font-medium {getConnectionStateClass(connectionState)}">
            {connectionState}
        </span>
    </div>
    
    <div class="flex justify-between items-center py-2 border-b border-border">
        <span class="text-sm text-muted-foreground">Last Known URL</span>
        <span id="debug-last-url" 
              class="text-xs font-mono text-muted-foreground max-w-[200px] truncate">
            {lastKnownUrl}
        </span>
    </div>
    
    <div class="flex justify-between items-center py-2 border-b border-border">
        <span class="text-sm text-muted-foreground">Debug Port</span>
        <span id="debug-port" class="text-sm font-medium">{debugPort}</span>
    </div>
    
    <div class="flex justify-between items-center py-2 border-b border-border">
        <span class="text-sm text-muted-foreground">Port Available</span>
        <span id="debug-port-available" 
              class="text-sm font-medium {getPortAvailableClass(debugPortAvailable)}">
            {getPortAvailableText(debugPortAvailable)}
        </span>
    </div>
    
    <div class="flex justify-between items-center py-2 border-b border-border">
        <span class="text-sm text-muted-foreground">ClickUp Status</span>
        <span id="debug-clickup-status" class="text-sm font-medium">{clickUpStatus}</span>
    </div>
    
    <div class="mt-4">
        <div class="text-sm text-muted-foreground mb-2">Recent Navigations</div>
        <div id="debug-navigations" 
             class="max-h-[200px] overflow-y-auto bg-muted rounded p-2">
            {#if recentNavigations.length === 0}
                <div class="text-xs text-muted-foreground">No navigation events yet</div>
            {:else}
                {#each recentNavigations as nav}
                    <div class="py-2 text-sm font-mono text-muted-foreground border-b border-border last:border-0">
                        {nav}
                    </div>
                {/each}
            {/if}
        </div>
    </div>
    
    <Button 
        variant="outline"
        class="mt-4"
        onclick={handleRefresh}>
        Refresh
    </Button>
</div>

