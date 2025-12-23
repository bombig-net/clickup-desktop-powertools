<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { appState, updateState } from '$lib/state';
    import { sendMessage, onMessage, offMessage, init } from '$lib/bridge';
    import Header from '../sections/Header.svelte';
    import ToolsList from '../sections/ToolsList.svelte';
    import ApiSettings from '../sections/ApiSettings.svelte';
    import SystemSettings from '../sections/SystemSettings.svelte';
    import Diagnostics from '../sections/Diagnostics.svelte';
    import { Toast } from '$lib/components/ui/toast';

    let toastMessage = '';
    let toastType: 'info' | 'error' | 'success' | 'warning' = 'info';
    let showToast = false;
    let toastTimeout: ReturnType<typeof setTimeout> | null = null;

    function displayToast(message: string, type: 'info' | 'error' | 'success' | 'warning' = 'info') {
        // Clear any existing timeout
        if (toastTimeout) {
            clearTimeout(toastTimeout);
            toastTimeout = null;
        }
        
        // Set toast state
        toastMessage = message;
        toastType = type;
        showToast = true;
        
        // Auto-hide after 3 seconds
        toastTimeout = setTimeout(() => {
            showToast = false;
            toastTimeout = null;
        }, 3000);
    }

    // Message handlers
    function handleStateChanged(payload: unknown) {
        updateState(payload as Record<string, unknown>);
    }

    function handleLaunchResult(payload: unknown) {
        const result = payload as { success?: boolean; error?: string };
        if (!result.success) {
            displayToast(result.error || 'Failed to launch ClickUp', 'error');
        }
    }

    // Store handler references for cleanup
    const stateChangedHandler = handleStateChanged;
    const launchResultHandler = handleLaunchResult;

    onMount(() => {
        // Register message handlers
        onMessage('state-changed', stateChangedHandler);
        onMessage('launch-result', launchResultHandler);

        // Initialize bridge
        init();
    });

    onDestroy(() => {
        // Unregister all message handlers
        offMessage('state-changed', stateChangedHandler);
        offMessage('launch-result', launchResultHandler);
        
        // Clear toast timeout if component unmounts
        if (toastTimeout) {
            clearTimeout(toastTimeout);
            toastTimeout = null;
        }
    });
</script>

<div class="min-h-screen bg-background">
    <div class="max-w-[600px] mx-auto px-8 py-8 min-h-screen flex flex-col">
        <Header />

        <main class="flex-1">
            <ToolsList />
            <ApiSettings />
            <SystemSettings />
            <Diagnostics />
        </main>

        <footer class="text-center py-6 border-t border-border mt-auto">
            <p class="text-xs text-muted-foreground">PowerTools is experimental and intended for power users.</p>
        </footer>
    </div>
    
    <Toast message={toastMessage} type={toastType} show={showToast} />
</div>

