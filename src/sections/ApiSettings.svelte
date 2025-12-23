<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { appState } from '../lib/state';
    import { sendMessage, onMessage, offMessage } from '../lib/bridge';
    import { getTokenStatus } from '../lib/status';
    import { Accordion, AccordionItem, AccordionTrigger, AccordionContent } from '$lib/components/ui/accordion';
    import { Input } from '$lib/components/ui/input';
    import { Button } from '$lib/components/ui/button';
    import { Alert } from '$lib/components/ui/alert';
    import { Label } from '$lib/components/ui/label';

    let tokenInput = '';
    let userEditingToken = false;
    let testButtonText = 'Test Connection';
    let testButtonDisabled = false;
    let tokenError = '';
    let showTokenError = false;
    let tokenErrorTimeout: ReturnType<typeof setTimeout> | null = null;
    let accordionValue: string | undefined = 'api-settings';

    function handleTestResult(payload: unknown): void {
        const result = payload as { success?: boolean; error?: string };
        testButtonDisabled = false;
        testButtonText = 'Test Connection';
        if (result.success) {
            // Clear any existing timeout
            if (tokenErrorTimeout) {
                clearTimeout(tokenErrorTimeout);
                tokenErrorTimeout = null;
            }
            showTokenError = false;
        } else {
            tokenError = result.error || 'Token test failed';
            showTokenError = true;
            // Clear any existing timeout before setting new one
            if (tokenErrorTimeout) {
                clearTimeout(tokenErrorTimeout);
            }
            tokenErrorTimeout = setTimeout(() => {
                showTokenError = false;
                tokenErrorTimeout = null;
            }, 5000);
        }
    }

    function handleSaveToken(): void {
        if (tokenInput.trim()) {
            sendMessage('set-api-token', { token: tokenInput.trim() });
            tokenInput = '';
            userEditingToken = false;
        }
    }

    function handleTestToken(): void {
        testButtonDisabled = true;
        testButtonText = 'Testing...';
        sendMessage('test-api-token');
    }

    function handleClearToken(): void {
        if (confirm('Are you sure you want to remove the API token?')) {
            sendMessage('clear-api-token');
        }
    }

    function handleTokenInput(): void {
        userEditingToken = tokenInput.length > 0;
    }

    // Update token input placeholder based on state
    $: tokenPlaceholder = $appState.hasApiToken && !userEditingToken 
        ? '••••••••••••••••' 
        : 'Enter your ClickUp API token';

    // Reactive token status
    $: tokenStatus = getTokenStatus($appState);

    // Store handler reference for cleanup
    const testResultHandler = handleTestResult;

    onMount(() => {
        onMessage('test-result', testResultHandler);
    });

    onDestroy(() => {
        offMessage('test-result', testResultHandler);
        
        // Clear token error timeout
        if (tokenErrorTimeout) {
            clearTimeout(tokenErrorTimeout);
            tokenErrorTimeout = null;
        }
    });
</script>

<Accordion type="single" bind:value={accordionValue}>
    <AccordionItem value="api-settings">
        <AccordionTrigger class="font-semibold">
            ClickUp API
        </AccordionTrigger>
        <AccordionContent>
            <p class="pt-4 pb-5 text-sm text-muted-foreground">Configure your personal ClickUp API token to enable PowerTools features.</p>
        
            <div class="mb-5">
                <Label for="token-input" class="block mb-2">API Token</Label>
                <div class="flex gap-2">
                    <Input 
                        type="password"
                        id="token-input"
                        bind:value={tokenInput}
                        placeholder={tokenPlaceholder}
                        autocomplete="off"
                        spellcheck="false"
                        class="flex-1"
                        oninput={handleTokenInput}
                        onkeydown={(e) => {
                            if (e.key === 'Enter') {
                                handleSaveToken();
                            }
                        }} />
                    <Button 
                        id="save-btn" 
                        variant="default"
                        disabled={!tokenInput.trim()}
                        onclick={handleSaveToken}>
                        Save
                    </Button>
                </div>
            </div>

            <div class="flex justify-between items-center py-2 border-b border-border">
                <span class="text-sm text-muted-foreground">Status</span>
                <span id="token-status" 
                      class="text-sm font-medium
                             {tokenStatus.variant === 'valid' ? 'text-green-400' : ''}
                             {tokenStatus.variant === 'invalid' ? 'text-red-400' : ''}
                             {tokenStatus.variant === 'untested' ? 'text-yellow-400' : ''}
                             {tokenStatus.variant === 'none' ? 'text-muted-foreground' : ''}">
                    {tokenStatus.text}
                </span>
            </div>

            {#if showTokenError}
                <Alert id="token-error" variant="destructive" className="mt-4">
                    {tokenError}
                </Alert>
            {/if}

            <div class="flex gap-3 mt-4">
                <Button 
                    id="test-btn" 
                    variant="outline"
                    disabled={!$appState.hasApiToken || testButtonDisabled}
                    onclick={handleTestToken}>
                    {testButtonText}
                </Button>
                <Button 
                    id="clear-btn" 
                    variant="destructive"
                    disabled={!$appState.hasApiToken}
                    onclick={handleClearToken}>
                    Clear Token
                </Button>
            </div>

            <p class="text-xs text-muted-foreground mt-4">
                Get your API token from 
                <a href="https://app.clickup.com/settings/apps" target="_blank" rel="noopener" class="text-primary hover:underline">ClickUp Settings → Apps</a>
            </p>
        </AccordionContent>
    </AccordionItem>
</Accordion>

