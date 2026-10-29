import { useCallback, useEffect, useState } from 'react';

const THEME_STORAGE_KEY = 'helix-theme';

export function useTheme() {
  const [isDark, setIsDark] = useState(
    () => typeof document !== 'undefined' && document.documentElement.classList.contains('dark')
  );

  useEffect(() => {
    document.documentElement.classList.toggle('dark', isDark);
    try {
      window.localStorage.setItem(THEME_STORAGE_KEY, isDark ? 'dark' : 'light');
    } catch {
      // Theme persistence is optional.
    }
  }, [isDark]);

  const toggle = useCallback(() => setIsDark((value) => !value), []);

  return { isDark, toggle };
}