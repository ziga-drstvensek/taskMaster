<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import type { Board } from '../types';
import api from '../services/api';
import { useBacklogStore } from '../store/backlog';
import { Plus, Edit2, Trash2, Trello, Users, LayoutGrid, GripVertical } from 'lucide-vue-next';
import BaseInput from './common/BaseInput.vue';
import draggable from 'vuedraggable';

const { t } = useI18n();
const backlogStore = useBacklogStore();
const boards = ref<Board[]>([]);
const loading = ref(false);
const showForm = ref(false);

const editingId = ref<number | null>(null);
const name = ref('');
const description = ref('');
const selectedUsernames = ref<string[]>([]);
const allUsers = ref<string[]>([]);
const boardColumns = ref<{id?: number, name: string, color: string, order: number}[]>([]);

const addColumnToBoard = () => {
  boardColumns.value.push({
    name: t('columns.new_column'),
    color: '#cbd5e1',
    order: boardColumns.value.length + 1
  });
};

const onDragEnd = () => {
  boardColumns.value.forEach((col, index) => {
    col.order = index + 1;
  });
};

const removeColumnFromBoard = (index: number) => {
  boardColumns.value.splice(index, 1);
};

const fetchUsers = async () => {
  try {
    const response = await api.get('/auth/users');
    allUsers.value = response.data;
  } catch (err) {
    console.error('Failed to fetch users');
  }
};

const fetchBoards = async () => {
  loading.value = true;
  try {
    const response = await api.get('/boards');
    boards.value = response.data;
    backlogStore.boards = boards.value;
  } catch (err) {
    console.error('Failed to fetch boards');
  } finally {
    loading.value = false;
  }
};

const resetForm = () => {
  editingId.value = null;
  name.value = '';
  description.value = '';
  selectedUsernames.value = [];
  boardColumns.value = [
    { name: 'TODO', color: '#94a3b8', order: 1 },
    { name: 'IN PROGRESS', color: '#6366f1', order: 2 },
    { name: 'DONE', color: '#22c55e', order: 3 }
  ];
  showForm.value = false;
};

const editBoard = (board: Board) => {
  editingId.value = board.id;
  name.value = board.name;
  description.value = board.description || '';
  selectedUsernames.value = [...board.usernames];
  boardColumns.value = board.columns.map(c => ({...c}));
  showForm.value = true;
};

const triggerToast = (msg: string, type: 'info' | 'error' | 'success' = 'info') => {
  (window as any).triggerToast?.(msg, type);
};

const toggleUser = (username: string) => {
  const index = selectedUsernames.value.indexOf(username);
  if (index === -1) {
    selectedUsernames.value.push(username);
  } else {
    selectedUsernames.value.splice(index, 1);
  }
};

const handleSubmit = async () => {
  const data = {
    name: name.value,
    description: description.value,
    usernames: selectedUsernames.value,
    columns: boardColumns.value
  };

  try {
    if (editingId.value) {
      await api.put(`/boards/${editingId.value}`, data);
      triggerToast(t('common.success.saved'), 'success');
    } else {
      await api.post('/boards', data);
      triggerToast(t('common.success.created'), 'success');
    }
    await fetchBoards();
    if (editingId.value === backlogStore.selectedBoardId) {
      const { useColumnStore } = await import('../store/column');
      const columnStore = useColumnStore();
      columnStore.fetchColumns(backlogStore.selectedBoardId);
    }
    resetForm();
  } catch (err) {
    triggerToast(t('common.error.save'), 'error');
  }
};

const deleteBoard = async (id: number) => {
  if (!confirm(t('common.delete_confirm'))) return;
  try {
    await api.delete(`/boards/${id}`);
    await fetchBoards();
    triggerToast(t('common.success.deleted'), 'success');
  } catch (err) {
    triggerToast(t('common.error.delete'), 'error');
  }
};

onMounted(() => {
  fetchBoards();
  fetchUsers();
});
</script>

<template>
  <div>
    <form v-if="showForm" @submit.prevent="handleSubmit" class="mb-8 p-6 bg-indigo-50/50 dark:bg-indigo-900/20 rounded-3xl border border-indigo-100 dark:border-indigo-900/40 space-y-6 animate-in fade-in slide-in-from-top-4 duration-300 shadow-sm">
      <div class="flex items-center gap-2 mb-2">
        <Plus :size="18" class="text-indigo-600 dark:text-indigo-400" />
        <h4 class="font-bold text-slate-800 dark:text-slate-100">{{ editingId ? $t('common.edit') : $t('common.add') }}</h4>
      </div>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="md:col-span-2">
          <BaseInput v-model="name" :label="$t('columns.name')" required />
        </div>
        <div class="md:col-span-2">
          <BaseInput v-model="description" :label="$t('common.description')" />
        </div>
      </div>

      <!-- Stolpci -->
      <div class="space-y-3">
        <label class="block text-[10px] font-black uppercase text-slate-400 dark:text-slate-500 ml-1 flex items-center gap-1 tracking-widest">
          <LayoutGrid :size="14" /> {{ $t('common.columns') }}
        </label>
        <div class="space-y-2 bg-white/50 dark:bg-slate-900/40 p-4 rounded-2xl border border-slate-100 dark:border-slate-800">
          <draggable 
            v-model="boardColumns" 
            item-key="index"
            handle=".drag-handle"
            @end="onDragEnd"
            class="space-y-2"
          >
            <template #item="{ element, index }">
              <div class="flex items-center gap-3 p-2 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-2xl shadow-sm group/col hover:border-indigo-300 dark:hover:border-indigo-500 transition-all">
                <div class="drag-handle cursor-grab active:cursor-grabbing p-1 text-slate-300 dark:text-slate-600 hover:text-indigo-500 transition-colors">
                  <GripVertical :size="18" />
                </div>
                
                <div class="flex-1 flex items-center gap-3">
                  <div 
                    class="w-3 h-3 rounded-full flex-shrink-0 shadow-sm"
                    :style="{ backgroundColor: element.color }"
                  ></div>
                  <input 
                    v-model="element.name" 
                    type="text" 
                    class="flex-1 px-0 py-1 bg-transparent border-none text-xs font-bold outline-none dark:text-slate-100 placeholder:text-slate-300 dark:placeholder:text-slate-600" 
                    :placeholder="$t('columns.name')"
                  >
                </div>

                <div class="flex items-center gap-2 pr-1">
                  <div class="relative w-8 h-8 rounded-lg overflow-hidden border border-slate-200 dark:border-slate-700 group/color">
                    <input 
                      v-model="element.color" 
                      type="color" 
                      class="absolute inset-0 w-[150%] h-[150%] -translate-x-1/4 -translate-y-1/4 cursor-pointer"
                    >
                  </div>
                  
                  <button 
                    @click="removeColumnFromBoard(index)" 
                    type="button" 
                    class="p-2 text-slate-300 hover:text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-xl transition-all"
                  >
                    <Trash2 :size="14" />
                  </button>
                </div>
              </div>
            </template>
          </draggable>
          
          <button 
            type="button" 
            @click="addColumnToBoard"
            class="w-full mt-2 py-3 border-2 border-dashed border-slate-100 dark:border-slate-800 rounded-2xl text-[10px] font-black uppercase tracking-widest text-slate-400 dark:text-slate-500 hover:border-indigo-300 dark:hover:border-indigo-900/40 hover:text-indigo-600 dark:hover:text-indigo-400 transition-all flex items-center justify-center gap-2 bg-slate-50/50 dark:bg-slate-900/20"
          >
            <Plus :size="12" /> {{ $t('columns.add') }}
          </button>
        </div>
      </div>

      <div class="space-y-3">
        <label class="block text-[10px] font-black uppercase text-slate-400 dark:text-slate-500 ml-1 flex items-center gap-1 tracking-widest">
          <Users :size="14" /> {{ $t('common.assignee') }}
        </label>
        <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3 bg-white/50 dark:bg-slate-900/40 p-4 rounded-2xl border border-slate-100 dark:border-slate-800 min-h-[120px]">
          <button 
            v-for="user in allUsers" 
            :key="user"
            type="button"
            @click="toggleUser(user)"
            class="flex items-center gap-2 px-3 py-2.5 rounded-2xl border transition-all text-xs font-bold relative overflow-hidden group/u"
            :class="selectedUsernames.includes(user) 
              ? 'bg-indigo-600 text-white border-indigo-600 shadow-md shadow-indigo-100 dark:shadow-none ring-2 ring-indigo-600 ring-offset-2' 
              : 'bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 border-slate-200 dark:border-slate-700 hover:border-indigo-300 dark:hover:border-indigo-500 hover:text-indigo-600 dark:hover:text-indigo-400'"
          >
            <div 
              class="w-6 h-6 rounded-lg flex items-center justify-center text-[10px] font-black uppercase flex-shrink-0 transition-colors"
              :class="selectedUsernames.includes(user) ? 'bg-white/20' : 'bg-slate-100 dark:bg-slate-700 group-hover/u:bg-indigo-100 dark:group-hover/u:bg-indigo-900/50'"
            >
              {{ user.substring(0, 2) }}
            </div>
            <span class="truncate">{{ user }}</span>
          </button>
          <div v-if="allUsers.length === 0" class="col-span-full flex flex-col items-center justify-center py-6 text-slate-400 dark:text-slate-600">
            <Users :size="24" class="mb-2 opacity-20" />
            <p class="text-[10px] font-bold uppercase tracking-widest">{{ $t('users_mng.no_users') }}</p>
          </div>
        </div>
      </div>

      <div class="flex justify-end gap-4 pt-4 border-t border-indigo-100/50 dark:border-indigo-900/30">
        <button type="button" @click="resetForm" class="px-4 py-2 text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200 text-sm font-bold uppercase tracking-wider transition-colors">{{ $t('common.cancel') }}</button>
        <button type="submit" class="bg-indigo-600 text-white px-8 py-2.5 rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-indigo-700 transition-all shadow-lg shadow-indigo-100 dark:shadow-none active:scale-95">
          {{ editingId ? $t('common.save') : $t('common.add') }}
        </button>
      </div>
    </form>

    <!-- List Header -->
    <div class="flex justify-between items-center mb-6">
      <div class="flex items-center gap-2">
        <Trello class="text-slate-400 dark:text-slate-500" :size="18" />
        <span class="text-xs font-black text-slate-400 dark:text-slate-500 uppercase tracking-widest">{{ $t('common.boards') }} ({{ boards.length }})</span>
      </div>
      <button 
        v-if="!showForm"
        @click="resetForm(); showForm = true" 
        class="bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-indigo-600 dark:text-indigo-400 px-4 py-1.5 rounded-xl text-xs font-black uppercase tracking-widest hover:bg-indigo-50 dark:hover:bg-indigo-900/30 hover:border-indigo-200 dark:hover:border-indigo-900/40 transition-all shadow-sm active:scale-95"
      >
        <Plus :size="14" class="inline mr-1" /> {{ $t('common.add') }}
      </button>
    </div>

    <!-- List -->
    <div v-if="loading" class="flex justify-center py-12">
      <div class="w-8 h-8 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin"></div>
    </div>
    
    <div v-else-if="boards.length === 0" class="text-center py-12 bg-slate-50 dark:bg-slate-900/40 rounded-3xl border-2 border-dashed border-slate-200 dark:border-slate-800">
      <Trello class="mx-auto text-slate-300 dark:text-slate-700 mb-2" :size="48" />
    </div>

    <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-6">
      <div v-for="board in boards" :key="board.id" class="group bg-white dark:bg-slate-900 p-5 rounded-3xl border border-slate-100 dark:border-slate-800 shadow-sm hover:shadow-xl hover:border-indigo-100 dark:hover:border-indigo-900/50 transition-all relative overflow-hidden">
        <div class="flex justify-between items-start mb-3">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-2xl bg-indigo-50 dark:bg-indigo-900/40 flex items-center justify-center text-indigo-600 dark:text-indigo-400 shadow-inner">
              <Trello :size="20" />
            </div>
            <div>
              <h4 class="font-black text-slate-800 dark:text-slate-100 tracking-tight">{{ board.name }}</h4>
              <p class="text-[10px] text-slate-400 dark:text-slate-500 font-bold uppercase tracking-widest">{{ $t('common.created') }}: {{ new Date(board.createdAt).toLocaleDateString() }}</p>
            </div>
          </div>
          <div class="flex gap-1 opacity-0 group-hover:opacity-100 transition-all">
            <button @click="editBoard(board)" class="p-2 text-slate-400 dark:text-slate-500 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-xl transition-all">
              <Edit2 :size="16" />
            </button>
            <button @click="deleteBoard(board.id)" class="p-2 text-slate-400 dark:text-slate-500 hover:text-rose-600 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-xl transition-all">
              <Trash2 :size="16" />
            </button>
          </div>
        </div>
        
        <p v-if="board.description" class="text-xs text-slate-500 dark:text-slate-400 mb-4 line-clamp-2 leading-relaxed font-medium">{{ board.description }}</p>
        
        <div class="flex flex-wrap gap-1.5 pt-3 border-t border-slate-50 dark:border-slate-800/50">
          <div v-for="user in board.usernames" :key="user" 
               class="w-6 h-6 rounded-lg bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-[8px] font-black uppercase text-slate-500 dark:text-slate-400 border border-white dark:border-slate-700 shadow-sm"
               :title="user">
            {{ user.substring(0, 2) }}
          </div>
          <div v-if="board.usernames.length === 0" class="text-[9px] font-black uppercase tracking-widest text-slate-300 dark:text-slate-700">{{ $t('common.none') }}</div>
        </div>
      </div>
    </div>
  </div>
</template>
