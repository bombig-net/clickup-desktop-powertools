<script lang="ts">
    import { appState } from '$lib/state';
    import { sendMessage } from '$lib/bridge';
    import { getClickUpBadge, getApiBadge, getUptimeBadge } from '$lib/status';
    import { Button } from '$lib/components/ui/button';
    import { Badge } from '$lib/components/ui/badge';
    import type { BadgeVariant as StatusBadgeVariant } from '$lib/status';
    import { getStatusDotClass } from '$lib/utils';
    import RefreshCw from '@lucide/svelte/icons/refresh-cw';
    import Rocket from '@lucide/svelte/icons/rocket';
    import ExternalLink from '@lucide/svelte/icons/external-link';

    function handleRefreshRuntimeStatus(): void {
        sendMessage('refresh-runtime-status');
        if ($appState.hasApiToken) {
            sendMessage('test-api-token');
        }
    }

    // Reactive badge computations
    $: clickUpBadge = getClickUpBadge($appState);
    $: apiBadge = getApiBadge($appState);
    $: uptimeBadge = getUptimeBadge($appState);
</script>

<header class="flex flex-col items-center gap-4">
    <div class="flex flex-col items-center gap-2 mb-2">
        <h1 class="text-4xl md:text-5xl lg:text-6xl font-bold tracking-tight gradient-text">
            ClickUp Desktop PowerTools
        </h1>
        <div class="flex gap-4 justify-center items-center text-sm text-muted-foreground text-balance">
            <span>v<span id="header-version">{$appState.version || '1.0.0'}</span></span>
            <a href="https://app.clickup.com/settings/apps" target="_blank" rel="noopener" 
               class="text-foreground/80 hover:text-foreground hover:underline flex items-center gap-1 transition-colors">
                API Settings
                <ExternalLink class="size-3" />
            </a>
        </div>
    </div>
    
    <div class="flex gap-3 justify-center items-center flex-wrap">
        <Badge id="badge-clickup" 
               variant="outline"
               title={clickUpBadge.title}
               class="flex items-center gap-1.5">
            <span class={getStatusDotClass(clickUpBadge.variant)}></span>
            {clickUpBadge.text}
        </Badge>
        <Badge id="badge-api"
               variant="outline"
               title={apiBadge.title}
               class="flex items-center gap-1.5">
            <span class={getStatusDotClass(apiBadge.variant)}></span>
            {apiBadge.text}
        </Badge>
        <Badge id="badge-uptime" 
               variant="outline"
               title={uptimeBadge.title}
               class="flex items-center gap-1.5">
            <span class={getStatusDotClass(uptimeBadge.variant)}></span>
            {uptimeBadge.text}
        </Badge>
        <Button id="header-refresh-btn" 
                variant="outline"
                size="sm"
                onclick={handleRefreshRuntimeStatus}
                title="Refresh all status information">
            <RefreshCw class="size-4" />
            Refresh
        </Button>
    </div>
    
    <Button id="launch-debug-btn" 
            variant="default"
            size="lg"
            onclick={() => sendMessage('launch-clickup-debug')}
            disabled={!$appState.clickUpInstallPath}
            title={$appState.clickUpInstallPath ? 'Launch ClickUp with remote debugging enabled' : 'ClickUp Desktop not found'}
            class="mt-2">
        <Rocket class="size-4" />
        Launch ClickUp Debug
    </Button>
</header>

