<script lang="ts">
    import { appState } from '$lib/state';
    import { sendMessage } from '$lib/bridge';
    import { Accordion, AccordionItem, AccordionTrigger, AccordionContent } from '$lib/components/ui/accordion';
    import CustomCssJsTool from './tools/CustomCssJsTool.svelte';
    import TimeTrackingTool from './tools/TimeTrackingTool.svelte';
    import { Switch } from '$lib/components/ui/switch';
    import type { Tool } from '$lib/state';
    import FileCode from '@lucide/svelte/icons/file-code';
    import Timer from '@lucide/svelte/icons/timer';
    import Power from '@lucide/svelte/icons/power';

    let accordionValue: string | undefined = undefined;

    function handleToolToggle(toolId: string, enabled: boolean): void {
        sendMessage('set-tool-enabled', { toolId, enabled });
    }

    // Get icon for tool based on tool ID
    function getToolIcon(toolId: string) {
        if (toolId === 'custom-css-js') return FileCode;
        if (toolId === 'time-tracking') return Timer;
        return null;
    }
</script>

<div id="tools-container">
    <Accordion type="single" bind:value={accordionValue} class="space-y-4">
        {#each $appState.tools || [] as tool (tool.id)}
            <AccordionItem value={tool.id} id="tool-{tool.id}" data-tool-id={tool.id}>
                <AccordionTrigger class="text-lg font-semibold flex items-center gap-2">
                    {#if getToolIcon(tool.id)}
                        {@const ToolIcon = getToolIcon(tool.id)}
                        <ToolIcon class="size-5" />
                    {/if}
                    {tool.name}
                </AccordionTrigger>
                <AccordionContent>
                    <div class="space-y-4 pt-1">
                        <p class="text-sm text-muted-foreground">{tool.description}</p>
                        
                        {#if tool.id === 'custom-css-js'}
                            <CustomCssJsTool {tool} />
                        {:else if tool.id === 'time-tracking'}
                            <TimeTrackingTool {tool} />
                        {:else}
                            <!-- Generic tool enable toggle -->
                            <div class="flex items-center gap-3 py-3">
                                <span class="flex-1 text-sm text-muted-foreground flex items-center gap-2">
                                    <Power class="size-4" />
                                    Enable Tool
                                </span>
                                <Switch 
                                    checked={tool.enabled}
                                    semantic="positive"
                                    onclick={() => handleToolToggle(tool.id, !tool.enabled)} />
                            </div>
                        {/if}
                    </div>
                </AccordionContent>
            </AccordionItem>
        {/each}
    </Accordion>
</div>
