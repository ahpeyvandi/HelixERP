import { cn } from '../lib/cn';

interface BrandMarkProps {
  className?: string;
}

export function BrandMark({ className }: BrandMarkProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center justify-center rounded-2xl bg-gradient-to-br from-brand-500 via-brand-600 to-indigo-600 text-white shadow-glow',
        className
      )}
      aria-hidden="true"
    >
      <svg viewBox="0 0 24 24" fill="none" className="h-[55%] w-[55%]">
        <path
          d="M6 3.5c6.5 0 5.5 17 12 17"
          stroke="currentColor"
          strokeWidth="2.1"
          strokeLinecap="round"
        />
        <path
          d="M18 3.5c-6.5 0-5.5 17-12 17"
          stroke="currentColor"
          strokeWidth="2.1"
          strokeLinecap="round"
          opacity="0.65"
        />
        <circle cx="6" cy="3.5" r="1.4" fill="currentColor" />
        <circle cx="18" cy="20.5" r="1.4" fill="currentColor" opacity="0.65" />
      </svg>
    </span>
  );
}