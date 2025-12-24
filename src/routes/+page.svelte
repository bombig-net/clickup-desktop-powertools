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

<div class="bg-background relative z-10 flex min-h-svh flex-col">
    <main class="flex flex-1 flex-col">
        <section>
            <div class="container-wrapper">
                <div class="container flex flex-col items-center gap-2 py-8 text-center md:py-16 lg:py-20 xl:gap-4">
                    <Header />
                </div>
            </div>
        </section>

        <section>
            <div class="container-wrapper">
                <div class="container flex flex-col gap-6 py-8 md:py-12">
                    <ToolsList />
                    <ApiSettings />
                    <SystemSettings />
                    <Diagnostics />
                </div>
            </div>
        </section>
    </main>

    <footer class="text-center py-6 mt-auto">
        <div class="container-wrapper">
            <div class="container">
                <p class="text-xs text-muted-foreground">PowerTools is experimental and intended for power users.</p>
            </div>
        </div>
    </footer>
    
    <Toast message={toastMessage} type={toastType} show={showToast} />
</div>

