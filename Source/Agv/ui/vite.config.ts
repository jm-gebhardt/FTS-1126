import { defineConfig, loadEnv, splitVendorChunkPlugin } from 'vite'
import { test } from 'vitest'
import vue from '@vitejs/plugin-vue'
import path from 'path'

// https://vitejs.dev/config/
export default ({ mode }) => {
  process.env = Object.assign(process.env, loadEnv(mode, process.cwd(), ''))
  
  return defineConfig({
    plugins: [vue(),splitVendorChunkPlugin()],
    base: process.env.VITE_BASE_PATH,
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
        vue: 'vue/dist/vue.esm-bundler.js',
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
