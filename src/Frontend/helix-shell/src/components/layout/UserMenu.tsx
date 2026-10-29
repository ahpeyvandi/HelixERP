import { useEffect, useRef, useState } from 'react';
import type { AuthUser } from '../../lib/auth/types';
import { ChevronDownIcon, LogoutIcon, ShieldIcon } from '../icons';

interface UserMenuProps {
  user: AuthUser;
  onLogout: () => Promise<void> | void;
}

export function UserMenu({ user, onLogout }: UserMenuProps) {
  const [open, setOpen] = useState(false);
  const [pending, setPending] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    function handlePointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpen(false);
      }
    }

    document.addEventListener('mousedown', handlePointerDown);
    document.addEventListener('keydown', handleKeyDown);

    return () => {
      document.removeEventListener('mousedown', handlePointerDown);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [open]);

  const initial = (user.displayName.trim().charAt(0) || user.login.charAt(0) || 'H').toUpperCase();
  const visibleGroups = user.groups.slice(0, 4);
  const hiddenGroups = user.groups.length - visibleGroups.length;

  async function handleLogout() {
    if (pending) {
      return;
    }

    setPending(true);
    setOpen(false);

    try {
      await onLogout();
    } finally {
      setPending(false);
    }
  }

  return (
    <div ref={rootRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-haspopup="menu"
        aria-expanded={open}
        className="flex items-center gap-2.5 rounded-2xl p-1.5 text-start transition hover:bg-slate-100 focus:outline-none focus-visible:ring-4 focus-visible:ring-brand-500/20 dark:hover:bg-white/5"
      >
        <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-indigo-600 text-sm font-semibold text-white">
          {initial}
        </span>
        <span className="hidden leading-tight sm:block">
          <span className="block max-w-[10rem] truncate text-xs font-semibold text-slate-800 dark:text-slate-100">
            {user.displayName}
          </span>
          <span className="block text-[11px] text-slate-400 dark:text-slate-500" dir="ltr">
            {user.login}
          </span>
        </span>
        <ChevronDownIcon
          className={`hidden h-4 w-4 text-slate-400 transition-transform sm:block ${open ? 'rotate-180' : ''}`}
        />
      </button>

      {open && (
        <div
          role="menu"
          className="absolute end-0 top-full z-30 mt-2 w-72 overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-soft dark:border-white/10 dark:bg-ink-900"
        >
          <div className="flex items-start gap-3 border-b border-slate-100 px-4 py-3.5 dark:border-white/5">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-indigo-600 text-sm font-semibold text-white">
              {initial}
            </span>
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold text-slate-900 dark:text-white">
                {user.displayName}
              </p>
              <p className="truncate text-[11px] text-slate-400 dark:text-slate-500" dir="ltr">
                {user.login}
              </p>
              {user.isSuperUser && (
                <span className="mt-1.5 inline-flex items-center gap-1 rounded-full bg-brand-50 px-2 py-0.5 text-[10px] font-medium text-brand-700 dark:bg-brand-500/10 dark:text-brand-300">
                  <ShieldIcon className="h-3 w-3" />
                  مدیر پلتفرم
                </span>
              )}
            </div>
          </div>

          {visibleGroups.length > 0 && (
            <div className="border-b border-slate-100 px-4 py-3 dark:border-white/5">
              <p className="mb-2 text-[10px] font-medium uppercase tracking-wider text-slate-400 dark:text-slate-500">
                گروه‌های مؤثر
              </p>
              <div className="flex flex-wrap gap-1.5">
                {visibleGroups.map((group) => (
                  <span
                    key={group}
                    className="rounded-lg bg-slate-100 px-2 py-1 font-mono text-[10px] text-slate-600 dark:bg-white/5 dark:text-slate-300"
                    dir="ltr"
                  >
                    {group}
                  </span>
                ))}
                {hiddenGroups > 0 && (
                  <span className="rounded-lg bg-slate-100 px-2 py-1 text-[10px] text-slate-500 dark:bg-white/5 dark:text-slate-400">
                    +{hiddenGroups}
                  </span>
                )}
              </div>
            </div>
          )}

          <div className="p-2">
            <button
              type="button"
              role="menuitem"
              onClick={handleLogout}
              disabled={pending}
              className="flex w-full items-center gap-2.5 rounded-xl px-3 py-2.5 text-xs font-medium text-rose-600 transition hover:bg-rose-50 focus:outline-none focus-visible:ring-4 focus-visible:ring-rose-500/20 disabled:opacity-60 dark:text-rose-400 dark:hover:bg-rose-500/10"
            >
              {pending ? (
                <span className="h-4 w-4 animate-spin rounded-full border-2 border-rose-300 border-t-rose-600" aria-hidden="true" />
              ) : (
                <LogoutIcon className="h-4 w-4" />
              )}
              {pending ? 'در حال خروج…' : 'خروج از حساب'}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}