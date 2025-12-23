<script lang="ts">
    import { appState } from '$lib/state';
    import { sendMessage } from '$lib/bridge';
    import { getClickUpStatus } from '$lib/status';
    import { Accordion, AccordionItem, AccordionTrigger, AccordionContent } from '$lib/components/ui/accordion';
    import { Button } from '$lib/components/ui/button';
    import { getStatusColorClass } from '$lib/utils';

    function handleRefreshRuntimeStatus(): void {
        sendMessage('refresh-runtime-status');
        if ($appState.hasApiToken) {
            sendMessage('test-api-token');
        }
    }

    // Reactive status computation
    $: clickUpStatus = getClickUpStatus($appState);
    let accordionValue: string | undefined = undefined;
</script>

<Accordion type="single" bind:value={accordionValue}>
    <AccordionItem value="diagnostics">
        <AccordionTrigger class="font-semibold">
            Status & Diagnostics
        </AccordionTrigger>
        <AccordionContent>
            <div class="flex justify-between items-center py-2 border-b border-border">
                <span class="text-sm text-muted-foreground">Version</span>
                <span id="version" class="text-sm font-medium">{$appState.version || '1.0.0'}</span>
            </div>
            <div class="flex justify-between items-center py-2 border-b border-border">
                <span class="text-sm text-muted-foreground">.NET Runtime</span>
                <span id="dotnet-version" class="text-sm font-medium">{$appState.dotNetVersion || '-'}</span>
            </div>
            <div class="flex justify-between items-center py-2 border-b border-border">
                <span class="text-sm text-muted-foreground">WebView2</span>
                <span id="webview2-version" class="text-sm font-medium">{$appState.webView2Version || '-'}</span>
            </div>
            <div class="flex justify-between items-center py-2 border-b border-border">
                <span class="text-sm text-muted-foreground">ClickUp Desktop</span>
                <div class="flex items-center gap-2">
                    <span id="clickup-desktop-status" 
                          class="text-sm font-medium {getStatusColorClass(clickUpStatus.variant)}">
                        {clickUpStatus.text}
                    </span>
                    <Button 
                        id="refresh-runtime-btn" 
                        variant="outline"
                        size="sm"
                        onclick={handleRefreshRuntimeStatus}>
                        Refresh
                    </Button>
                </div>
            </div>
            <div class="flex justify-between items-center py-2">
                <span class="text-sm text-muted-foreground">Logs</span>
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
                        Open
                    </Button>
                </div>
            </div>
        </AccordionContent>
    </AccordionItem>
</Accordion>

