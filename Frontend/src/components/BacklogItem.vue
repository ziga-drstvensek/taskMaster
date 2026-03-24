<script setup lang="ts">
import { ref, computed } from 'vue';
import { useI18n } from 'vue-i18n';
import type { BacklogItem } from '../types';
import { BacklogItemPriority, BacklogItemStatus } from '../types';
import { useBacklogStore } from '../store/backlog';
import { useAuthStore } from '../store/auth';
import { useUIStore } from '../store/ui';
import { MessageSquare, Trash2, Calendar, Edit2, ChevronDown, ChevronRight, PlusCircle, User, UserCheck, Paperclip, Clock, LayoutGrid } from 'lucide-vue-next';
import CommentList from './CommentList.vue';
import BacklogForm from './BacklogForm.vue';

const { t, locale } = useI18n();
const props = defineProps<{
  item: BacklogItem
}>();

const backlogStore = useBacklogStore();
const authStore = useAuthStore();
const uiStore = useUIStore();
const showComments = ref(false);
const showEditModal = ref(false);
const showSubtasks = ref(false);

const canEditOrDelete = computed(() => {
  if (authStore.user?.role === 'Admin' || authStore.user?.role === 'Manager') return true;
  return authStore.user?.username === props.item.createdBy;
});

const subtaskToEdit = ref<{item?: BacklogItem, parentId?: number} | null>(null);

const handleEditSubtask = (payload: {item?: BacklogItem, parentId?: number}) => {
  // Close the main edit modal if it's open to ensure the subtask modal appears on top
  showEditModal.value = false;
  subtaskToEdit.value = payload;
  if (!showSubtasks.value) {
    showSubtasks.value = true;
  }
};

const subtasks = computed(() => {
  if (!props.item || !backlogStore.items) return [];
  return backlogStore.items.filter(i => i.parentId === props.item.id);
});

const isAnyModalOpenInItem = computed(() => {
  return showEditModal.value || 
         !!subtaskToEdit.value || 
         uiStore.isModalOpen;
});

const priorityClass = {
  [BacklogItemPriority.Low]: 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-700',
  [BacklogItemPriority.Medium]: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border-amber-200 dark:border-amber-800',
  [BacklogItemPriority.High]: 'bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-400 border-rose-200 dark:border-rose-800',
};

const priorityDotClass = {
  [BacklogItemPriority.Low]: 'bg-slate-400',
  [BacklogItemPriority.Medium]: 'bg-amber-500',
  [BacklogItemPriority.High]: 'bg-rose-500',
};

const priorityLabel = computed(() => ({
  [BacklogItemPriority.Low]: t('common.low'),
  [BacklogItemPriority.Medium]: t('common.medium'),
  [BacklogItemPriority.High]: t('common.high'),
}));

const isNearDueDate = computed(() => {
  if (!props.item.dueDate) return false;
  const due = new Date(props.item.dueDate);
  const now = new Date();
  const diffTime = due.getTime() - now.getTime();
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  return diffDays <= 7;
});

const formattedDueDate = computed(() => {
  if (!props.item.dueDate) return '';
  return new Date(props.item.dueDate).toLocaleDateString(locale.value === 'sl' ? 'sl-SI' : 'en-US', { day: '2-digit', month: '2-digit' });
});

const handleDelete = async () => {
  if (confirm(t('backlog.delete_confirm'))) {
    await backlogStore.deleteItem(props.item.id);
  }
};
</script>

<template>
  <div 
    class="card group hover:border-indigo-300 dark:hover:border-indigo-500 transition-all duration-200 hover:shadow-md dark:shadow-indigo-900/10 relative bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl"
    :class="isAnyModalOpenInItem ? 'cursor-default' : 'cursor-grab active:cursor-grabbing'"
  >
    <!-- Priority indicator bar -->
    <div class="absolute top-0 left-0 w-1 h-full" :class="priorityDotClass[item.priority]"></div>
    
    <div class="p-5 md:p-4">
      <div class="flex justify-between items-start gap-2 mb-3">
        <div class="flex items-start gap-2 flex-1">
          <button 
            v-if="subtasks.length > 0" 
            @click="showSubtasks = !showSubtasks"
            class="mt-1 text-slate-400 dark:text-slate-500 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
          >
            <ChevronDown v-if="showSubtasks" :size="20" class="md:w-4 md:h-4" />
            <ChevronRight v-else :size="20" class="md:w-4 md:h-4" />
          </button>
          <h4 class="font-bold text-slate-800 dark:text-slate-100 text-base md:text-sm leading-tight cursor-pointer hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
              @click="showEditModal = true">
            {{ item.title }}
          </h4>
        </div>
        <div class="flex items-center gap-1">
          <button 
            v-if="canEditOrDelete"
            @click="showEditModal = true"
            class="p-2 md:p-1.5 text-slate-300 dark:text-slate-600 hover:text-indigo-500 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-md transition-all opacity-100 md:opacity-0 group-hover:opacity-100"
            :title="$t('common.edit')"
          >
            <Edit2 :size="18" class="md:w-3.5 md:h-3.5" />
          </button>
           <button 
            v-if="canEditOrDelete"
            @click="handleDelete"
            class="p-2 md:p-1.5 text-slate-300 dark:text-slate-600 hover:text-rose-500 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-md transition-all opacity-100 md:opacity-0 group-hover:opacity-100"
            :title="$t('common.delete')"
          >
            <Trash2 :size="18" class="md:w-3.5 md:h-3.5" />
          </button>
        </div>
      </div>

      <p v-if="item.description" class="text-sm md:text-xs text-slate-500 dark:text-slate-400 mb-5 line-clamp-2 leading-relaxed">
        {{ item.description }}
      </p>

      <div class="flex flex-wrap items-center justify-between gap-4 pt-4 border-t border-slate-50 dark:border-slate-800">
        <div class="flex flex-wrap items-center gap-2.5 md:gap-2">
          <span class="badge"
                :class="priorityClass[item.priority]">
            {{ priorityLabel[item.priority] }}
          </span>
          <span v-if="item.sprintName" class="badge bg-indigo-50 dark:bg-indigo-900/30 text-indigo-600 dark:text-indigo-400 border border-indigo-100 dark:border-indigo-800">
            <Calendar :size="12" class="md:w-2.5 md:h-2.5" /> {{ item.sprintName }}
          </span>
          <span v-if="backlogStore.selectedBoardId === -1" class="badge px-3 py-1.5 md:px-2.5 md:py-1 rounded-full bg-orange-50 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400 border border-orange-200 dark:border-orange-800 shadow-sm" :title="$t('common.boards')">
            <LayoutGrid :size="14" class="md:w-3 md:h-3" /> {{ backlogStore.boards.find(b => b.id === item.boardId)?.name || '' }}
          </span>
          <div v-if="item.assignedTo" class="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-emerald-50 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 border border-emerald-100 dark:border-emerald-800 shadow-sm" :title="$t('common.assignee')">
            <div class="w-5 h-5 rounded-full bg-emerald-500 text-white flex items-center justify-center text-[10px] font-bold uppercase tracking-tighter">
              {{ item.assignedTo.substring(0, 2) }}
            </div>
            <span class="text-xs font-semibold">{{ item.assignedTo }}</span>
          </div>
          <span class="badge px-3 py-1.5 md:px-2.5 md:py-1 rounded-full font-medium text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700" :title="$t('common.author')">
            <User :size="14" class="md:w-3 md:h-3" /> <span class="font-bold mr-1">{{ $t('common.author') }}:</span> {{ item.createdBy || $t('common.unknown') }}
          </span>
          <span v-if="item.attachments && item.attachments.length > 0" class="badge px-3 py-1.5 md:px-2.5 md:py-1 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 border border-slate-200 dark:border-slate-700 shadow-sm" :title="$t('common.attachments')">
            <Paperclip :size="14" class="md:w-3 md:h-3" /> {{ item.attachments.length }}
          </span>
          <span v-if="item.commentsCount > 0" class="badge px-3 py-1.5 md:px-2.5 md:py-1 rounded-full bg-indigo-100 dark:bg-indigo-900/50 text-indigo-700 dark:text-indigo-300 border border-indigo-200 dark:border-indigo-800 shadow-md animate-pulse-slow" :title="$t('common.comments')">
            <MessageSquare :size="14" class="md:w-3 md:h-3" /> {{ item.commentsCount }}
          </span>
          <span v-if="item.dueDate" class="badge px-3 py-1.5 md:px-2.5 md:py-1 rounded-full shadow-sm" 
                :class="isNearDueDate ? 'bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-400 border border-rose-200 dark:border-rose-800' : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 border border-slate-200 dark:border-slate-700'"
                :title="$t('common.due_date')">
            <Clock :size="14" class="md:w-3 md:h-3" /> {{ formattedDueDate }}
          </span>
        </div>

        <div class="flex items-center gap-4 md:gap-3">
          <button 
            @click="handleEditSubtask({ parentId: item.id })"
            class="inline-flex items-center gap-2 text-slate-400 hover:text-emerald-600 transition-colors p-1"
            :title="$t('common.new_subtask')"
          >
            <PlusCircle :size="18" class="md:w-3.5 md:h-3.5" />
          </button>
          <button 
            @click="showComments = !showComments"
            class="inline-flex items-center gap-2 text-slate-400 hover:text-indigo-600 transition-colors p-1"
          >
            <div class="relative flex items-center gap-1.5 md:gap-1">
              <MessageSquare :size="18" class="md:w-3.5 md:h-3.5" />
              <span class="text-xs md:text-[11px] font-medium">{{ $t('common.comments') }}</span>
            </div>
          </button>
        </div>
      </div>

      <div v-if="showSubtasks && subtasks.length > 0" class="mt-4 pt-4 border-t border-slate-100 dark:border-slate-800 space-y-3 md:space-y-2">
        <div v-for="sub in subtasks" :key="sub.id" class="flex items-center justify-between group/sub">
          <div class="flex items-center gap-2">
            <div class="w-1.5 h-1.5 rounded-full" :class="priorityDotClass[sub.priority]"></div>
            <div class="flex flex-col">
              <span class="text-sm md:text-xs text-slate-600 dark:text-slate-300 cursor-pointer hover:text-indigo-600 dark:hover:text-indigo-400" @click="handleEditSubtask({ item: sub })">
                {{ sub.title }}
              </span>
              <div class="flex flex-wrap items-center gap-3 mt-1.5 md:mt-1">
                <span v-if="sub.commentsCount > 0" class="badge bg-indigo-50 dark:bg-indigo-900/30 text-indigo-600 dark:text-indigo-400 border border-indigo-100 dark:border-indigo-800 shadow-sm" :title="$t('common.comments')">
                   <MessageSquare :size="12" class="md:w-2.5 md:h-2.5" /> {{ sub.commentsCount }}
                </span>
                <span class="badge bg-slate-50 dark:bg-slate-800 px-2 py-0.5 md:px-1.5 md:py-0.5 border border-slate-100 dark:border-slate-700">
                  <User :size="12" class="md:w-2.5 md:h-2.5" /> <span class="font-bold">{{ $t('common.author') }}:</span> {{ sub.createdBy || $t('common.unknown') }}
                </span>
                <div v-if="sub.assignedTo" class="flex items-center gap-1.5 px-2 py-0.5 rounded-full bg-emerald-50 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 border border-emerald-100 dark:border-emerald-800 shadow-sm" :title="$t('common.assignee')">
                  <div class="w-4 h-4 rounded-full bg-emerald-500 text-white flex items-center justify-center text-[8px] font-bold uppercase tracking-tighter">
                    {{ sub.assignedTo.substring(0, 2) }}
                  </div>
                  <span class="text-[10px] font-semibold">{{ sub.assignedTo }}</span>
                </div>
              </div>
            </div>
          </div>
          <div class="flex items-center gap-1 opacity-100 md:opacity-0 group-hover/sub:opacity-100 transition-opacity">
            <button @click="backlogStore.deleteItem(sub.id)" class="text-slate-300 dark:text-slate-600 hover:text-rose-500 dark:hover:text-rose-400 p-1.5 md:p-1">
              <Trash2 :size="16" class="md:w-3 md:h-3" />
            </button>
          </div>
        </div>
      </div>

      <!-- Inline Comment List -->
      <div v-if="showComments" class="mt-4 pt-4 border-t border-slate-100 dark:border-slate-800 animate-in fade-in slide-in-from-top-2 duration-300">
        <CommentList :backlogItemId="item.id" />
      </div>
    </div>

    <!-- Edit Modal -->
    <BacklogForm 
      v-if="showEditModal"
      :item="item" 
      @close="showEditModal = false"
      @edit-subtask="handleEditSubtask"
      @submit="backlogStore.fetchItems()"
    />

    <!-- Subtask Edit/Add Modal -->
    <BacklogForm 
      v-if="subtaskToEdit"
      v-bind="subtaskToEdit"
      :defaultColumnId="item.columnId"
      :defaultSprintId="item.sprintId"
      @close="subtaskToEdit = null"
      @submit="backlogStore.fetchItems()"
    />
  </div>
</template>
