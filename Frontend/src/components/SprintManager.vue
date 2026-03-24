<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import type { Sprint } from '../types';
import api from '../services/api';
import { useBacklogStore } from '../store/backlog';
import { Plus, Edit2, Trash2, X, Check, Calendar } from 'lucide-vue-next';
import BaseDatePicker from './common/BaseDatePicker.vue';

const { t } = useI18n();
const backlogStore = useBacklogStore();
const sprints = ref<Sprint[]>([]);
const loading = ref(false);
const showForm = ref(false);

const editingSprintId = ref<number | null>(null);
const name = ref('');
const startDate = ref('');
const endDate = ref('');
const isActive = ref(true);

const resetForm = () => {
  editingSprintId.value = null;
  name.value = '';
  startDate.value = '';
  endDate.value = '';
  isActive.value = true;
  showForm.value = false;
};

const editSprint = (sprint: Sprint) => {
  editingSprintId.value = sprint.id;
  name.value = sprint.name;
  startDate.value = sprint.startDate.split('T')[0];
  endDate.value = sprint.endDate.split('T')[0];
  isActive.value = sprint.isActive;
  showForm.value = true;
};

const triggerToast = (msg: string, type: 'info' | 'error' | 'success' = 'info') => {
  (window as any).triggerToast?.(msg, type);
};

const handleSubmit = async () => {
  const data: any = {
    name: name.value,
    startDate: new Date(startDate.value).toISOString(),
    endDate: new Date(endDate.value).toISOString(),
    isActive: isActive.value
  };

  if (backlogStore.selectedBoardId !== null && backlogStore.selectedBoardId !== -1 && backlogStore.selectedBoardId !== 0) {
    data.boardId = backlogStore.selectedBoardId;
  }

  try {
    if (editingSprintId.value) {
      await api.put(`/sprints/${editingSprintId.value}`, data);
      triggerToast(t('common.success.saved'), 'success');
    } else {
      await api.post('/sprints', data);
      triggerToast(t('common.success.created'), 'success');
    }
    resetForm();
  } catch (err) {
    triggerToast(t('sprints.save_error'), 'error');
  }
};

const deleteSprint = async (id: number) => {
  if (!confirm(t('sprints.delete_confirm'))) return;
  try {
    await api.delete(`/sprints/${id}`);
    triggerToast(t('common.success.deleted'), 'success');
  } catch (err) {
    triggerToast(t('sprints.delete_error'), 'error');
  }
};

onMounted(() => {
  if (backlogStore.sprints.length === 0) {
    backlogStore.fetchSprints();
  }
});
</script>

<template>
  <div>
    <!-- Form -->
    <form @submit.prevent="handleSubmit" v-if="showForm" class="mb-8 p-6 bg-indigo-50/50 dark:bg-indigo-900/20 rounded-3xl border border-indigo-100 dark:border-indigo-900/40 space-y-6 animate-in fade-in slide-in-from-top-4 duration-300 shadow-sm">
      <div class="flex items-center gap-2 mb-2">
        <Plus :size="18" class="text-indigo-600 dark:text-indigo-400" />
        <h4 class="font-bold text-slate-800 dark:text-slate-100">{{ editingSprintId ? $t('sprints.edit') : $t('sprints.add') }}</h4>
      </div>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="md:col-span-2">
          <label class="block text-[10px] font-black uppercase text-slate-400 dark:text-slate-500 mb-1.5 ml-1 tracking-widest">{{ $t('sprints.name') }}</label>
          <input v-model="name" type="text" required class="w-full px-4 py-2.5 border border-slate-200 dark:border-slate-700 rounded-2xl outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-white dark:bg-slate-800 dark:text-slate-100 transition-all">
        </div>
        <div>
          <BaseDatePicker 
            v-model="startDate"
            :label="$t('sprints.start')"
            required
          />
        </div>
        <div>
          <BaseDatePicker 
            v-model="endDate"
            :label="$t('sprints.end')"
            required
          />
        </div>
        <div class="flex items-center gap-3 px-2 py-2">
          <label class="relative inline-flex items-center cursor-pointer">
            <input v-model="isActive" type="checkbox" class="sr-only peer">
            <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border-gray-300 dark:after:border-slate-600 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
            <span class="ms-3 text-sm font-bold text-slate-700 dark:text-slate-300 cursor-pointer">{{ $t('sprints.active_sprint') }}</span>
          </label>
        </div>
      </div>
      <div class="flex justify-end gap-4 pt-4 border-t border-indigo-100/50 dark:border-indigo-900/30">
        <button type="button" @click="resetForm" class="px-4 py-2 text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200 text-sm font-bold uppercase tracking-wider transition-colors">{{ $t('common.cancel') }}</button>
        <button type="submit" class="bg-indigo-600 text-white px-8 py-2.5 rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-indigo-700 transition-all shadow-lg shadow-indigo-100 dark:shadow-none active:scale-95">
          {{ editingSprintId ? $t('common.save') : $t('sprints.add') }}
        </button>
      </div>
    </form>

    <!-- List Header -->
    <div class="flex justify-between items-center mb-6">
      <div class="flex items-center gap-2">
        <Calendar class="text-slate-400 dark:text-slate-500" :size="18" />
        <span class="text-xs font-black text-slate-400 dark:text-slate-500 uppercase tracking-widest">{{ $t('common.sprints') }} ({{ backlogStore.sprints.length }})</span>
      </div>
      <button 
        v-if="!showForm"
        @click="showForm = true" 
        class="bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-indigo-600 dark:text-indigo-400 px-4 py-1.5 rounded-xl text-xs font-black uppercase tracking-widest hover:bg-indigo-50 dark:hover:bg-indigo-900/30 hover:border-indigo-200 dark:hover:border-indigo-900/40 transition-all shadow-sm active:scale-95"
      >
        <Plus :size="14" class="inline mr-1" /> {{ $t('sprints.add') }}
      </button>
    </div>

    <!-- List -->
    <div v-if="loading" class="flex justify-center py-12">
      <div class="w-8 h-8 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin"></div>
    </div>
    
    <div v-else-if="backlogStore.sprints.length === 0" class="text-center py-12 bg-slate-50 dark:bg-slate-900/40 rounded-3xl border-2 border-dashed border-slate-200 dark:border-slate-800">
      <Calendar class="mx-auto text-slate-300 mb-2" :size="48" />
      <p class="text-slate-400 dark:text-slate-500 font-bold uppercase tracking-widest text-xs">{{ $t('sprints.no_sprints') }}</p>
    </div>

    <div v-else class="overflow-x-auto -mx-2">
      <table class="w-full text-left border-collapse">
        <thead>
          <tr class="text-slate-400 dark:text-slate-500 text-[10px] font-black uppercase tracking-widest border-b border-slate-100 dark:border-slate-800">
            <th class="px-6 py-4">{{ $t('sprints.name') }}</th>
            <th class="px-6 py-4">{{ $t('common.period') }}</th>
            <th class="px-6 py-4">{{ $t('common.status') }}</th>
            <th class="px-6 py-4 text-right">{{ $t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-50 dark:divide-slate-800/50">
          <tr v-for="s in backlogStore.sprints" :key="s.id" class="group hover:bg-slate-50/50 dark:hover:bg-slate-800/30 transition-all">
            <td class="px-6 py-4">
               <span class="font-bold text-slate-700 dark:text-slate-200 tracking-tight">{{ s.name }}</span>
            </td>
            <td class="px-6 py-4 text-xs text-slate-500 dark:text-slate-400 font-medium">
              <div class="flex items-center gap-1.5">
                <Calendar :size="12" class="text-slate-300 dark:text-slate-600" />
                {{ new Date(s.startDate).toLocaleDateString() }} - {{ new Date(s.endDate).toLocaleDateString() }}
              </div>
            </td>
            <td class="px-6 py-4">
              <span v-if="s.isActive" class="px-3 py-1 bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 text-[9px] font-black rounded-lg uppercase tracking-widest shadow-sm">{{ $t('common.active') }}</span>
              <span v-else class="px-3 py-1 bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 text-[9px] font-black rounded-lg uppercase tracking-widest">{{ $t('common.completed') }}</span>
            </td>
            <td class="px-6 py-4 text-right">
              <div class="flex justify-end gap-1 opacity-0 group-hover:opacity-100 transition-all">
                <button @click="editSprint(s)" class="p-2 text-slate-400 dark:text-slate-500 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-xl transition-all" :title="$t('common.edit')">
                  <Edit2 :size="16" />
                </button>
                <button @click="deleteSprint(s.id)" class="p-2 text-slate-400 dark:text-slate-500 hover:text-rose-600 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-xl transition-all" :title="$t('common.delete')">
                  <Trash2 :size="16" />
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
