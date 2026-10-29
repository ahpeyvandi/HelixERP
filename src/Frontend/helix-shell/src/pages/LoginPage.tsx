import { useState, type FormEvent, type KeyboardEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { BrandMark } from '../components/BrandMark';
import {
  AlertIcon,
  ArrowLeftIcon,
  EyeIcon,
  EyeOffIcon,
  KeyIcon,
  LockIcon,
  MoonIcon,
  SunIcon,
  UserIcon
} from '../components/icons';
import { ApiError } from '../lib/auth/authApi';

interface LoginPageProps {
  isDark: boolean;
  onToggleTheme: () => void;
}

function describeError(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.status) {
      case 0:
        return 'ارتباط با سرور برقرار نشد. از اجرای HelixERP Shell مطمئن شوید.';
      case 400:
        return 'اطلاعات ارسال‌شده نامعتبر است. ورودی‌ها را بررسی کنید.';
      case 401:
        return 'نام کاربری یا رمز عبور نادرست است.';
      case 403:
        return 'این حساب اجازه ورود به پوسته را ندارد.';
      default:
        return `خطای سرور (${error.status}). لطفاً چند لحظه بعد دوباره تلاش کنید.`;
    }
  }

  return 'خطای پیش‌بینی‌نشده‌ای رخ داد. دوباره تلاش کنید.';
}

export function LoginPage({ isDark, onToggleTheme }: LoginPageProps) {
  const { login } = useAuth();
  const [loginName, setLoginName] = useState('');
  const [password, setPassword] = useState('');
  const [remember, setRemember] = useState(true);
  const [showPassword, setShowPassword] = useState(false);
  const [capsLock, setCapsLock] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submitting) {
      return;
    }

    const normalizedLogin = loginName.trim();
    if (!normalizedLogin || !password) {
      setError('نام کاربری و رمز عبور را وارد کنید.');
      return;
    }

    setSubmitting(true);
    setError(null);
    setNotice(null);

    try {
      await login({ login: normalizedLogin, password, remember });
    } catch (caught) {
      setError(describeError(caught));
      setSubmitting(false);
    }
  }

  function handlePasswordKey(event: KeyboardEvent<HTMLInputElement>) {
    setCapsLock(event.getModifierState('CapsLock'));
  }

  return (
    <div className="relative flex min-h-screen flex-col items-center justify-center overflow-hidden bg-slate-50 px-4 py-12 text-slate-900 dark:bg-ink-950 dark:text-slate-100">
      <div aria-hidden="true" className="pointer-events-none absolute inset-0">
        <div className="absolute -top-44 start-1/4 h-96 w-96 rounded-full bg-brand-500/15 blur-3xl" />
        <div className="absolute -bottom-44 end-1/4 h-96 w-96 rounded-full bg-indigo-500/10 blur-3xl" />
        <div
          className="absolute inset-0 opacity-40 dark:opacity-20"
          style={{
            backgroundImage:
              'linear-gradient(to right, rgb(148 163 184 / 0.16) 1px, transparent 1px), linear-gradient(to bottom, rgb(148 163 184 / 0.16) 1px, transparent 1px)',
            backgroundSize: '46px 46px',
            maskImage: 'radial-gradient(ellipse at center, black 10%, transparent 72%)',
            WebkitMaskImage: 'radial-gradient(ellipse at center, black 10%, transparent 72%)'
          }}
        />
      </div>

      <button
        type="button"
        onClick={onToggleTheme}
        className="absolute end-4 top-4 z-10 rounded-xl border border-slate-200/80 bg-white/70 p-2.5 text-slate-500 shadow-sm backdrop-blur transition hover:bg-white hover:text-slate-900 focus:outline-none focus-visible:ring-4 focus-visible:ring-brand-500/20 dark:border-white/10 dark:bg-white/5 dark:text-slate-400 dark:hover:bg-white/10 dark:hover:text-white sm:end-6 sm:top-6"
        aria-label={isDark ? 'روشن کردن تم' : 'تاریک کردن تم'}
      >
        {isDark ? <SunIcon className="h-5 w-5" /> : <MoonIcon className="h-5 w-5" />}
      </button>

      <div className="relative w-full max-w-[26.5rem]">
        <div className="rounded-[30px] border border-slate-200/80 bg-white/95 p-7 shadow-soft backdrop-blur-xl dark:border-white/10 dark:bg-ink-900/90 sm:p-9">
          <div className="flex flex-col items-center text-center">
            <BrandMark className="h-14 w-14" />
            <p className="mt-4 font-display text-xl font-semibold tracking-tight text-slate-900 dark:text-white">
              HelixERP
            </p>
            <p className="mt-1 text-[10px] font-medium uppercase tracking-[0.3em] text-slate-400 dark:text-slate-500">
              Enterprise Resource Planning
            </p>
          </div>

          <div className="mt-7 border-t border-slate-100 pt-6 dark:border-white/5">
            <h1 className="text-lg font-semibold text-slate-900 dark:text-white">
              ورود به حساب کاربری
            </h1>
            <p className="mt-1.5 text-xs leading-6 text-slate-500 dark:text-slate-400">
              برای ادامه، نام کاربری و رمز عبور خود را وارد کنید.
            </p>
          </div>

          <form className="mt-5 space-y-4" onSubmit={handleSubmit} noValidate>
            <div className="space-y-1.5">
              <label
                htmlFor="login"
                className="block text-xs font-medium text-slate-700 dark:text-slate-300"
              >
                نام کاربری
              </label>
              <div className="relative">
                <UserIcon className="pointer-events-none absolute start-3.5 top-1/2 h-[18px] w-[18px] -translate-y-1/2 text-slate-400 dark:text-slate-500" />
                <input
                  id="login"
                  name="login"
                  type="text"
                  dir="ltr"
                  autoComplete="username"
                  autoFocus
                  disabled={submitting}
                  value={loginName}
                  onChange={(event) => setLoginName(event.target.value)}
                  placeholder="admin"
                  className="h-12 w-full rounded-2xl border border-slate-200 bg-slate-50/70 ps-11 pe-4 text-sm text-slate-800 outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:bg-white focus:ring-4 focus:ring-brand-500/15 disabled:opacity-60 dark:border-white/10 dark:bg-white/5 dark:text-slate-100 dark:placeholder:text-slate-600 dark:focus:border-brand-500 dark:focus:bg-white/10"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <label
                htmlFor="password"
                className="block text-xs font-medium text-slate-700 dark:text-slate-300"
              >
                رمز عبور
              </label>
              <div className="relative">
                <LockIcon className="pointer-events-none absolute start-3.5 top-1/2 h-[18px] w-[18px] -translate-y-1/2 text-slate-400 dark:text-slate-500" />
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  dir="ltr"
                  autoComplete="current-password"
                  disabled={submitting}
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  onKeyDown={handlePasswordKey}
                  onKeyUp={handlePasswordKey}
                  placeholder="••••••••"
                  className="h-12 w-full rounded-2xl border border-slate-200 bg-slate-50/70 ps-11 pe-12 text-sm text-slate-800 outline-none transition placeholder:text-slate-400 focus:border-brand-400 focus:bg-white focus:ring-4 focus:ring-brand-500/15 disabled:opacity-60 dark:border-white/10 dark:bg-white/5 dark:text-slate-100 dark:placeholder:text-slate-600 dark:focus:border-brand-500 dark:focus:bg-white/10"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((value) => !value)}
                  className="absolute end-2 top-1/2 -translate-y-1/2 rounded-xl p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-700 focus:outline-none focus-visible:ring-4 focus-visible:ring-brand-500/20 dark:text-slate-500 dark:hover:bg-white/10 dark:hover:text-slate-200"
                  aria-label={showPassword ? 'پنهان کردن رمز عبور' : 'نمایش رمز عبور'}
                  aria-pressed={showPassword}
                >
                  {showPassword ? (
                    <EyeOffIcon className="h-[18px] w-[18px]" />
                  ) : (
                    <EyeIcon className="h-[18px] w-[18px]" />
                  )}
                </button>
              </div>
              {capsLock && (
                <p className="flex items-center gap-1.5 pt-0.5 text-[11px] text-amber-600 dark:text-amber-400">
                  <KeyIcon className="h-3.5 w-3.5" />
                  Caps Lock روشن است.
                </p>
              )}
            </div>

            <div className="flex items-center justify-between gap-3 pt-0.5">
              <label className="inline-flex cursor-pointer select-none items-center gap-2 text-xs text-slate-600 dark:text-slate-300">
                <input
                  type="checkbox"
                  checked={remember}
                  onChange={(event) => setRemember(event.target.checked)}
                  className="h-4 w-4 rounded border-slate-300 accent-brand-600 focus:ring-brand-500/30 dark:border-white/20 dark:bg-white/5"
                />
                مرا به خاطر بسپار
              </label>
              <button
                type="button"
                onClick={() => setNotice('بازیابی رمز عبور در فاز بعدی فعال می‌شود.')}
                className="text-xs font-medium text-brand-600 transition hover:text-brand-700 focus:outline-none focus-visible:underline dark:text-brand-300 dark:hover:text-brand-200"
              >
                رمز عبور را فراموش کرده‌اید؟
              </button>
            </div>

            {error && (
              <div
                role="alert"
                className="flex items-start gap-2.5 rounded-2xl border border-rose-200 bg-rose-50 px-3.5 py-3 text-xs leading-6 text-rose-700 dark:border-rose-500/30 dark:bg-rose-500/10 dark:text-rose-300"
              >
                <AlertIcon className="mt-0.5 h-4 w-4 shrink-0" />
                <span>{error}</span>
              </div>
            )}

            {notice && !error && (
              <p className="rounded-2xl border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-[11px] leading-5 text-slate-500 dark:border-white/10 dark:bg-white/5 dark:text-slate-400">
                {notice}
              </p>
            )}

            <button
              type="submit"
              disabled={submitting}
              className="group flex h-12 w-full items-center justify-center gap-2 rounded-2xl bg-brand-600 text-sm font-semibold text-white shadow-glow transition hover:bg-brand-700 focus:outline-none focus-visible:ring-4 focus-visible:ring-brand-500/30 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {submitting ? (
                <>
                  <span
                    className="h-4 w-4 animate-spin rounded-full border-2 border-white/40 border-t-white"
                    aria-hidden="true"
                  />
                  در حال ورود…
                </>
              ) : (
                <>
                  ورود
                  <ArrowLeftIcon className="h-[18px] w-[18px] transition-transform group-hover:-translate-x-0.5" />
                </>
              )}
            </button>
          </form>

          <div className="mt-6 flex items-center justify-between gap-2 border-t border-slate-100 pt-4 text-[11px] text-slate-400 dark:border-white/5 dark:text-slate-500">
            <span>نسخه ۰.۱.۰ · Phase 2</span>
            <span className="inline-flex items-center gap-1.5">
              <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" />
              JWT + Refresh Token
            </span>
          </div>
        </div>

        <p className="mt-6 text-center text-[11px] text-slate-400 dark:text-slate-500">
          HelixERP © 2026 — معماری ماژولار الهام‌گرفته از Odoo
        </p>
      </div>
    </div>
  );
}