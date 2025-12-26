<script lang="ts">
    import { sendMessage } from '$lib/bridge';
    import { Switch } from '$lib/components/ui/switch';
    import { Button } from '$lib/components/ui/button';
    import type { Tool } from '$lib/state';
    import Power from '@lucide/svelte/icons/power';
    import Play from '@lucide/svelte/icons/play';

    export let tool: Tool;

    function handleToolToggle(enabled: boolean): void {
        sendMessage('set-tool-enabled', { toolId: tool.id, enabled });
    }

    function handleTestOverlay(): void {
        sendMessage('test-time-tracking-overlay');
    }
</script>

<!-- Enable/disable toggle -->
<div class="py-3">
    <div class="flex items-center gap-3">
        <span class="flex-1 text-sm text-muted-foreground flex items-center gap-2">
            <Power class="size-4" />
            Enable Tool
        </span>
        <Switch 
            checked={tool.enabled}
            semantic="positive"
            onclick={() => handleToolToggle(!tool.enabled)} />
    </div>
</div>

<!-- Test Overlay Button -->
<div class="pt-2">
    <Button 
        variant="outline"
        disabled={!tool.enabled}
        onclick={handleTestOverlay}>
        <Play class="size-4" />
        Test Overlay
    </Button>
    <p class="text-xs text-muted-foreground mt-2">
        Shows the overlay with placeholder data. The overlay will appear on the right edge of your screen.
    </p>
</div>

