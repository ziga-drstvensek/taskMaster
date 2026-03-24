<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import api from '../services/api';
import { Mail, Server, Shield, Send, CheckCircle2, AlertCircle, Loader2 } from 'lucide-vue-next';

const { t } = useI18n();
const loading = ref(false);
const saving = ref(false);
const testing = ref(false);
const testEmail = ref('');
const message = ref({ text: '', type: '' });

const settings = ref({
  host: '',
  port: 587,
  userName: '',
  password: '',
  enableSsl: true,
  fromEmail: '',
  fromName: ''
});

const fetchSettings = async () => {
  loading.value = true;
  try {
    const response = await api.get('/settings/smtp');
    settings.value = {
      host: response.data.host || '',
      port: response.data.port || 587,
      userName: response.data.userName || '',
      password: '', // Don't use null for password field
      enableSsl: response.data.enableSsl !== undefined ? response.data.enableSsl : true,
      fromEmail: response.data.fromEmail || '',
      fromName: response.data.fromName || ''
    };
  } catch (error) {
    console.error('Failed to fetch SMTP settings', error);
  } finally {
    loading.value = false;
  }
};

const saveSettings = async () => {
  saving.value = true;
  message.value = { text: '', type: '' };
  try {
    const payload = { ...settings.value };
    if (!payload.password) delete (payload as any).password;
    
    await api.post('/settings/smtp', payload);
    message.value = { text: t('settings.smtp.saveSuccess'), type: 'success' };
    setTimeout(() => { message.value = { text: '', type: '' }; }, 3000);
  } catch (error) {
    message.value = { text: t('settings.smtp.saveError'), type: 'error' };
  } finally {
    saving.value = false;
  }
};

const testSettings = async () => {
  if (!testEmail.value) return;
  testing.value = true;
  message.value = { text: '', type: '' };
  try {
    // First save the current settings so the backend can use them for testing
    const payload = { ...settings.value };
    if (!payload.password) delete (payload as any).password;
    await api.post('/settings/smtp', payload);
    
    console.log('Testing SMTP with email:', testEmail.value);
    await api.post('/settings/smtp/test', JSON.stringify(testEmail.value), {
        headers: { 'Content-Type': 'application/json' }
    });
    message.value = { text: t('settings.smtp.testSuccess'), type: 'success' };
  } catch (error: any) {
    const errorMsg = error.response?.data?.message || t('settings.smtp.testError');
    message.value = { text: errorMsg, type: 'error' };
  } finally {
    testing.value = false;
  }
};

onMounted(fetchSettings);
</script>

<template>
  <div class="space-y-8 max-w-2xl">
    <div>
      <h3 class="text-xl font-bold text-slate-900 dark:text-white flex items-center gap-2 mb-2">
        <Mail class="text-indigo-600" :size="24" />
        {{ t('settings.smtp.title') }}
      </h3>
      <p class="text-slate-500 dark:text-slate-400">
        {{ t('settings.smtp.description') }}
      </p>
    </div>

    <div v-if="loading" class="flex justify-center py-12">
      <Loader2 class="animate-spin text-indigo-600" :size="32" />
    </div>

    <form v-else @submit.prevent="saveSettings" class="space-y-6">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="col-span-full bg-blue-50 dark:bg-blue-900/20 p-4 rounded-xl border border-blue-100 dark:border-blue-800">
          <h5 class="text-sm font-bold text-blue-800 dark:text-blue-300 mb-2 flex items-center gap-2">
            <Server :size="16" />
            {{ t('settings.smtp.mailtrapGuideTitle') }}
          </h5>
          <div class="flex flex-wrap gap-2">
            <button type="button" @click="settings.host = 'sandbox.smtp.mailtrap.io'; settings.port = 2525; settings.enableSsl = true" class="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-200 rounded hover:bg-blue-200 transition-colors">
              Mailtrap (2525)
            </button>
            <button type="button" @click="settings.host = 'sandbox.smtp.mailtrap.io'; settings.port = 587; settings.enableSsl = true" class="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-200 rounded hover:bg-blue-200 transition-colors">
              Mailtrap (587)
            </button>
          </div>
        </div>

        <div class="space-y-2">
          <label class="text-sm font-bold text-slate-700 dark:text-slate-300 flex items-center gap-2">
            <Server :size="16" />
            {{ t('settings.smtp.host') }}
          </label>
          <input 
            v-model="settings.host" 
            type="text" 
            required
            class="input-field w-full"
            placeholder="smtp.example.com"
          />
        </div>

        <div class="space-y-2">
          <label class="text-sm font-bold text-slate-700 dark:text-slate-300">
            {{ t('settings.smtp.port') }}
          </label>
          <input 
            v-model.number="settings.port" 
            type="number" 
            required
            class="input-field w-full"
            placeholder="587"
          />
        </div>

        <div class="space-y-2">
          <label class="text-sm font-bold text-slate-700 dark:text-slate-300">
            {{ t('settings.smtp.userName') }}
          </label>
          <input 
            v-model="settings.userName" 
            type="text" 
            required
            class="input-field w-full"
            placeholder="user@example.com"
          />
        </div>

        <div class="space-y-2">
          <label class="text-sm font-bold text-slate-700 dark:text-slate-300 flex items-center gap-2">
            <Shield :size="16" />
            {{ t('settings.smtp.password') }}
          </label>
          <input 
            v-model="settings.password" 
            type="password" 
            autocomplete="new-password"
            class="input-field w-full"
            :placeholder="t('settings.smtp.passwordPlaceholder')"
          />
          <p class="text-xs text-slate-500">{{ t('settings.smtp.passwordHelp') }}</p>
        </div>

        <div class="space-y-2">
          <label class="text-sm font-bold text-slate-700 dark:text-slate-300">
            {{ t('settings.smtp.fromEmail') }}
          </label>
          <input 
            v-model="settings.fromEmail" 
            type="email" 
            required
            class="input-field w-full"
            placeholder="noreply@example.com"
          />
        </div>

        <div class="space-y-2">
          <label class="text-sm font-bold text-slate-700 dark:text-slate-300">
            {{ t('settings.smtp.fromName') }}
          </label>
          <input 
            v-model="settings.fromName" 
            type="text" 
            required
            class="input-field w-full"
            placeholder="TaskMaster"
          />
        </div>
      </div>

      <div class="flex items-center gap-3">
        <label class="relative inline-flex items-center cursor-pointer">
          <input type="checkbox" v-model="settings.enableSsl" class="sr-only peer">
          <div class="w-11 h-6 bg-slate-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-indigo-300 dark:peer-focus:ring-indigo-800 rounded-full peer dark:bg-slate-700 peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all dark:border-slate-600 peer-checked:bg-indigo-600"></div>
          <span class="ml-3 text-sm font-medium text-slate-700 dark:text-slate-300">{{ t('settings.smtp.enableSsl') }}</span>
        </label>
      </div>

      <div class="flex items-center justify-between pt-4 border-t border-slate-100 dark:border-slate-800">
        <div v-if="message.text" 
             :class="message.type === 'success' ? 'text-emerald-600 bg-emerald-50 dark:bg-emerald-900/20' : 'text-rose-600 bg-rose-50 dark:bg-rose-900/20'"
             class="px-4 py-2 rounded-lg flex items-center gap-2 text-sm font-medium">
          <CheckCircle2 v-if="message.type === 'success'" :size="18" />
          <AlertCircle v-else :size="18" />
          {{ message.text }}
        </div>
        <div v-else></div>

        <button 
          type="submit" 
          :disabled="saving"
          class="flex items-center gap-2 px-6 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl font-bold shadow-lg shadow-indigo-200 dark:shadow-none transition-all disabled:opacity-50"
        >
          <Loader2 v-if="saving" class="animate-spin" :size="20" />
          {{ t('common.save') }}
        </button>
      </div>
    </form>

    <!-- Test Email Section -->
    <div v-if="!loading" class="pt-8 border-t border-slate-100 dark:border-slate-800">
      <h4 class="text-lg font-bold text-slate-900 dark:text-white mb-4">
        {{ t('settings.smtp.testTitle') }}
      </h4>
      <div class="flex gap-3">
        <input 
          v-model="testEmail" 
          type="email" 
          class="input-field flex-1"
          :placeholder="t('settings.smtp.testPlaceholder')"
        />
        <button 
          @click="testSettings" 
          :disabled="testing || !testEmail"
          class="flex items-center gap-2 px-6 py-2.5 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 rounded-xl font-bold transition-all disabled:opacity-50"
        >
          <Loader2 v-if="testing" class="animate-spin" :size="20" />
          <Send v-else :size="20" />
          {{ t('settings.smtp.testButton') }}
        </button>
      </div>
    </div>
  </div>
</template>
