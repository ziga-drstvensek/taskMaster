import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  base: './',
  plugins: [
    vue(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:12002',
        changeOrigin: true,
        secure: false
      },
      '/backlogHub': {
        target: 'http://localhost:12002',
        ws: true,
        changeOrigin: true,
        secure: false
      }
    }
  }
})
