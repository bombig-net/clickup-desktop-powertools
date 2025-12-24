<script lang="ts">
  import { cn } from '$lib/utils';
  import { fade } from 'svelte/transition';
  import Info from '@lucide/svelte/icons/info';
  import XCircle from '@lucide/svelte/icons/x-circle';
  import CheckCircle from '@lucide/svelte/icons/check-circle';
  import AlertTriangle from '@lucide/svelte/icons/alert-triangle';
  
  type ToastType = 'info' | 'error' | 'success' | 'warning';
  
  export let show = false;
  export let message = '';
  export let type: ToastType = 'info';
  export let duration = 3000;
  export let className = '';

  function getToastIcon(toastType: ToastType) {
    if (toastType === 'info') return Info;
    if (toastType === 'error') return XCircle;
    if (toastType === 'success') return CheckCircle;
    if (toastType === 'warning') return AlertTriangle;
    return Info;
  }
  
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
  {@const ToastIcon = getToastIcon(type)}
  <div
    class={cn(
      'fixed top-5 right-5 z-50 px-4 py-3 rounded-lg shadow-lg transition-opacity flex items-center gap-2',
      typeStyles[type] || typeStyles.info,
      className
    )}
    transition:fade={{ duration: 300 }}
  >
    <ToastIcon class="size-4 shrink-0" />
    {message}
  </div>
{/if}

