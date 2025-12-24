import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Get semantic color class for status variants.
 * Uses shadcn design system colors for consistency.
 */
export function getStatusColorClass(variant: 'valid' | 'invalid' | 'untested' | 'none' | 'info'): string {
  switch (variant) {
    case 'valid':
      return 'text-success'; // Success state - green
    case 'invalid':
      return 'text-destructive'; // Error state - red
    case 'untested':
      return 'text-warning'; // Warning/unknown state - orange
    case 'info':
      return 'text-muted-foreground'; // Informational - gray
    case 'none':
    default:
      return 'text-muted-foreground'; // Neutral/default - gray
  }
}

/**
 * Get Tailwind classes for colored status dot indicators.
 * Returns classes for a small colored circle (1.5px) based on status variant.
 */
export function getStatusDotClass(variant: 'valid' | 'invalid' | 'untested' | 'none' | 'info'): string {
  switch (variant) {
    case 'valid':
      return 'w-1.5 h-1.5 rounded-full bg-green-500';
    case 'invalid':
      return 'w-1.5 h-1.5 rounded-full bg-red-500';
    case 'untested':
      return 'w-1.5 h-1.5 rounded-full bg-orange-500';
    case 'info':
    case 'none':
    default:
      return 'w-1.5 h-1.5 rounded-full bg-muted-foreground';
  }
}

