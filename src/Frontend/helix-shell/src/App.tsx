import { AuthProvider, useAuth } from './auth/AuthContext';
import { AppShell } from './components/layout/AppShell';
import { SplashScreen } from './components/layout/SplashScreen';
import { useTheme } from './lib/useTheme';
import { LoginPage } from './pages/LoginPage';

function AuthGate() {
  const { isDark, toggle } = useTheme();
  const { status } = useAuth();

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