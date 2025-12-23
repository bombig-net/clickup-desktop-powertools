<script lang="ts">
    import { appState } from '$lib/state';
    import { sendMessage } from '$lib/bridge';
    import { getClickUpBadge, getApiBadge, getUptimeBadge } from '$lib/status';
    import { Button } from '$lib/components/ui/button';
    import { Badge } from '$lib/components/ui/badge';
    import type { BadgeVariant as StatusBadgeVariant } from '$lib/status';

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
    
    // Map badge variants to shadcn variants
    function getBadgeVariant(variant: StatusBadgeVariant): 'default' | 'destructive' | 'secondary' | 'outline' {
        if (variant === 'valid') return 'default';
        if (variant === 'invalid') return 'destructive';
        if (variant === 'untested') return 'secondary';
        return 'outline';
    }
</script>

<header class="text-center pb-6 border-b border-border mb-8">
    <h1 class="text-2xl font-semibold text-foreground tracking-tight mb-2">ClickUp Desktop PowerTools</h1>
    <div class="flex gap-4 justify-center items-center text-sm text-muted-foreground mb-4">
        <span>v<span id="header-version">{$appState.version || '1.0.0'}</span></span>
        <a href="https://app.clickup.com/settings/apps" target="_blank" rel="noopener" 
           class="text-primary hover:underline">API Settings</a>
    </div>
    <div class="flex gap-2 justify-center items-center mb-4 flex-wrap">
        <Badge id="badge-clickup" 
               variant={getBadgeVariant(clickUpBadge.variant)}
               title={clickUpBadge.title}>
            {clickUpBadge.text}
        </Badge>
        <Badge id="badge-api"
               variant={getBadgeVariant(apiBadge.variant)}
               title={apiBadge.title}>
            {apiBadge.text}
        </Badge>
        <Badge id="badge-uptime" 
               variant={getBadgeVariant(uptimeBadge.variant)}
               title={uptimeBadge.title}>
            {uptimeBadge.text}
        </Badge>
        <Button id="header-refresh-btn" 
                variant="outline"
                size="sm"
                onclick={handleRefreshRuntimeStatus}
                title="Refresh all status information">
            Refresh
        </Button>
    </div>
    <Button id="launch-debug-btn" 
            variant="default"
            onclick={() => sendMessage('launch-clickup-debug')}
            disabled={!$appState.clickUpInstallPath}
            title={$appState.clickUpInstallPath ? 'Launch ClickUp with remote debugging enabled' : 'ClickUp Desktop not found'}>
        Launch ClickUp Debug
    </Button>
</header>

