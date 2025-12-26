<script lang="ts">
    import { appState } from '$lib/state';
    import { sendMessage } from '$lib/bridge';
    import { getClickUpStatus } from '$lib/status';
    import { Accordion, AccordionItem, AccordionTrigger, AccordionContent } from '$lib/components/ui/accordion';
    import { Button } from '$lib/components/ui/button';
    import { getStatusDotClass } from '$lib/utils';
    import RefreshCw from '@lucide/svelte/icons/refresh-cw';
    import FolderOpen from '@lucide/svelte/icons/folder-open';
    import Info from '@lucide/svelte/icons/info';
    import FileText from '@lucide/svelte/icons/file-text';
    import Activity from '@lucide/svelte/icons/activity';

    function handleRefreshRuntimeStatus(): void {
        sendMessage('refresh-runtime-status');
    }

    // Reactive status computation
    $: clickUpStatus = getClickUpStatus($appState);
    let accordionValue: string | undefined = undefined;
</script>

<Accordion type="single" bind:value={accordionValue}>
    <AccordionItem value="diagnostics">
        <AccordionTrigger class="text-lg font-semibold flex items-center gap-2">
            <Activity class="size-5" />
            System Info
        </AccordionTrigger>
        <AccordionContent>
            <div class="space-y-6 pt-2">
                <p class="text-sm text-muted-foreground text-balance">View system status and diagnostic information.</p>
                <div class="flex justify-between items-center py-3">
                    <span class="text-sm text-muted-foreground flex items-center gap-2">
                        <Info class="size-4" />
                        Version
                    </span>
                    <span id="version" class="text-sm font-medium">{$appState.version || '1.0.0'}</span>
                </div>
                <div class="flex justify-between items-center py-3">
                    <span class="text-sm text-muted-foreground flex items-center gap-2">
                        <Info class="size-4" />
                        .NET Runtime
                    </span>
                    <span id="dotnet-version" class="text-sm font-medium">{$appState.dotNetVersion || '-'}</span>
                </div>
                <div class="flex justify-between items-center py-3">
                    <span class="text-sm text-muted-foreground flex items-center gap-2">
                        <Info class="size-4" />
                        WebView2
                    </span>
                    <span id="webview2-version" class="text-sm font-medium">{$appState.webView2Version || '-'}</span>
                </div>
                <div class="flex justify-between items-center py-3">
                    <span class="text-sm text-muted-foreground">ClickUp Desktop</span>
                    <div class="flex items-center gap-2">
                        <span id="clickup-desktop-status" 
                              class="text-sm font-medium text-foreground flex items-center gap-1.5">
                            <span class={getStatusDotClass(clickUpStatus.variant)}></span>
                            {clickUpStatus.text}
                        </span>
                        <Button 
                            id="refresh-runtime-btn" 
                            variant="outline"
                            size="sm"
                            onclick={handleRefreshRuntimeStatus}>
                            <RefreshCw class="size-4" />
                            Refresh
                        </Button>
                    </div>
                </div>
                <div class="flex justify-between items-center py-3">
                    <span class="text-sm text-muted-foreground flex items-center gap-2">
                        <FileText class="size-4" />
                        Logs
                    </span>
                    <div class="flex items-center gap-2">
                        <span id="log-path" 
                              class="text-xs font-mono text-muted-foreground max-w-[200px] truncate"
                              title={$appState.logFilePath || ''}>
                            {$appState.logFilePath || '-'}
                        </span>
                        <Button 
                            id="open-logs-btn" 
                            variant="outline"
                            size="sm"
                            onclick={() => sendMessage('open-log-folder')}>
                            <FolderOpen class="size-4" />
                            Open
                        </Button>
                    </div>
                </div>
            </div>
        </AccordionContent>
    </AccordionItem>
</Accordion>
