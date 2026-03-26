<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useAuthStore } from '../store/auth';
import { useI18n } from 'vue-i18n';
import { Save, BellRing, Info, Send } from 'lucide-vue-next';

const { t } = useI18n();
const authStore = useAuthStore();
const teamsWebhookUrl = ref('');
const isSaving = ref(false);
const isTesting = ref(false);

onMounted(() => {
  if (authStore.user?.teamsWebhookUrl) {
    teamsWebhookUrl.value = authStore.user.teamsWebhookUrl;
  }
});

const saveSettings = async () => {
  isSaving.value = true;
  try {
    await authStore.updateProfile({ teamsWebhookUrl: teamsWebhookUrl.value });
    (window as any).triggerToast(t('common.success.saved'), 'success');
  } catch (err) {
    (window as any).triggerToast(t('common.error.save'), 'error');
  } finally {
    isSaving.value = false;
  }
};

const testWebhook = async () => {
  if (!teamsWebhookUrl.value) return;
  
  isTesting.value = true;
  try {
    await authStore.testTeamsWebhook(teamsWebhookUrl.value);
    (window as any).triggerToast(t('common.teams_test_success'), 'success');
  } catch (err) {
    (window as any).triggerToast(t('common.teams_test_error'), 'error');
  } finally {
    isTesting.value = false;
  }
};
</script>

<template>
  <div class="space-y-6 max-w-2xl">
    <div>
      <h3 class="text-lg font-bold text-slate-800 dark:text-slate-100 flex items-center gap-2">
        <BellRing :size="20" class="text-indigo-500" />
        {{ t('common.notification_settings') }}
      </h3>
      <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">
        {{ t('common.notification_desc') }}
      </p>
    </div>

    <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-6 shadow-sm">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-black text-slate-700 dark:text-slate-300 uppercase tracking-wider mb-2">
            {{ t('common.teams_webhook') }}
          </label>
          <div class="relative">
            <input
              v-model="teamsWebhookUrl"
              type="url"
              class="w-full px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 outline-none transition-all text-slate-800 dark:text-slate-100"
              placeholder="https://your-org.webhook.office.com/webhookb2/..."
            />
          </div>
          <div class="mt-4 p-4 bg-indigo-50 dark:bg-indigo-900/20 rounded-xl border border-indigo-100 dark:border-indigo-800/50 flex gap-3">
            <Info :size="18" class="text-indigo-600 dark:text-indigo-400 shrink-0 mt-0.5" />
            <div class="text-sm text-indigo-800 dark:text-indigo-300">
              <p class="font-bold mb-1">{{ t('common.teams_info_title') }}</p>
              <p>{{ t('common.teams_info_text') }}</p>
            </div>
          </div>
        </div>

        <div class="pt-4 flex justify-end gap-3">
          <button
            @click="testWebhook"
            :disabled="isTesting || !teamsWebhookUrl"
            class="flex items-center gap-2 px-6 py-3 bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 dark:hover:bg-slate-700 disabled:opacity-50 text-slate-700 dark:text-slate-200 rounded-xl font-bold transition-all"
          >
            <Send :size="18" v-if="!isTesting" />
            <div v-else class="w-4 h-4 border-2 border-slate-500 border-t-transparent rounded-full animate-spin"></div>
            <span>{{ t('common.teams_test_button') }}</span>
          </button>

          <button
            @click="saveSettings"
            :disabled="isSaving"
            class="flex items-center gap-2 px-6 py-3 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white rounded-xl font-bold transition-all shadow-lg shadow-indigo-200 dark:shadow-none"
          >
            <Save :size="18" v-if="!isSaving" />
            <div v-else class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
            <span>{{ t('common.save') }}</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
