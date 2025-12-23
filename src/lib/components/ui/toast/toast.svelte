<script lang="ts">
  import { cn } from '$lib/utils';
  import { fade } from 'svelte/transition';
  
  type ToastType = 'info' | 'error' | 'success' | 'warning';
  
  export let show = false;
  export let message = '';
  export let type: ToastType = 'info';
  export let duration = 3000;
  export let className = '';
  
  let timeout: ReturnType<typeof setTimeout> | null = null;
  
  $: if (show) {
    if (timeout) clearTimeout(timeout);
    timeout = setTimeout(() => {
      show = false;
    }, duration);
  }
  
  $: if (!show && timeout) {
    clearTimeout(timeout);
    timeout = null;
  }
  
  const typeStyles: Record<ToastType, string> = {
    info: 'bg-primary text-primary-foreground',
    error: 'bg-destructive text-destructive-foreground',
    success: 'bg-green-500 text-white',
    warning: 'bg-yellow-500 text-black',
  };
</script>

{#if show}
  <div
    class={cn(
      'fixed top-5 right-5 z-50 px-4 py-3 rounded-lg shadow-lg transition-opacity',
      typeStyles[type] || typeStyles.info,
      className
    )}
    transition:fade={{ duration: 300 }}
  >
    {message}
  </div>
{/if}

