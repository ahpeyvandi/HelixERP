import { BrandMark } from '../BrandMark';

export function SplashScreen() {
  return (
    <div
      role="status"
      aria-live="polite"
      className="flex min-h-screen flex-col items-center justify-center gap-4 bg-slate-50 text-slate-900 dark:bg-ink-950 dark:text-slate-100"
    >
      <BrandMark className="h-14 w-14 animate-pulse" />
      <div className="flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400">
        <span
          className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-slate-300 border-t-brand-600 dark:border-white/15 dark:border-t-brand-400"
          aria-hidden="true"
        />
        در حال بازیابی نشست…
      </div>
    </div>
  );
}