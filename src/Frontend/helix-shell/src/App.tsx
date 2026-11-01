import { useEffect } from 'react';
import { AuthProvider, useAuth } from './auth/AuthContext';
import { AppShell } from './components/layout/AppShell';
import { SplashScreen } from './components/layout/SplashScreen';
import { useTheme } from './lib/useTheme';
import { LoginPage } from './pages/LoginPage';
import { clearOidcCallbackHash, readOidcCallbackHash } from './lib/auth/oidc';
import { saveSession } from './lib/auth/sessionStore';

function AuthGate() {
  const { isDark, toggle } = useTheme();
  const { status } = useAuth();

  useEffect(() => {
    const { session, failed } = readOidcCallbackHash();

    if (!session && !failed) {
      return;
    }

    if (session) {
      saveSession(session);
    }

    clearOidcCallbackHash();

    if (session || failed) {
      window.location.reload();
    }
  }, []);

  if (status === 'loading') {
    return <SplashScreen />;
  }

  if (status === 'anonymous') {
    return <LoginPage isDark={isDark} onToggleTheme={toggle} />;
  }

  return <AppShell isDark={isDark} onToggleTheme={toggle} />;
}

export default function App() {
  return (
    <AuthProvider>
      <AuthGate />
    </AuthProvider>
  );
}