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
    import { getStatusDotClass } from '$lib/utils';
    import type { Tool } from '$lib/state';
    import Power from '@lucide/svelte/icons/power';
    import Save from '@lucide/svelte/icons/save';
    import FileCode from '@lucide/svelte/icons/file-code';
    import Code from '@lucide/svelte/icons/code';

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
<div class="py-3">
    <div class="flex items-center gap-3">
        <span class="flex-1 text-sm text-muted-foreground flex items-center gap-2">
            <Power class="size-4" />
            Enable Tool
        </span>
        <Switch 
            checked={currentTool.enabled}
            semantic="positive"
            onclick={() => handleToolToggle(!currentTool.enabled)} />
    </div>
</div>

<div class="space-y-4">
    <div>
        <Label for="css-input-custom-css-js" class="block mb-2 flex items-center gap-2">
            <FileCode class="size-4" />
            Custom CSS
        </Label>
        <Textarea 
            id="css-input-custom-css-js"
            bind:value={cssContent}
            placeholder="/* Enter custom CSS here */"
            rows={5}
            disabled={!currentTool.enabled}
            class="w-full font-mono resize-y min-h-[100px]"
            oninput={handleCssInput} />
    </div>

    <div>
        <Label for="js-input-custom-css-js" class="block mb-2 flex items-center gap-2">
            <Code class="size-4" />
            Custom JavaScript
        </Label>
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
        <p class="text-xs text-muted-foreground">Enable tool to edit and apply custom CSS/JS</p>
    {/if}

    <!-- Footer with Save button and status -->
    <div class="flex justify-between items-center gap-4 pt-2">
    <Button 
        variant="outline"
        disabled={!currentTool.enabled}
        onclick={handleSave}>
        <Save class="size-4" />
        Save & Apply
    </Button>
    
    {#if showStatus && currentTool.enabled}
        <div class="flex items-center gap-2">
            <span class="text-sm text-muted-foreground">Last Injection</span>
            <Badge 
                id="css-js-status-custom-css-js"
                variant="outline"
                class="flex items-center gap-1.5">
                {#if lastError}
                    <span class={getStatusDotClass('invalid')}></span>
                    Error
                {:else if lastResult}
                    <span class={getStatusDotClass('valid')}></span>
                    {lastResult}
                {:else}
                    <span class={getStatusDotClass('none')}></span>
                    -
                {/if}
            </Badge>
        </div>
    {/if}
    </div>
</div>

{#if showStatus && lastError && currentTool.enabled}
    <Alert 
        id="css-js-error-custom-css-js" 
        variant="destructive"
        className="mt-2">
        {lastError}
    </Alert>
{/if}

