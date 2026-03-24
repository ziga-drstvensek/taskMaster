<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import type { Comment } from '../types';
import api from '../services/api';
import { Send, Edit2, Trash2, Check, X } from 'lucide-vue-next';
import { useAuthStore } from '../store/auth';
import BaseInput from './common/BaseInput.vue';
import BaseTextarea from './common/BaseTextarea.vue';

const props = defineProps<{
  backlogItemId: number
}>();

const { t, locale } = useI18n();
const authStore = useAuthStore();
const comments = ref<Comment[]>([]);
const newComment = ref('');
const loading = ref(false);

const editingId = ref<number | null>(null);
const editingContent = ref('');

const fetchComments = async () => {
  try {
    const response = await api.get(`/backlog/${props.backlogItemId}/comments`);
    console.log('Fetched comments:', response.data);
    comments.value = response.data;
  } catch (err: any) {
    console.error('Failed to fetch comments', err.response?.data || err.message);
  }
};

const triggerToast = (msg: string, type: 'info' | 'error' | 'success' = 'info') => {
  (window as any).triggerToast?.(msg, type);
};

const addComment = async () => {
  if (!newComment.value.trim()) return;
  
  loading.value = true;
  console.log('Adding comment for item:', props.backlogItemId);
  try {
    const response = await api.post(`/backlog/${props.backlogItemId}/comments`, {
      content: newComment.value
    });
    console.log('Comment added:', response.data);
    comments.value.unshift(response.data);
    newComment.value = '';
    triggerToast(t('common.success.saved'), 'success');
    
    const backlogStore = (window as any).backlogStore || (await import('../store/backlog')).useBacklogStore();
    const item = backlogStore.items.find((i: any) => i.id === props.backlogItemId);
    if (item) {
      item.commentsCount = (item.commentsCount || 0) + 1;
    }
  } catch (err: any) {
    console.error('Failed to add comment', err.response?.data || err.message);
    triggerToast(t('common.error.save') + ': ' + (err.response?.data?.message || err.message), 'error');
  } finally {
    loading.value = false;
  }
};

const startEdit = (comment: Comment) => {
  editingId.value = comment.id;
  editingContent.value = comment.content;
};

const cancelEdit = () => {
  editingId.value = null;
  editingContent.value = '';
};

const saveEdit = async (comment: Comment) => {
  if (!editingContent.value.trim()) return;
  
  try {
    await api.put(`/backlog/comments/${comment.id}`, {
      content: editingContent.value
    });
    comment.content = editingContent.value;
    editingId.value = null;
    triggerToast(t('common.success.saved'), 'success');
  } catch (err: any) {
    console.error('Failed to update comment', err.response?.data || err.message);
    triggerToast(t('common.error.save'), 'error');
  }
};

const deleteComment = async (id: number) => {
  if (!confirm(t('common.delete_confirm'))) return;
  
  try {
    await api.delete(`/backlog/comments/${id}`);
    comments.value = comments.value.filter(c => c.id !== id);
    triggerToast(t('common.success.deleted'), 'success');

    const backlogStore = (window as any).backlogStore || (await import('../store/backlog')).useBacklogStore();
    const item = backlogStore.items.find((i: any) => i.id === props.backlogItemId);
    if (item && item.commentsCount > 0) {
      item.commentsCount -= 1;
    }
  } catch (err: any) {
    console.error('Failed to delete comment', err.response?.data || err.message);
    triggerToast(t('common.error.delete'), 'error');
  }
};

onMounted(fetchComments);

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString(locale.value === 'sl' ? 'sl-SI' : 'en-US', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};
</script>

<template>
  <div class="space-y-4">
    <!-- Input -->
    <div class="flex gap-2 items-end">
      <BaseInput 
        v-model="newComment" 
        :placeholder="$t('common.write_comment')"
        @keyup.enter="addComment"
      />
      <button 
        @click="addComment"
        :disabled="loading"
        class="bg-indigo-600 text-white p-2.5 rounded-lg hover:bg-indigo-700 disabled:opacity-50 transition h-[42px] flex items-center justify-center shadow-sm"
      >
        <Send :size="16" />
      </button>
    </div>

    <!-- List -->
    <div class="max-h-64 overflow-y-auto space-y-3 pr-2 custom-scrollbar">
      <div v-for="c in comments" :key="c.id" class="group bg-slate-50 dark:bg-slate-900/40 p-3 rounded-xl border border-gray-100 dark:border-slate-800 transition-all hover:bg-white dark:hover:bg-slate-900 hover:shadow-sm">
        <div class="flex justify-between items-start mb-1.5">
          <div class="flex flex-col">
            <span class="font-bold text-xxs text-indigo-600 dark:text-indigo-400">{{ c.author }}</span>
            <span class="text-xxs text-gray-400 dark:text-slate-500">{{ formatDate(c.createdAt) }}</span>
          </div>
          
          <div v-if="authStore.user?.username === c.author" class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <template v-if="editingId === c.id">
              <button @click="saveEdit(c)" class="p-1 text-emerald-600 hover:bg-emerald-50 dark:hover:bg-emerald-900/30 rounded" :title="$t('common.save')">
                <Check :size="14" />
              </button>
              <button @click="cancelEdit" class="p-1 text-gray-400 dark:text-slate-500 hover:bg-gray-100 dark:hover:bg-slate-800 rounded" :title="$t('common.cancel')">
                <X :size="14" />
              </button>
            </template>
            <template v-else>
              <button @click="startEdit(c)" class="p-1 text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded" :title="$t('common.edit')">
                <Edit2 :size="14" />
              </button>
              <button @click="deleteComment(c.id)" class="p-1 text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded" :title="$t('common.delete')">
                <Trash2 :size="14" />
              </button>
            </template>
          </div>
        </div>

        <div v-if="editingId === c.id">
          <BaseTextarea 
            v-model="editingContent"
            :rows="2"
            @keyup.esc="cancelEdit"
          />
        </div>
        <p v-else class="text-gray-700 dark:text-slate-300 text-sm leading-relaxed whitespace-pre-wrap">{{ c.content }}</p>
      </div>
      
      <p v-if="comments.length === 0" class="text-center text-gray-400 dark:text-slate-500 text-xs py-4">
        {{ $t('common.no_comments') }}
      </p>
    </div>
  </div>
</template>

<style scoped>
.custom-scrollbar::-webkit-scrollbar {
  width: 4px;
}
.custom-scrollbar::-webkit-scrollbar-track {
  background: transparent;
}
.custom-scrollbar::-webkit-scrollbar-thumb {
  background: #e2e8f0;
  border-radius: 10px;
}
.custom-scrollbar::-webkit-scrollbar-thumb:hover {
  background: #cbd5e1;
}
</style>
