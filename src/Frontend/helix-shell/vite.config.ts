import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

const proxy = {
  '/api': {
    target: 'http://localhost:5080',
    changeOrigin: false
  },
  '/modules': {
    target: 'http://localhost:5080',
    changeOrigin: false
  }
};

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    strictPort: true,
    proxy
  },
  preview: {
    port: 4173,
    strictPort: true,
    proxy
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    restoreMocks: true
  }
});