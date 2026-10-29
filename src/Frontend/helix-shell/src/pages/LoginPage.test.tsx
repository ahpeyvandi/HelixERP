import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '../lib/auth/authApi';

const mocks = vi.hoisted(() => ({
  login: vi.fn()
}));

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => ({
    status: 'anonymous',
    user: null,
    login: mocks.login,
    logout: vi.fn(),
    refresh: vi.fn(),
    getAccessToken: vi.fn()
  })
}));

import { LoginPage } from './LoginPage';

function renderPage() {
  const onToggleTheme = vi.fn();
  render(<LoginPage isDark={false} onToggleTheme={onToggleTheme} />);
  return { onToggleTheme };
}

describe('LoginPage', () => {
  beforeEach(() => {
    mocks.login.mockReset();
  });

  it('renders the Odoo-inspired login card', () => {
    renderPage();

    expect(screen.getByText('HelixERP')).toBeTruthy();
    expect(screen.getByRole('heading', { name: 'ورود به حساب کاربری' })).toBeTruthy();
    expect(screen.getByLabelText('نام کاربری')).toBeTruthy();
    expect(screen.getByLabelText('رمز عبور')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'ورود' })).toBeTruthy();
  });

  it('validates empty credentials before hitting the API', async () => {
    renderPage();

    await userEvent.click(screen.getByRole('button', { name: 'ورود' }));

    expect(screen.getByRole('alert').textContent).toContain('نام کاربری و رمز عبور را وارد کنید.');
    expect(mocks.login).not.toHaveBeenCalled();
  });

  it('submits trimmed credentials and the remember flag', async () => {
    mocks.login.mockResolvedValue(undefined);
    renderPage();

    await userEvent.type(screen.getByLabelText('نام کاربری'), '  admin  ');
    await userEvent.type(screen.getByLabelText('رمز عبور'), 'Helix@12345');
    await userEvent.click(screen.getByRole('button', { name: 'ورود' }));

    await waitFor(() =>
      expect(mocks.login).toHaveBeenCalledWith({
        login: 'admin',
        password: 'Helix@12345',
        remember: true
      })
    );
  });

  it('passes remember=false when the checkbox is unchecked', async () => {
    mocks.login.mockResolvedValue(undefined);
    renderPage();

    await userEvent.type(screen.getByLabelText('نام کاربری'), 'admin');
    await userEvent.type(screen.getByLabelText('رمز عبور'), 'Helix@12345');
    await userEvent.click(screen.getByLabelText('مرا به خاطر بسپار'));
    await userEvent.click(screen.getByRole('button', { name: 'ورود' }));

    await waitFor(() =>
      expect(mocks.login).toHaveBeenCalledWith({
        login: 'admin',
        password: 'Helix@12345',
        remember: false
      })
    );
  });

  it('shows a Persian message for invalid credentials', async () => {
    mocks.login.mockRejectedValue(new ApiError(401, 'Invalid credentials'));
    renderPage();

    await userEvent.type(screen.getByLabelText('نام کاربری'), 'admin');
    await userEvent.type(screen.getByLabelText('رمز عبور'), 'wrong-password');
    await userEvent.click(screen.getByRole('button', { name: 'ورود' }));

    await waitFor(() =>
      expect(screen.getByRole('alert').textContent).toContain(
        'نام کاربری یا رمز عبور نادرست است.'
      )
    );
    expect(screen.getByRole('button', { name: 'ورود' })).not.toHaveProperty('disabled', true);
  });

  it('shows a network message when the Shell API is unreachable', async () => {
    mocks.login.mockRejectedValue(new ApiError(0, 'NetworkError'));
    renderPage();

    await userEvent.type(screen.getByLabelText('نام کاربری'), 'admin');
    await userEvent.type(screen.getByLabelText('رمز عبور'), 'Helix@12345');
    await userEvent.click(screen.getByRole('button', { name: 'ورود' }));

    await waitFor(() =>
      expect(screen.getByRole('alert').textContent).toContain('ارتباط با سرور برقرار نشد.')
    );
  });

  it('toggles password visibility', async () => {
    renderPage();
    const password = screen.getByLabelText('رمز عبور') as HTMLInputElement;

    expect(password.type).toBe('password');
    await userEvent.click(screen.getByRole('button', { name: 'نمایش رمز عبور' }));
    expect(password.type).toBe('text');
    await userEvent.click(screen.getByRole('button', { name: 'پنهان کردن رمز عبور' }));
    expect(password.type).toBe('password');
  });

  it('toggles the theme from the login page', async () => {
    const { onToggleTheme } = renderPage();

    await userEvent.click(screen.getByRole('button', { name: 'تاریک کردن تم' }));

    expect(onToggleTheme).toHaveBeenCalledTimes(1);
  });
});