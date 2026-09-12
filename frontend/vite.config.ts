import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    globals: true,
    unstubGlobals: true
  },
  server: {
    port: 5173,
    proxy: {
      '/api': 'https://localhost:7001',
      '/health': 'https://localhost:7001'
    }
  }
});
