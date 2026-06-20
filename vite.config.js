

import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes('node_modules')) return;
          if (id.includes('react') || id.includes('scheduler')) return 'react-vendor';
          if (id.includes('@microsoft/signalr')) return 'signalr-vendor';
          if (id.includes('leaflet') || id.includes('react-leaflet')) return 'maps-vendor';
          if (id.includes('bootstrap')) return 'bootstrap-vendor';
          if (id.includes('axios')) return 'http-vendor';
          return 'vendor';
        },
      },
    },
  },
})