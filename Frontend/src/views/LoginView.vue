<script setup lang="ts">
import { ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '../store/auth';
import api from '../services/api';
import BaseInput from '../components/common/BaseInput.vue';

const authStore = useAuthStore();
const { t } = useI18n();
const isLogin = ref(true);
const username = ref('');
const password = ref('');
const email = ref('');
const error = ref('');

const handleSubmit = async () => {
  error.value = '';
  try {
    await authStore.login({ username: username.value, password: password.value });
  } catch (err: any) {
    error.value = err.response?.data?.message || t('auth.error_login');
  }
};

const seedAdmin = async () => {
  try {
    await api.post('/auth/seed-admin');
    alert(t('common.success.admin_seeded'));
  } catch (err) {
    alert(t('common.error.seed_admin'));
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 px-4 transition-colors duration-300">
    <div class="max-w-md w-full">
      <!-- Logo/Brand -->
      <div class="text-center mb-10">
        <div class="inline-flex items-center justify-center w-24 h-24 mb-4">
           <img src="../assets/logo2.png" :alt="$t('common.backlog')" class="w-full h-full object-contain drop-shadow-xl" />
        </div>
        <h1 class="text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">{{ $t('common.backlog') }}</h1>
        <p class="text-slate-500 dark:text-slate-400 mt-2">{{ $t('common.welcome') }}</p>
      </div>

      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-xl shadow-slate-200/60 dark:shadow-black/20 p-8 border border-slate-100 dark:border-slate-800 transition-colors">
        <h2 class="text-xl font-bold text-slate-800 dark:text-slate-100 mb-6">
          {{ $t('common.login') }}
        </h2>

        <form @submit.prevent="handleSubmit" class="space-y-5">
          <BaseInput 
            v-model="username"
            :label="$t('common.username')"
            required
            :placeholder="$t('common.username')"
          />

          <BaseInput 
            v-model="password"
            type="password"
            :label="$t('common.password')"
            required
            placeholder="••••••••"
          />

          <div v-if="error" class="bg-rose-50 dark:bg-rose-900/30 border border-rose-100 dark:border-rose-800 text-rose-600 dark:text-rose-400 px-4 py-3 rounded-xl text-sm flex items-center gap-2">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
            {{ error }}
          </div>

          <button 
            type="submit"
            class="w-full btn-primary py-4 shadow-lg shadow-indigo-100 dark:shadow-indigo-900/20 active:scale-[0.98]"
          >
            {{ $t('auth.login_btn') }}
          </button>
        </form>

        <div class="mt-10 pt-6 border-t border-slate-100 dark:border-slate-800">
          <button 
            @click="seedAdmin"
            class="w-full text-xxs font-bold uppercase tracking-widest text-slate-400 dark:text-slate-500 hover:text-indigo-500 dark:hover:text-indigo-400 transition-colors flex items-center justify-center gap-2"
          >
            <svg xmlns="http://www.w3.org/2000/svg" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/></svg>
            {{ $t('common.seed_admin') }}
          </button>
        </div>
      </div>
      <p class="text-center text-slate-400 dark:text-slate-500 text-xs mt-8">
       Version: 1.0.0
      </p>
      <p class="text-center text-slate-400 dark:text-slate-500 text-xs mt-8">
        &copy; 2026 Backlog Systems. {{ $t('common.rights') }}
      </p>
    </div>
  </div>
</template>
