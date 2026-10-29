import type { AuthUser } from '../../lib/auth/types';
import { BellIcon, MenuIcon, MoonIcon, SearchIcon, SunIcon } from '../icons';
import { UserMenu } from './UserMenu';

interface HeaderProps {
  title: string;
  subtitle: string;
  isDark: boolean;
  user: AuthUser;
  onOpenSidebar: () => void;
  onToggleTheme: () => void;
  onLogout: () => Promise<void> | void;
}

export function Header({
  title,
  subtitle,
  isDark,
  user,
  onOpenSidebar,
  onToggleTheme,
  onLogout
}: HeaderProps) {
  return (
    <header className="sticky top-0 z-20 border-b border-slate-200/70 bg-white/80 backdrop-blur-xl dark:border-white/10 dark:bg-ink-950/80">
      <div className="flex h-16 items-center gap-3 px-4 sm:px-6">
        <button
          type="button"
          onClick={onOpenSidebar}
          className="rounded-xl p-2 text-slate-600 transition hover:bg-slate-100 hover:text-slate-900 dark:text-slate-300 dark:hover:bg-white/5 dark:hover:text-white lg:hidden"
          aria-label="باز کردن منو"
        >
          <MenuIcon className="h-5 w-5" />
        </button>

        <div className="min-w-0">
          <p className="hidden text-[11px] text-slate-400 dark:text-slate-500 sm:block">
            HelixERP / {subtitle}
          </p>
          <h1 className="truncate text-base font-semibold text-slate-900 dark:text-white sm:text-lg">
            {title}
          </h1>
        </div>

        <div className="mx-auto hidden w-full max-w-md md:block">
          <label className="group relative flex items-center">
            <SearchIcon className="pointer-events-none absolute start-3 h-4 w-4 text-slate-400" />
            <input
              type="search"
              placeholder="جست‌وجوی سریع…"
              className="h-10 w-full rounded-2xl border border-slate-200/80 bg-slate-50/80 ps-9 pe-16 text-sm text-slate-700 outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:bg-white focus:ring-4 focus:ring-brand-500/10 dark:border-white/10 dark:bg-white/5 dark:text-slate-200 dark:placeholder:text-slate-500 dark:focus:border-brand-500 dark:focus:bg-white/10"
            />
            <kbd className="absolute end-2.5 rounded-lg border border-slate-200 bg-white px-1.5 py-0.5 text-[10px] text-slate-400 dark:border-white/10 dark:bg-white/5 dark:text-slate-500">
              Ctrl K
            </kbd>
          </label>
        </div>

        <div className="ms-auto flex items-center gap-1.5">
          <button
            type="button"
            onClick={onToggleTheme}
            className="rounded-xl p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-white/5 dark:hover:text-white"
            aria-label={isDark ? 'روشن کردن تم' : 'تاریک کردن تم'}
          >
            {isDark ? <SunIcon className="h-5 w-5" /> : <MoonIcon className="h-5 w-5" />}
          </button>

          <button
            type="button"
            className="relative rounded-xl p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-white/5 dark:hover:text-white"
            aria-label="اعلان‌ها"
          >
            <BellIcon className="h-5 w-5" />
            <span className="absolute end-1.5 top-1.5 h-2 w-2 rounded-full bg-rose-500 ring-2 ring-white dark:ring-ink-950" />
          </button>

          <span className="mx-1 hidden h-8 w-px bg-slate-200 dark:bg-white/10 sm:block" />

          <UserMenu user={user} onLogout={onLogout} />
        </div>
      </div>
    </header>
  );
}