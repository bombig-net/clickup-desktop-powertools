<script lang="ts">
    import { onDestroy } from 'svelte';
    import { appState } from '../lib/state';
    import { sendMessage } from '../lib/bridge';
    import { Accordion, AccordionItem, AccordionTrigger, AccordionContent } from '$lib/components/ui/accordion';
    import CustomCssJsTool from '../components/tools/CustomCssJsTool.svelte';
    import DebugInspectorTool from '../components/tools/DebugInspectorTool.svelte';
    import { Switch } from '$lib/components/ui/switch';
    import type { Tool } from '../lib/state';

    let debugInspectorTimeout: ReturnType<typeof setTimeout> | null = null;
    let accordionValue: string | undefined = undefined;

    function handleToolToggle(toolId: string, enabled: boolean): void {
        sendMessage('set-tool-enabled', { toolId, enabled });
    }

    function handleDebugInspectorToggle(enabled: boolean): void {
        handleToolToggle('debug-inspector', enabled);
        if (enabled) {
            // Clear any existing timeout
            if (debugInspectorTimeout) {
                clearTimeout(debugInspectorTimeout);
            }
            debugInspectorTimeout = setTimeout(() => {
                sendMessage('get-debug-inspector-state');
                debugInspectorTimeout = null;
            }, 100);
        }
    }

    onDestroy(() => {
        // Clear debug inspector timeout
        if (debugInspectorTimeout) {
            clearTimeout(debugInspectorTimeout);
            debugInspectorTimeout = null;
        }
    });
</script>

<div id="tools-container" class="mb-6">
    <Accordion type="single" bind:value={accordionValue}>
        {#each $appState.tools || [] as tool (tool.id)}
            <AccordionItem value={tool.id} id="tool-{tool.id}" data-tool-id={tool.id}>
                <AccordionTrigger class="font-semibold">
                    {tool.name}
                </AccordionTrigger>
                <AccordionContent>
                    <p class="pb-5 text-sm text-muted-foreground">{tool.description}</p>
                    
                    {#if tool.id === 'custom-css-js'}
                        <CustomCssJsTool {tool} />
                    {:else if tool.id === 'debug-inspector'}
                        <DebugInspectorTool />
                    {:else}
                        <!-- Generic tool enable toggle -->
                        <div class="flex items-center gap-3 py-3 border-b border-border">
                            <span class="flex-1 text-sm text-muted-foreground">Enable Tool</span>
                            <Switch 
                                checked={tool.enabled}
                                onclick={() => {
                                    if (tool.id === 'debug-inspector') {
                                        handleDebugInspectorToggle(!tool.enabled);
                                    } else {
                                        handleToolToggle(tool.id, !tool.enabled);
                                    }
                                }} />
                        </div>
                    {/if}
                </AccordionContent>
            </AccordionItem>
        {/each}
    </Accordion>
</div>

