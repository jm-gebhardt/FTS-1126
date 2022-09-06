import { defineConfig, splitVendorChunkPlugin } from 'vite'
import { test } from 'vitest'
import vue from '@vitejs/plugin-vue'
import path from 'path'
import { json } from 'stream/consumers'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue(), splitVendorChunkPlugin()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      'balm-ui-css': 'balm-ui/dist/balm-ui.css'
    }
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5000',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path.replace(/^\/api/, '')
      },
      '/hub': {
        target: 'ws://127.0.0.1:5000',
        changeOrigin: true,
        secure: false,
        ws: true
      }
    }
  },
  test: {
    environment: 'jsdom',
    coverage: {
      provider: 'c8',
      reporter: ['text', 'cobertura']
    }
  }
})
