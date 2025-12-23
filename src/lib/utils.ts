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
      return 'text-primary'; // Success state
    case 'invalid':
      return 'text-destructive'; // Error state
    case 'untested':
      return 'text-muted-foreground'; // Warning/unknown state
    case 'info':
      return 'text-muted-foreground'; // Informational
    case 'none':
    default:
      return 'text-muted-foreground'; // Neutral/default
  }
}

