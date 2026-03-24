<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import type { BacklogItem, Sprint, BoardColumn, Attachment, HistoryEntry } from '../types';
import { BacklogItemPriority, BacklogItemStatus } from '../types';
import { useBacklogStore } from '../store/backlog';
import { useColumnStore } from '../store/column';
import api from '../services/api';
import { X, Trash2, User, UserCheck, PlusCircle, Paperclip, Download, Maximize2, Minimize2, History, Clock, ChevronDown, ChevronRight } from 'lucide-vue-next';
import BaseInput from './common/BaseInput.vue';
import BaseDatePicker from './common/BaseDatePicker.vue';
import BaseSelect from './common/BaseSelect.vue';
import BaseTextarea from './common/BaseTextarea.vue';
import BaseModal from './common/BaseModal.vue';

const { t, locale } = useI18n();
const props = defineProps<{
  item?: BacklogItem,
  parentId?: number,
  defaultColumnId?: number,
  defaultSprintId?: number
}>();

const emit = defineEmits(['close', 'edit-subtask', 'submit']);
const backlogStore = useBacklogStore();
const columnStore = useColumnStore();

const title = ref(props.item?.title || '');
const description = ref(props.item?.description || '');
const priority = ref(props.item?.priority ?? BacklogItemPriority.Medium);
const columnId = ref<number | string>(props.item?.columnId ?? props.defaultColumnId ?? '');
const boardId = ref<number | string>(props.item?.boardId ?? (backlogStore.selectedBoardId === -1 || backlogStore.selectedBoardId === null ? (backlogStore.boards.length > 0 ? backlogStore.boards[0].id : '') : backlogStore.selectedBoardId));

const filteredColumns = computed(() => {
  const currentBoardId = boardId.value === '' ? null : parseInt(boardId.value.toString());
  if (currentBoardId === null) return columnStore.columns;
  return columnStore.columns.filter(c => c.boardId === currentBoardId);
});

const filteredSprints = computed(() => {
  const currentBoardId = boardId.value === '' ? null : parseInt(boardId.value.toString());
  if (currentBoardId === null) return backlogStore.sprints;
  return backlogStore.sprints.filter(s => s.boardId === currentBoardId || s.boardId === null);
});

watch(boardId, (newBoardId) => {
  const currentBoardId = newBoardId === '' ? null : parseInt(newBoardId.toString());
  const boardColumns = columnStore.columns.filter(c => c.boardId === currentBoardId);
  
  if (boardColumns.length > 0) {
    // If current column is not in the new board, reset it
    const currentColumnId = parseInt(columnId.value.toString());
    const isColumnValid = boardColumns.some(c => c.id === currentColumnId);
    
    if (!isColumnValid) {
      columnId.value = boardColumns[0].id;
    }
  }

  // Same for sprint
  if (sprintId.value !== '') {
    const currentSprintId = parseInt(sprintId.value.toString());
    const boardSprints = backlogStore.sprints.filter(s => s.boardId === currentBoardId || s.boardId === null);
    const isSprintValid = boardSprints.some(s => s.id === currentSprintId);
    
    if (!isSprintValid) {
      sprintId.value = '';
    }
  }
});
const sprintId = ref<number | string>(props.item?.sprintId ?? props.defaultSprintId ?? '');
const assignedTo = ref(props.item?.assignedTo || '');
const dueDate = ref(props.item?.dueDate ? props.item.dueDate.split('T')[0] : '');
const isExpanded = ref(false);
const isHistoryExpanded = ref(false);

const localItem = ref<BacklogItem | null>(props.item || null);

const users = ref<string[]>([]);
const uploading = ref(false);

const attachments = computed(() => localItem.value?.attachments || []);
const history = computed(() => localItem.value?.history || []);
const subtasks = computed(() => {
  if (!localItem.value || !backlogStore.items) return [];
  const itemId = localItem.value.id;
  return backlogStore.items.filter(i => i.parentId === itemId);
});

const priorityDotClass = {
  [BacklogItemPriority.Low]: 'bg-slate-400',
  [BacklogItemPriority.Medium]: 'bg-amber-500',
  [BacklogItemPriority.High]: 'bg-rose-500',
};

onMounted(async () => {
  if (props.item) {
    title.value = props.item.title;
    description.value = props.item.description || '';
    priority.value = props.item.priority;
    columnId.value = props.item.columnId;
    boardId.value = props.item.boardId ?? '';
    sprintId.value = props.item.sprintId ?? '';
    assignedTo.value = props.item.assignedTo || '';
    dueDate.value = props.item.dueDate ? props.item.dueDate.split('T')[0] : '';
  }

  try {
    const currentBoardId = boardId.value === '' ? null : parseInt(boardId.value.toString());
    const [usersRes] = await Promise.all([
      api.get('/auth/users'),
      columnStore.fetchColumns(currentBoardId),
      backlogStore.fetchSprints()
    ]);
    users.value = usersRes.data;
    
    // Če imamo stolpce v trgovini, a še nimamo izbranega stolpca (npr. pri ustvarjanju novega)
    if (!columnId.value && filteredColumns.value.length > 0) {
      columnId.value = filteredColumns.value[0].id;
    }
  } catch (err) {
    console.error('Failed to fetch metadata', err);
  }
});

const triggerToast = (msg: string, type: 'info' | 'error' | 'success' = 'info') => {
  (window as any).triggerToast?.(msg, type);
};

const handleFileUpload = async (event: any) => {
  const file = event.target.files[0];
  if (!file || !props.item) return;

  const formData = new FormData();
  formData.append('file', file);

  uploading.value = true;
  try {
    await api.post(`/attachments/${props.item.id}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    // Refresh only this item to update attachments and history
    localItem.value = await backlogStore.fetchItem(props.item.id);
    triggerToast(t('common.success.uploaded'), 'success');
    emit('submit');
  } catch (err) {
    triggerToast(t('common.error.upload'), 'error');
  } finally {
    uploading.value = false;
  }
};

const downloadAttachment = async (attachment: Attachment) => {
  try {
    const response = await api.get(`/attachments/${attachment.id}`, { responseType: 'blob' });
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', attachment.fileName);
    document.body.appendChild(link);
    link.click();
    link.remove();
  } catch (err) {
    triggerToast(t('common.error.download'), 'error');
  }
};

const deleteAttachment = async (id: number) => {
  if (!confirm(t('common.delete_confirm'))) return;
  try {
    await api.delete(`/attachments/${id}`);
    if (props.item) {
      localItem.value = await backlogStore.fetchItem(props.item.id);
    }
    triggerToast(t('common.success.deleted'), 'success');
    emit('submit');
  } catch (err) {
    triggerToast(t('common.error.delete'), 'error');
  }
};

const handleSubmit = async () => {
  const data = {
    title: title.value,
    description: description.value,
    priority: parseInt(priority.value.toString()),
    columnId: parseInt(columnId.value.toString()),
    boardId: boardId.value === '' ? null : parseInt(boardId.value.toString()),
    sprintId: sprintId.value === '' ? null : parseInt(sprintId.value.toString()),
    assignedTo: assignedTo.value || null,
    dueDate: dueDate.value ? new Date(dueDate.value).toISOString() : null,
    parentId: props.item?.parentId ?? props.parentId ?? null
  };

  try {
    if (props.item) {
      await backlogStore.updateItem(props.item.id, data);
      triggerToast(t('common.success.saved'), 'success');
    } else {
      await backlogStore.addItem(data);
      triggerToast(t('common.success.created'), 'success');
    }
    emit('submit');
    emit('close');
  } catch (err: any) {
    console.error('Save error:', err.response?.data);
    triggerToast(t('backlog.save_error') + (err.response?.data?.title || 'Preverite vnose.'), 'error');
  }
};
</script>

<template>
  <BaseModal 
    :show="true" 
    @close="$emit('close')"
    :maxWidth="isExpanded ? '1000px' : '700px'"
    persistent
  >
    <template #header>
      <h3 class="text-xl font-bold text-slate-800 dark:text-slate-100 tracking-tight">
        {{ item ? (item.parentId ? $t('common.edit_subtask') : $t('common.edit_task')) : (parentId ? $t('common.new_subtask') : $t('common.new_task')) }}
      </h3>
      <div class="flex items-center gap-2 mr-4">
        <button @click="isExpanded = !isExpanded" class="text-slate-400 dark:text-slate-500 hover:text-indigo-600 dark:hover:text-indigo-400 transition p-1.5 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg" :title="isExpanded ? $t('common.collapse') : $t('common.expand')">
          <Minimize2 v-if="isExpanded" :size="18" />
          <Maximize2 v-else :size="18" />
        </button>
      </div>
    </template>

    <form @submit.prevent="handleSubmit" class="flex flex-col h-full overflow-hidden">
      <div class="flex-1 overflow-y-auto space-y-6 custom-scrollbar">
        <div :class="['grid grid-cols-1 gap-6', (!parentId && (!item || !item.parentId)) ? 'lg:grid-cols-2' : '']">
          <!-- Left Side: Main Info -->
          <div class="space-y-5">
            <BaseInput 
              v-model="title" 
              :label="$t('common.title')"
              required
              :placeholder="$t('common.title')"
            />

            <BaseTextarea 
              v-model="description" 
              :label="$t('common.description')"
              :rows="isExpanded ? 15 : 6"
              :placeholder="$t('common.description')"
            />

            <!-- Attachments Section -->
            <div v-if="item" class="space-y-3 pt-2">
              <div class="flex justify-between items-center">
                <label class="block text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
                  <Paperclip :size="14" /> {{ $t('common.attachments') }} ({{ attachments.length }})
                </label>
                <label class="cursor-pointer text-xs font-bold text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 dark:hover:text-indigo-300 transition bg-indigo-50 dark:bg-indigo-900/30 px-2 py-1 rounded-lg">
                  <input type="file" class="hidden" @change="handleFileUpload" :disabled="uploading" />
                  <span v-if="uploading">{{ $t('common.loading') }}</span>
                  <span v-else class="flex items-center gap-1"><PlusCircle :size="14" /> {{ $t('common.upload') }}</span>
                </label>
              </div>

              <div v-if="attachments.length > 0" class="bg-slate-50 dark:bg-slate-900/40 rounded-2xl border border-slate-100 dark:border-slate-800 p-2 space-y-1">
                <div v-for="att in attachments" :key="att.id" class="flex items-center justify-between p-2.5 bg-white dark:bg-slate-900 rounded-xl border border-slate-50 dark:border-slate-800 group shadow-sm transition-all hover:shadow-md">
                  <div class="flex items-center gap-2 overflow-hidden">
                    <Paperclip :size="12" class="text-slate-400 dark:text-slate-500 flex-shrink-0" />
                    <span class="text-xs text-slate-600 dark:text-slate-300 truncate font-medium" :title="att.fileName">{{ att.fileName }}</span>
                    <span class="text-xxs text-slate-400 dark:text-slate-500 flex-shrink-0">({{ (att.size / 1024).toFixed(1) }} KB)</span>
                  </div>
                  <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button type="button" @click="downloadAttachment(att)" class="p-1.5 text-indigo-500 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-lg" :title="$t('common.download')">
                      <Download :size="14" />
                    </button>
                    <button type="button" @click="deleteAttachment(att.id)" class="p-1.5 text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-lg" :title="$t('common.delete')">
                      <Trash2 :size="14" />
                    </button>
                  </div>
                </div>
              </div>
              <div v-else class="text-xxs text-slate-400 dark:text-slate-500 text-center py-4 bg-slate-50 dark:bg-slate-900/40 rounded-2xl border-2 border-dashed border-slate-200 dark:border-slate-800">
                {{ $t('common.no_attachments') }}
              </div>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <BaseSelect 
                v-model="boardId"
                :label="$t('common.boards')"
                :options="backlogStore.boards.map(b => ({ value: b.id, label: b.name }))"
              />
              <BaseSelect 
                v-model="sprintId"
                :label="$t('common.sprint')"
                :placeholder="$t('common.no_sprint')"
                :options="filteredSprints.map(s => ({ value: s.id, label: s.name }))"
              />
            </div>

            <div class="grid grid-cols-2 gap-4">
              <BaseSelect 
                v-model="priority"
                :label="$t('common.priority')"
                :options="[
                  { value: BacklogItemPriority.Low, label: $t('common.low') },
                  { value: BacklogItemPriority.Medium, label: $t('common.medium') },
                  { value: BacklogItemPriority.High, label: $t('common.high') }
                ]"
              />
              <BaseSelect 
                v-model="columnId"
                :label="$t('common.column')"
                :options="filteredColumns.map(c => ({ value: c.id, label: c.name }))"
              />
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div class="space-y-1.5">
                <label class="label-caps">{{ $t('common.assignee') }}</label>
                <div class="relative group">
                  <div class="absolute inset-y-0 left-3 flex items-center pointer-events-none text-slate-400 group-focus-within:text-indigo-500 transition-colors">
                    <UserCheck :size="18" />
                  </div>
                  <select 
                    v-model="assignedTo"
                    class="w-full pl-10 pr-4 py-2.5 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-sm text-slate-700 dark:text-slate-200 focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-400 dark:focus:border-indigo-500 transition-all outline-none appearance-none"
                  >
                    <option value="">{{ $t('common.none') }}</option>
                    <option v-for="user in users" :key="user" :value="user">
                      {{ user }}
                    </option>
                  </select>
                  <div class="absolute inset-y-0 right-3 flex items-center pointer-events-none text-slate-400">
                    <ChevronDown :size="16" />
                  </div>
                </div>
              </div>
              <BaseDatePicker 
                v-model="dueDate"
                :label="$t('common.due_date')"
              />
            </div>
          </div>

          <!-- Right Side: Subtasks (only for main tasks) -->
          <div v-if="!parentId && (!item || !item.parentId)" class="space-y-5 flex flex-col h-full">
            <div class="flex justify-between items-center">
              <label class="block text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">{{ $t('common.subtasks') }} ({{ subtasks.length }})</label>
              <button 
                v-if="item"
                type="button"
                @click="$emit('edit-subtask', { parentId: item.id })"
                class="text-xxs flex items-center gap-1 text-indigo-600 dark:text-indigo-400 hover:text-indigo-800 dark:hover:text-indigo-300 font-black uppercase tracking-widest bg-indigo-50 dark:bg-indigo-900/30 px-2 py-1 rounded-lg transition-all"
              >
                <PlusCircle :size="14" /> {{ $t('common.add') }}
              </button>
            </div>
            
          <div class="bg-slate-50 dark:bg-slate-900/40 rounded-2xl border border-slate-100 dark:border-slate-800 min-h-[150px] max-h-[500px] overflow-y-auto p-3 space-y-3 custom-scrollbar flex-1">
              <div v-if="subtasks.length === 0" class="h-full flex flex-col items-center justify-center text-slate-400 dark:text-slate-500 py-12">
                <PlusCircle :size="32" class="mb-2 opacity-20" />
                <p class="text-xxs font-bold uppercase tracking-widest">{{ $t('common.no_subtasks') }}</p>
              </div>
              <div v-for="sub in subtasks" :key="sub.id" class="bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-50 dark:border-slate-800 shadow-sm flex items-center justify-between group transition-all hover:shadow-md hover:border-indigo-100 dark:hover:border-indigo-900/40">
                <div class="flex items-center gap-3 overflow-hidden">
                  <div class="w-2 h-2 rounded-full flex-shrink-0" :class="priorityDotClass[sub.priority]"></div>
                  <div class="flex flex-col overflow-hidden min-w-0">
                    <span class="text-sm font-bold text-slate-700 dark:text-slate-200 hover:text-indigo-600 dark:hover:text-indigo-400 cursor-pointer transition-colors" @click="$emit('edit-subtask', { item: sub })">
                      {{ sub.title }}
                    </span>
                    <div class="flex flex-wrap items-center gap-3 mt-1.5">
                      <span class="text-[9px] text-slate-400 dark:text-slate-500 flex items-center gap-1 font-bold uppercase">
                        <User :size="10" /> {{ sub.createdBy || $t('common.unknown') }}
                      </span>
                      <span v-if="sub.assignedTo" class="text-[9px] text-emerald-600 dark:text-emerald-400 font-bold flex items-center gap-1 uppercase bg-emerald-50 dark:bg-emerald-900/30 px-1.5 py-0.5 rounded-full">
                        <UserCheck :size="10" /> {{ sub.assignedTo }}
                      </span>
                    </div>
                  </div>
                </div>
                <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0 ml-4">
                  <button type="button" @click="backlogStore.deleteItem(sub.id)" class="p-1.5 text-slate-300 dark:text-slate-600 hover:text-rose-500 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-lg transition-all">
                    <Trash2 :size="14" />
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div v-if="item" class="p-4 bg-slate-50 dark:bg-slate-900/60 rounded-2xl border border-slate-100 dark:border-slate-800 space-y-4 transition-colors">
          <div class="flex flex-wrap justify-between gap-4">
            <div class="flex items-center gap-1.5">
              <div class="w-6 h-6 rounded-full bg-slate-200 dark:bg-slate-800 flex items-center justify-center text-xxs font-black uppercase text-slate-500 dark:text-slate-400">
                {{ (item.createdBy || 'N').substring(0, 1) }}
              </div>
              <span class="text-xxs font-bold text-slate-500 dark:text-slate-400"><span class="uppercase text-slate-400 dark:text-slate-500 mr-1">{{ $t('common.author') }}:</span> {{ item.createdBy || $t('common.unknown') }}</span>
            </div>
            <div class="flex flex-col items-end gap-1">
              <span class="text-xxs font-bold text-slate-400 dark:text-slate-500"><span class="uppercase mr-1">{{ $t('common.created') }}:</span> {{ new Date(item.createdAt).toLocaleString(locale === 'sl' ? 'sl-SI' : 'en-US') }}</span>
              <span class="text-xxs font-bold text-slate-400 dark:text-slate-500"><span class="uppercase mr-1">{{ $t('common.updatedAt') }}:</span> {{ new Date(item.updatedAt).toLocaleString(locale === 'sl' ? 'sl-SI' : 'en-US') }}</span>
            </div>
          </div>

          <!-- History Section moved to the very end of content -->
          <div v-if="history.length > 0" class="space-y-3 pt-4 border-t border-slate-200/60 dark:border-slate-800">
            <button 
              type="button"
              @click="isHistoryExpanded = !isHistoryExpanded"
              class="w-full flex items-center justify-between group/h-header"
            >
              <label class="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider flex items-center gap-1.5 cursor-pointer group-hover/h-header:text-indigo-600 dark:group-hover/h-header:text-indigo-400 transition-colors">
                <History :size="14" /> {{ $t('common.history') }} ({{ history.length }})
              </label>
              <div class="text-slate-400 group-hover/h-header:text-indigo-500 transition-colors">
                <ChevronDown v-if="isHistoryExpanded" :size="16" />
                <ChevronRight v-else :size="16" />
              </div>
            </button>
            
            <div v-if="isHistoryExpanded" class="space-y-3 max-h-60 overflow-y-auto pr-2 custom-scrollbar">
              <div v-for="h in history" :key="h.id" class="relative pl-6 pb-1 group/h">
                <!-- Timeline line -->
                <div class="absolute left-2 top-2 bottom-0 w-px bg-slate-200 dark:bg-slate-800 group-last/h:bg-transparent"></div>
                <!-- Timeline dot -->
                <div class="absolute left-0 top-1.5 w-4 h-4 rounded-full border-2 border-white dark:border-slate-900 bg-indigo-500 shadow-sm z-10"></div>
                
                <div class="flex flex-col">
                  <div class="flex items-center gap-2">
                    <span class="text-xxs font-black text-slate-700 dark:text-slate-300 uppercase tracking-tight">{{ h.changedBy }}</span>
                    <span class="text-xxs text-slate-400 dark:text-slate-500 font-bold flex items-center gap-1 uppercase">
                      <Clock :size="10" /> {{ new Date(h.changedAt).toLocaleString(locale === 'sl' ? 'sl-SI' : 'en-US') }}
                    </span>
                  </div>
                  <div class="bg-white dark:bg-slate-800 p-2.5 rounded-xl border border-slate-100 dark:border-slate-800 shadow-sm mt-1">
                    <span class="text-xxs font-black uppercase text-indigo-600 dark:text-indigo-400 mb-0.5 block">{{ h.changeType }}</span>
                    <p class="text-xxs text-slate-600 dark:text-slate-400 leading-relaxed">{{ h.description }}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="px-6 py-4 border-t border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/50 flex justify-end gap-3">
        <button 
          type="button" 
          @click="$emit('close')"
          class="px-6 py-2.5 text-slate-500 hover:text-slate-800 dark:text-slate-400 dark:hover:text-slate-200 font-bold transition-all text-sm uppercase tracking-widest"
        >
          {{ $t('common.cancel') }}
        </button>
        <button 
          type="submit"
          class="bg-indigo-600 text-white px-8 py-2.5 rounded-2xl font-black text-sm uppercase tracking-widest hover:bg-indigo-700 transition-all shadow-xl shadow-indigo-200 dark:shadow-none active:scale-95"
        >
          {{ item ? $t('common.save') : (parentId ? $t('common.new_subtask') : $t('common.new_task')) }}
        </button>
      </div>
    </form>
  </BaseModal>
</template>
