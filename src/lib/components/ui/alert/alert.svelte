<script lang="ts">
  import { cn } from '$lib/utils';
  
  type AlertVariant = 'default' | 'destructive';
  
  export let variant: AlertVariant = 'default';
  export let className = '';
  export let id = '';
  
  const variants: Record<AlertVariant, string> = {
    default: 'bg-background text-foreground',
    destructive: 'border-destructive/50 text-destructive dark:border-destructive [&>svg]:text-destructive',
  };
</script>

<div
  {id}
  role="alert"
  class={cn(
    'relative w-full rounded-lg border p-4 [&>svg~*]:pl-7 [&>svg+div]:translate-y-[-3px] [&>svg]:absolute [&>svg]:left-4 [&>svg]:top-4 [&>svg]:text-foreground',
    variants[variant],
    className
  )}
>
  <slot />
</div>

