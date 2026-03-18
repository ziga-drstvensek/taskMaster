import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  base: './',
  plugins: [
    vue(),
    tailwindcss(),
  ],
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
