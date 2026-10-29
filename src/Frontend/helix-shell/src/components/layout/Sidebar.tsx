import { navSections, type NavKey } from '../../lib/navigation';
import { cn } from '../../lib/cn';
import { CloseIcon, ShieldIcon } from '../icons';

interface SidebarProps {
  activeKey: NavKey;
  open: boolean;
  onSelect: (key: NavKey) => void;
  onClose: () => void;
}

export function Sidebar({ activeKey, open, onSelect, onClose }: SidebarProps) {
  return (
    <>
      <div
        aria-hidden="true"
        onClick={onClose}
        className={cn(
          'fixed inset-0 z-30 bg-slate-950/50 backdrop-blur-sm transition-opacity duration-200 lg:hidden',
          open ? 'opacity-100' : 'pointer-events-none opacity-0'
        )}
      />

      <aside
        data-open={open ? 'true' : 'false'}
        className="sidebar fixed inset-y-0 start-0 z-40 flex w-72 flex-col border-e border-slate-200/70 bg-white/95 px-4 py-5 backdrop-blur-xl dark:border-white/10 dark:bg-ink-950/95"
      >
        <div className="flex items-center justify-between gap-3 px-1">
          <div className="flex items-center gap-3">
            <div className="relative flex h-11 w-11 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-500 via-brand-600 to-indigo-600 text-lg font-bold text-white shadow-glow">
              H
              <span className="absolute -bottom-1 -end-1 h-3.5 w-3.5 rounded-full border-2 border-white bg-emerald-400 dark:border-ink-950" />
            </div>
            <div className="leading-tight">
              <p className="font-display text-lg font-semibold tracking-tight text-slate-900 dark:text-white">
                HelixERP
              </p>
              <p className="text-[11px] text-slate-500 dark:text-slate-400">پوسته یکپارچه سازمان</p>
            </div>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="rounded-xl p-2 text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-white/5 dark:hover:text-white lg:hidden"
            aria-label="بستن منو"
          >
            <CloseIcon className="h-5 w-5" />
          </button>
        </div>

        <nav className="scrollbar-thin mt-6 flex-1 space-y-6 overflow-y-auto pe-1">
          {navSections.map((section) => (
            <div key={section.title}>
              <p className="mb-2 px-3 text-[11px] font-medium uppercase tracking-wider text-slate-400 dark:text-slate-500">
                {section.title}
              </p>
              <div className="space-y-1">
                {section.items.map((item) => {
                  const isActive = item.key === activeKey;
                  const Icon = item.icon;

                  return (
                    <button
                      key={item.key}
                      type="button"
                      onClick={() => {
                        onSelect(item.key);
                        onClose();
                      }}
                      className={cn(
                        'group flex w-full items-center gap-3 rounded-2xl px-3 py-2.5 text-start transition-all duration-150',
                        isActive
                          ? 'bg-brand-600 text-white shadow-glow'
                          : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900 dark:text-slate-300 dark:hover:bg-white/5 dark:hover:text-white'
                      )}
                    >
                      <span
                        className={cn(
                          'flex h-9 w-9 shrink-0 items-center justify-center rounded-xl transition',
                          isActive
                            ? 'bg-white/15 text-white'
                            : 'bg-slate-100 text-slate-500 group-hover:bg-white group-hover:text-brand-600 dark:bg-white/5 dark:text-slate-400 dark:group-hover:bg-white/10 dark:group-hover:text-white'
                        )}
                      >
                        <Icon className="h-5 w-5" />
                      </span>
                      <span className="min-w-0 flex-1">
                        <span className="block truncate text-sm font-medium">{item.label}</span>
                        <span
                          className={cn(
                            'block truncate text-[11px]',
                            isActive ? 'text-white/70' : 'text-slate-400 dark:text-slate-500'
                          )}
                        >
                          {item.caption}
                        </span>
                      </span>
                      {isActive ? <span className="h-1.5 w-1.5 rounded-full bg-white/90" /> : null}
                    </button>
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        <div className="mt-4 rounded-2xl border border-slate-200/70 bg-gradient-to-br from-slate-50 to-white p-3.5 dark:border-white/10 dark:from-white/5 dark:to-transparent">
          <div className="flex items-center gap-2.5">
            <span className="flex h-8 w-8 items-center justify-center rounded-xl bg-emerald-500/10 text-emerald-600 dark:text-emerald-400">
              <ShieldIcon className="h-4.5 w-4.5" />
            </span>
            <div className="min-w-0">
              <p className="text-xs font-semibold text-slate-800 dark:text-slate-100">JWT فعال است</p>
              <p className="truncate text-[11px] text-slate-500 dark:text-slate-400">
                دسترسی‌ها بر پایه گروه‌ها
              </p>
            </div>
          </div>
          <div className="mt-3 flex flex-wrap gap-1.5">
            <span className="rounded-full bg-white px-2 py-0.5 text-[10px] text-slate-500 ring-1 ring-slate-200 dark:bg-white/5 dark:text-slate-400 dark:ring-white/10">
              Phase 1
            </span>
            <span className="rounded-full bg-white px-2 py-0.5 text-[10px] text-slate-500 ring-1 ring-slate-200 dark:bg-white/5 dark:text-slate-400 dark:ring-white/10">
              RTL
            </span>
            <span className="rounded-full bg-white px-2 py-0.5 text-[10px] text-slate-500 ring-1 ring-slate-200 dark:bg-white/5 dark:text-slate-400 dark:ring-white/10">
              YARP
            </span>
          </div>
        </div>
      </aside>
    </>
  );
}