<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { appState } from '$lib/state';
    import { sendMessage, onMessage, offMessage } from '$lib/bridge';
    import { Switch } from '$lib/components/ui/switch';
    import { Textarea } from '$lib/components/ui/textarea';
    import { Button } from '$lib/components/ui/button';
    import { Label } from '$lib/components/ui/label';
    import { Alert } from '$lib/components/ui/alert';
    import { Badge } from '$lib/components/ui/badge';
    import type { Tool } from '$lib/state';

    export let tool: Tool;
    
    // Reactive tool state from global state
    $: currentTool = $appState.tools?.find(t => t.id === tool.id) || tool;

    let cssContent = '';
    let jsContent = '';
    let applyAttempted = false;
    let lastResult: string | null = null;
    let lastError: string | null = null;
    let showStatus = false;
    let cssJsStateTimeout: ReturnType<typeof setTimeout> | null = null;

    function handleSave(): void {
        if (!currentTool.enabled) return;
        applyAttempted = true;
        showStatus = true;
        sendMessage('set-custom-css-js', { css: cssContent, javascript: jsContent });
    }

    function handleCssInput(): void {
        if (applyAttempted) {
            applyAttempted = false;
            showStatus = false;
            lastResult = null;
            lastError = null;
        }
    }

    function handleJsInput(): void {
        if (applyAttempted) {
            applyAttempted = false;
            showStatus = false;
            lastResult = null;
            lastError = null;
        }
    }

    function handleCustomCssJsState(payload: unknown): void {
        const state = payload as { css?: string; js?: string; lastError?: string; lastResult?: string };
        if (state.css !== undefined) {
            cssContent = state.css || '';
        }
        if (state.js !== undefined) {
            jsContent = state.js || '';
        }

        const isEnabled = currentTool.enabled;
        if (!isEnabled || !applyAttempted) {
            showStatus = false;
            lastResult = null;
            lastError = null;
        } else if (state.lastError) {
            showStatus = true;
            lastError = state.lastError;
            lastResult = null;
        } else if (state.lastResult) {
            showStatus = true;
            lastResult = state.lastResult;
            lastError = null;
        } else {
            showStatus = true;
            lastResult = null;
            lastError = null;
        }
    }

    function handleToolToggle(enabled: boolean): void {
        sendMessage('set-tool-enabled', { toolId: currentTool.id, enabled });
        if (enabled) {
            // Clear any existing timeout
            if (cssJsStateTimeout) {
                clearTimeout(cssJsStateTimeout);
            }
            cssJsStateTimeout = setTimeout(() => {
                sendMessage('get-custom-css-js');
                cssJsStateTimeout = null;
            }, 100);
        } else {
            // Clear timeout if disabling
            if (cssJsStateTimeout) {
                clearTimeout(cssJsStateTimeout);
                cssJsStateTimeout = null;
            }
            showStatus = false;
            lastResult = null;
            lastError = null;
        }
    }

    // Store handler reference for cleanup
    const customCssJsStateHandler = handleCustomCssJsState;

    onMount(() => {
        onMessage('custom-css-js-state', customCssJsStateHandler);
        if (currentTool.enabled) {
            sendMessage('get-custom-css-js');
        }
    });

    onDestroy(() => {
        offMessage('custom-css-js-state', customCssJsStateHandler);
        
        // Clear timeout
        if (cssJsStateTimeout) {
            clearTimeout(cssJsStateTimeout);
            cssJsStateTimeout = null;
        }
    });
</script>

<!-- Enable/disable toggle -->
<div class="mt-4 pt-3 pb-4 border-b border-border">
    <div class="flex items-center gap-3">
        <span class="flex-1 text-sm text-muted-foreground">Enable Tool</span>
        <Switch 
            checked={currentTool.enabled}
            onclick={() => handleToolToggle(!currentTool.enabled)} />
    </div>
</div>

<div class="mt-4">
    <Label for="css-input-custom-css-js" class="block mb-2">Custom CSS</Label>
    <Textarea 
        id="css-input-custom-css-js"
        bind:value={cssContent}
        placeholder="/* Enter custom CSS here */"
        rows={5}
        disabled={!currentTool.enabled}
        class="w-full font-mono resize-y min-h-[100px]"
        oninput={handleCssInput} />
</div>

<div class="mt-4">
    <Label for="js-input-custom-css-js" class="block mb-2">Custom JavaScript</Label>
    <Textarea 
        id="js-input-css-js"
        bind:value={jsContent}
        placeholder="// Enter custom JavaScript here"
        rows={5}
        disabled={!currentTool.enabled}
        class="w-full font-mono resize-y min-h-[100px]"
        oninput={handleJsInput} />
</div>

{#if !currentTool.enabled}
    <p class="text-xs text-muted-foreground mt-2">Enable tool to edit and apply custom CSS/JS</p>
{/if}

<!-- Footer with Save button and status -->
<div class="flex justify-between items-center gap-4 mt-4 pt-4 pb-4 border-t border-border">
    <Button 
        variant="outline"
        disabled={!currentTool.enabled}
        onclick={handleSave}>
        Save & Apply
    </Button>
    
    {#if showStatus && currentTool.enabled}
        <div class="flex items-center gap-2">
            <span class="text-sm text-muted-foreground">Last Injection</span>
            <Badge 
                id="css-js-status-custom-css-js"
                variant={lastError ? 'destructive' : (lastResult ? 'default' : 'outline')}>
                {#if lastError}
                    Error
                {:else if lastResult}
                    {lastResult}
                {:else}
                    -
                {/if}
            </Badge>
        </div>
    {/if}
</div>

{#if showStatus && lastError && currentTool.enabled}
    <Alert 
        id="css-js-error-custom-css-js" 
        variant="destructive"
        className="mt-2">
        {lastError}
    </Alert>
{/if}

