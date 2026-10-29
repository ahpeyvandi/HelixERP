import { useState } from 'react';
import { useAuth } from '../../auth/AuthContext';
import { Header } from './Header';
import { Sidebar } from './Sidebar';
import {
  ArrowLeftIcon,
  LayersIcon,
  PlusIcon,
  ShieldIcon,
  SparklesIcon,
  UsersIcon
} from '../icons';
import { findNavItem, type NavKey } from '../../lib/navigation';

const stats = [
  {
    label: 'ماژول‌های ثبت‌شده',
    value: '۰',
    hint: 'آماده برای اتصال',
    icon: LayersIcon,
    tone: 'bg-brand-500/10 text-brand-600 dark:text-brand-300'
  },
  {
    label: 'گروه‌های دسترسی',
    value: '۹',
    hint: 'گروه‌های استاندارد Odoo',
    icon: ShieldIcon,
    tone: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400'
  },
  {
    label: 'کاربران فعال',
    value: '۱',
    hint: 'مدیر سیستم',
    icon: UsersIcon,
    tone: 'bg-violet-500/10 text-violet-600 dark:text-violet-400'
  },
  {
    label: 'وضعیت پوسته',
    value: 'آماده',
    hint: 'RTL + JWT',
    icon: SparklesIcon,
    tone: 'bg-amber-500/10 text-amber-600 dark:text-amber-400'
  }
];

const infrastructure = [
  { label: 'SQL Server لوکال', detail: 'HelixERP' },
  { label: 'Redis', detail: 'AOF فعال' },
  { label: 'YARP Gateway', detail: 'مسیریابی پویا' },
  { label: 'ارپ فایلز', detail: 'volume: erp_files' }
];

interface AppShellProps {
  isDark: boolean;
  onToggleTheme: () => void;
}

export function AppShell({ isDark, onToggleTheme }: AppShellProps) {
  const { user, logout } = useAuth();
  const [activeKey, setActiveKey] = useState<NavKey>('dashboard');
  const [sidebarOpen, setSidebarOpen] = useState(false);

  if (!user) {
    return null;
  }

  const activeItem = findNavItem(activeKey);
  const ActiveIcon = activeItem.icon;

  return (
    <div className="min-h-screen bg-slate-100 text-slate-900 dark:bg-ink-950 dark:text-slate-100">
      <Sidebar
        activeKey={activeKey}
        open={sidebarOpen}
        onSelect={setActiveKey}
        onClose={() => setSidebarOpen(false)}
      />

      <div className="min-h-screen lg:ps-72">
        <Header
          title={activeItem.label}
          subtitle={activeItem.caption}
          isDark={isDark}
          user={user}
          onOpenSidebar={() => setSidebarOpen(true)}
          onToggleTheme={onToggleTheme}
          onLogout={logout}
        />

        <main className="mx-auto w-full max-w-7xl space-y-6 px-4 py-6 sm:px-6 lg:py-8">
          <section className="relative overflow-hidden rounded-3xl border border-slate-200/70 bg-white p-6 shadow-soft dark:border-white/10 dark:bg-ink-900/80 sm:p-8">
            <div className="pointer-events-none absolute -start-20 -top-28 h-72 w-72 rounded-full bg-brand-500/10 blur-3xl" />
            <div className="pointer-events-none absolute -bottom-28 -end-16 h-64 w-64 rounded-full bg-indigo-500/10 blur-3xl" />

            <div className="relative flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
              <div className="max-w-2xl space-y-3">
                <span className="inline-flex items-center gap-2 rounded-full border border-brand-200/70 bg-brand-50 px-3 py-1 text-[11px] font-medium text-brand-700 dark:border-brand-500/30 dark:bg-brand-500/10 dark:text-brand-200">
                  <span className="h-1.5 w-1.5 rounded-full bg-brand-500" />
                  Phase 2 · احراز هویت
                </span>
                <h2 className="font-display text-2xl font-semibold tracking-tight text-slate-900 dark:text-white sm:text-3xl">
                  خوش آمدید، {user.displayName}
                </h2>
                <p className="text-sm leading-7 text-slate-600 dark:text-slate-300">
                  این نسخه شامل YARP، احراز هویت JWT، گروه‌ها و دسترسی‌های Odoo، ثبت ماژول بر
                  اساس IP و پورت و ذخیره‌سازی فایل است. از منوی کناری بخش موردنظر را انتخاب
                  کنید.
                </p>
                <div className="flex flex-wrap items-center gap-2 pt-1">
                  <button
                    type="button"
                    className="inline-flex items-center gap-2 rounded-2xl bg-brand-600 px-4 py-2.5 text-sm font-medium text-white shadow-glow transition hover:bg-brand-700 focus:outline-none focus:ring-4 focus:ring-brand-500/25"
                  >
                    <PlusIcon className="h-4 w-4" />
                    ثبت ماژول جدید
                  </button>
                  <button
                    type="button"
                    className="inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700 transition hover:border-slate-300 hover:bg-slate-50 dark:border-white/10 dark:bg-white/5 dark:text-slate-200 dark:hover:bg-white/10"
                  >
                    مشاهده وضعیت پروژه
                    <ArrowLeftIcon className="h-4 w-4" />
                  </button>
                </div>
              </div>

              <div className="relative grid w-full max-w-sm grid-cols-2 gap-3 lg:w-auto">
                <div className="rounded-2xl border border-slate-200/80 bg-slate-50/80 p-3.5 dark:border-white/10 dark:bg-white/5">
                  <p className="text-[11px] text-slate-500 dark:text-slate-400">قلب سیستم</p>
                  <p className="mt-1 text-lg font-semibold text-slate-900 dark:text-white">YARP</p>
                  <p className="text-[11px] text-slate-400 dark:text-slate-500">داخل Shell</p>
                </div>
                <div className="rounded-2xl border border-slate-200/80 bg-slate-50/80 p-3.5 dark:border-white/10 dark:bg-white/5">
                  <p className="text-[11px] text-slate-500 dark:text-slate-400">احراز هویت</p>
                  <p className="mt-1 text-lg font-semibold text-slate-900 dark:text-white">JWT</p>
                  <p className="text-[11px] text-slate-400 dark:text-slate-500">Refresh Token</p>
                </div>
                <div className="rounded-2xl border border-slate-200/80 bg-slate-50/80 p-3.5 dark:border-white/10 dark:bg-white/5">
                  <p className="text-[11px] text-slate-500 dark:text-slate-400">دسترسی‌ها</p>
                  <p className="mt-1 text-lg font-semibold text-slate-900 dark:text-white">Odoo</p>
                  <p className="text-[11px] text-slate-400 dark:text-slate-500">گروه و قواعد</p>
                </div>
                <div className="rounded-2xl border border-slate-200/80 bg-slate-50/80 p-3.5 dark:border-white/10 dark:bg-white/5">
                  <p className="text-[11px] text-slate-500 dark:text-slate-400">فایل‌ها</p>
                  <p className="mt-1 text-lg font-semibold text-slate-900 dark:text-white">Volume</p>
                  <p className="text-[11px] text-slate-400 dark:text-slate-500">erp_files</p>
                </div>
              </div>
            </div>
          </section>

          <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {stats.map((stat) => {
              const Icon = stat.icon;

              return (
                <div
                  key={stat.label}
                  className="rounded-3xl border border-slate-200/70 bg-white p-5 shadow-soft dark:border-white/10 dark:bg-ink-900/70"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-xs text-slate-500 dark:text-slate-400">{stat.label}</p>
                      <p className="mt-2 text-2xl font-semibold text-slate-900 dark:text-white">
                        {stat.value}
                      </p>
                    </div>
                    <span className={`flex h-10 w-10 items-center justify-center rounded-2xl ${stat.tone}`}>
                      <Icon className="h-5 w-5" />
                    </span>
                  </div>
                  <p className="mt-3 text-[11px] text-slate-400 dark:text-slate-500">{stat.hint}</p>
                </div>
              );
            })}
          </section>

          <section className="grid gap-4 xl:grid-cols-3">
            <div className="flex min-h-[320px] flex-col items-center justify-center rounded-3xl border border-dashed border-slate-300 bg-white/70 p-8 text-center dark:border-white/10 dark:bg-ink-900/40 xl:col-span-2">
              <span className="flex h-16 w-16 items-center justify-center rounded-3xl bg-slate-100 text-slate-400 dark:bg-white/5 dark:text-slate-500">
                <ActiveIcon className="h-7 w-7" />
              </span>
              <h3 className="mt-4 text-lg font-semibold text-slate-900 dark:text-white">
                {activeItem.label} هنوز صفحه‌ای ندارد
              </h3>
              <p className="mt-2 max-w-md text-sm leading-6 text-slate-500 dark:text-slate-400">
                پوسته اصلی در این فاز آماده شده است. پس از ثبت ماژول در رجیستری، مسیر
                <span className="mx-1 rounded-lg bg-slate-100 px-1.5 py-0.5 font-mono text-[11px] text-slate-600 dark:bg-white/10 dark:text-slate-300">
                  /modules/{activeKey}
                </span>
                به‌صورت خودکار توسط YARP به سرویس مقصد هدایت می‌شود.
              </p>
              <button
                type="button"
                className="mt-5 inline-flex items-center gap-2 rounded-2xl border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:border-brand-300 hover:text-brand-700 dark:border-white/10 dark:bg-white/5 dark:text-slate-200 dark:hover:border-brand-500/40 dark:hover:text-white"
              >
                <PlusIcon className="h-4 w-4" />
                افزودن ماژول
              </button>
            </div>

            <div className="rounded-3xl border border-slate-200/70 bg-white p-5 shadow-soft dark:border-white/10 dark:bg-ink-900/70">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold text-slate-900 dark:text-white">وضعیت زیرساخت</h3>
                <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-500/10 px-2.5 py-1 text-[10px] font-medium text-emerald-700 dark:text-emerald-400">
                  <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" />
                  فعال
                </span>
              </div>

              <ul className="mt-4 space-y-3">
                {infrastructure.map((item) => (
                  <li
                    key={item.label}
                    className="flex items-center justify-between gap-3 rounded-2xl border border-slate-100 px-3 py-2.5 dark:border-white/5"
                  >
                    <span className="flex items-center gap-2.5">
                      <span className="flex h-8 w-8 items-center justify-center rounded-xl bg-slate-100 text-slate-500 dark:bg-white/5 dark:text-slate-400">
                        <LayersIcon className="h-4 w-4" />
                      </span>
                      <span className="text-xs font-medium text-slate-700 dark:text-slate-200">
                        {item.label}
                      </span>
                    </span>
                    <span className="text-[11px] text-slate-400 dark:text-slate-500">{item.detail}</span>
                  </li>
                ))}
              </ul>

              <div className="mt-4 rounded-2xl bg-gradient-to-br from-brand-600 to-indigo-700 p-4 text-white shadow-glow">
                <p className="text-[11px] text-white/70">پشتیبانی فارسی</p>
                <p className="mt-1 text-sm font-semibold">RTL کامل + تقویم جلالی در فاز بعد</p>
                <p className="mt-1 text-[11px] text-white/70">
                  فونت پیشنهادی: Vazirmatn — لایه فارسی آماده اتصال است.
                </p>
              </div>
            </div>
          </section>

          <footer className="pb-4 pt-2 text-center text-[11px] text-slate-400 dark:text-slate-500">
            HelixERP © 2026 — Odoo-inspired modular architecture
          </footer>
        </main>
      </div>
    </div>
  );
}