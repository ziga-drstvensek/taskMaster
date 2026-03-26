<script setup lang="ts">
import { ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '../store/auth';
import { Settings, Users, Trello, ChevronRight, Palette, Mail, BellRing } from 'lucide-vue-next';
import SprintManager from './SprintManager.vue';
import UserManager from './UserManager.vue';
import BoardManager from './BoardManager.vue';
import AppearanceSettings from './AppearanceSettings.vue';
import SmtpSettings from './SmtpSettings.vue';
import NotificationSettings from './NotificationSettings.vue';

const props = defineProps<{
  initialTab?: 'sprints' | 'users' | 'boards' | 'appearance' | 'mailing';
}>();

const { t } = useI18n();
const authStore = useAuthStore();

const activeTab = ref(props.initialTab || 'boards');

const tabs = computed(() => {
  const items = [
    { id: 'boards', name: t('common.boards'), icon: Trello, show: authStore.isManager },
    { id: 'sprints', name: t('common.sprints'), icon: Settings, show: authStore.isManager },
    { id: 'users', name: t('common.users'), icon: Users, show: authStore.isAdmin },
    { id: 'mailing', name: t('common.mailing'), icon: Mail, show: authStore.isAdmin },
    { id: 'notifications', name: t('common.notifications'), icon: BellRing, show: true },
    { id: 'appearance', name: t('common.appearance'), icon: Palette, show: true },
  ];
  return items.filter(item => item.show);
});
</script>

<template>
  <div class="flex flex-col md:flex-row h-full min-h-[500px]">
    <!-- Sidebar -->
    <div class="w-full md:w-64 border-b md:border-b-0 md:border-r border-slate-200 dark:border-slate-800 p-4 space-y-2">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        @click="activeTab = tab.id as any"
        class="w-full flex items-center justify-between px-4 py-3 rounded-xl text-sm font-bold transition-all group"
        :class="activeTab === tab.id
          ? 'bg-indigo-600 text-white shadow-lg shadow-indigo-200 dark:shadow-none'
          : 'text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800 hover:text-indigo-600 dark:hover:text-indigo-400'"
      >
        <div class="flex items-center gap-3">
          <component :is="tab.icon" :size="18" />
          <span>{{ tab.name }}</span>
        </div>
        <ChevronRight :size="14" v-if="activeTab !== tab.id" class="opacity-0 group-hover:opacity-100 transition-opacity" />
      </button>
    </div>

    <!-- Content -->
    <div class="flex-1 overflow-y-auto p-6 min-h-0">
      <div v-if="activeTab === 'boards'">
        <BoardManager />
      </div>
      <div v-else-if="activeTab === 'sprints'">
        <SprintManager />
      </div>
      <div v-else-if="activeTab === 'users' && authStore.isAdmin">
        <UserManager />
      </div>
      <div v-else-if="activeTab === 'mailing' && authStore.isAdmin">
        <SmtpSettings />
      </div>
      <div v-else-if="activeTab === 'appearance'">
        <AppearanceSettings />
      </div>
      <div v-else-if="activeTab === 'notifications'">
        <NotificationSettings />
      </div>
    </div>
  </div>
</template>
