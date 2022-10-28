import { defineConfig, loadEnv, splitVendorChunkPlugin } from 'vite'
import { test } from 'vitest'
import vue from '@vitejs/plugin-vue'
import path from 'path'

// https://vitejs.dev/config/
export default ({ mode }) => {
  process.env = Object.assign(process.env, loadEnv(mode, process.cwd(), ''))

  return defineConfig({
    plugins: [
      vue({
        template: {
          compilerOptions: {
            isCustomElement: (tag) => {
              const excludeTagPattern: boolean =
                tag.startsWith('ui-item-text') ||
                tag.startsWith('ui-icon') ||
                tag.startsWith('ui-grid') ||
                tag.startsWith('ui-list') ||
                tag.startsWith('ui-switch') ||
                tag.startsWith('ui-spinner') ||
                tag.startsWith('ui-datepicker') ||
                tag.startsWith('ui-alert') ||
                tag.endsWith('last-content') ||
                tag == 'router-link'
              return excludeTagPattern // (return true)
            }
          }
        }
      }),
      splitVendorChunkPlugin()
    ],
    base: process.env.VITE_BASE_PATH,
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
      threads: false, // Preventing Self-register error with canvas
      coverage: {
        provider: 'c8',
        reporter: ['text', 'cobertura']
      }
    }
  })
}
