<script setup lang="ts">
import { ref, computed, watch, onMounted, nextTick } from 'vue';
import { useI18n } from 'vue-i18n';
import { useNoteStore } from '../store/notes';
import { Plus, Trash2, Save, FileText, Clock, Search, X, Bold, Italic, Underline as UnderlineIcon, List, ListOrdered, Heading1, Heading2, AlignLeft } from 'lucide-vue-next';

const { t } = useI18n();
const noteStore = useNoteStore();

const searchQuery = ref('');
const editingTitle = ref(false);
const titleInput = ref<HTMLInputElement | null>(null);
const editorRef = ref<HTMLDivElement | null>(null);
const saveTimer = ref<ReturnType<typeof setTimeout> | null>(null);
const localTitle = ref('');
const localContent = ref('');
const isSavingLocally = ref(false);

const filteredNotes = computed(() => {
    if (!searchQuery.value) return noteStore.sortedNotes;
    const q = searchQuery.value.toLowerCase();
    return noteStore.sortedNotes.filter(n =>
        n.title.toLowerCase().includes(q) || n.content.toLowerCase().includes(q)
    );
});

const selectedNote = computed(() => noteStore.selectedNote);

watch(selectedNote, (note) => {
    if (note) {
        localTitle.value = note.title;
        localContent.value = note.content;
        nextTick(() => {
            if (editorRef.value) {
                editorRef.value.innerHTML = note.content;
            }
        });
    }
}, { immediate: true });

const scheduleSave = () => {
    if (saveTimer.value) clearTimeout(saveTimer.value);
    saveTimer.value = setTimeout(async () => {
        if (selectedNote.value) {
            const content = editorRef.value?.innerHTML ?? localContent.value;
            await noteStore.updateNote(selectedNote.value.id, localTitle.value, content);
        }
    }, 800);
};

const onContentInput = () => {
    scheduleSave();
};

const onTitleChange = () => {
    scheduleSave();
};

const startEditTitle = async () => {
    editingTitle.value = true;
    await nextTick();
    titleInput.value?.focus();
    titleInput.value?.select();
};

const finishEditTitle = () => {
    editingTitle.value = false;
    scheduleSave();
};

const formatDoc = (command: string, value?: string) => {
    document.execCommand(command, false, value);
    editorRef.value?.focus();
    scheduleSave();
};

const formatText = (format: string) => {
    switch (format) {
        case 'bold': formatDoc('bold'); break;
        case 'italic': formatDoc('italic'); break;
        case 'underline': formatDoc('underline'); break;
        case 'ul': formatDoc('insertUnorderedList'); break;
        case 'ol': formatDoc('insertOrderedList'); break;
        case 'h1': formatDoc('formatBlock', 'h1'); break;
        case 'h2': formatDoc('formatBlock', 'h2'); break;
        case 'p': formatDoc('formatBlock', 'p'); break;
    }
};

const createNote = async () => {
    await noteStore.createNote();
    await nextTick();
    if (editorRef.value && selectedNote.value) {
        editorRef.value.innerHTML = selectedNote.value.content;
        localTitle.value = selectedNote.value.title;
        localContent.value = selectedNote.value.content;
    }
    startEditTitle();
};

const deleteNote = async (id: number) => {
    if (confirm(t('notepad.confirm_delete'))) {
        await noteStore.deleteNote(id);
        if (selectedNote.value && editorRef.value) {
            editorRef.value.innerHTML = selectedNote.value.content;
            localTitle.value = selectedNote.value.title;
        } else if (editorRef.value) {
            editorRef.value.innerHTML = '';
            localTitle.value = '';
        }
    }
};

const selectNote = (id: number) => {
    if (saveTimer.value) {
        clearTimeout(saveTimer.value);
        if (selectedNote.value) {
            const content = editorRef.value?.innerHTML ?? localContent.value;
            noteStore.updateNote(selectedNote.value.id, localTitle.value, content);
        }
    }
    noteStore.selectNote(id);
};

const formatDate = (dateStr: string) => {
    const d = new Date(dateStr);
    return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
};

const stripHtml = (html: string) => {
    const tmp = document.createElement('div');
    tmp.innerHTML = html;
    return tmp.textContent || tmp.innerText || '';
};

onMounted(async () => {
    await noteStore.fetchNotes();
    if (selectedNote.value && editorRef.value) {
        editorRef.value.innerHTML = selectedNote.value.content;
        localTitle.value = selectedNote.value.title;
    }
});
</script>

<template>
  <div class="flex h-full overflow-hidden bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
    
    <!-- Sidebar: seznam beležk -->
    <aside class="w-64 shrink-0 flex flex-col border-r border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-900/50">
      <!-- Header sidebar -->
      <div class="p-3 border-b border-slate-200 dark:border-slate-800">
        <div class="flex items-center justify-between mb-2">
          <h2 class="text-xs font-black uppercase tracking-widest text-slate-500 dark:text-slate-400">{{ $t('notepad.title') }}</h2>
          <button
            @click="createNote"
            class="p-1.5 rounded-lg bg-indigo-600 text-white hover:bg-indigo-700 transition-colors shadow-sm active:scale-95"
            :title="$t('notepad.new_note')"
          >
            <Plus :size="14" />
          </button>
        </div>
        <div class="flex items-center bg-white dark:bg-slate-800 rounded-lg border border-slate-200 dark:border-slate-700 px-2 py-1.5 gap-1.5">
          <Search :size="12" class="text-slate-400 shrink-0" />
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="$t('notepad.search')"
            class="bg-transparent text-xs outline-none w-full text-slate-700 dark:text-slate-200 placeholder:text-slate-400"
          />
          <button v-if="searchQuery" @click="searchQuery = ''" class="text-slate-400 hover:text-slate-600">
            <X :size="12" />
          </button>
        </div>
      </div>

      <!-- Lista beležk -->
      <div class="flex-1 overflow-y-auto custom-scrollbar py-1">
        <div v-if="noteStore.loading" class="flex items-center justify-center h-20 text-slate-400 text-xs">
          <div class="w-4 h-4 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin mr-2"></div>
          {{ $t('common.loading') }}
        </div>
        <div v-else-if="filteredNotes.length === 0" class="flex flex-col items-center justify-center h-32 gap-2 text-slate-400">
          <FileText :size="24" class="opacity-40" />
          <p class="text-xs">{{ searchQuery ? $t('notepad.no_results') : $t('notepad.empty') }}</p>
          <button v-if="!searchQuery" @click="createNote" class="text-xs text-indigo-600 hover:underline">
            {{ $t('notepad.create_first') }}
          </button>
        </div>
        <button
          v-for="note in filteredNotes"
          :key="note.id"
          @click="selectNote(note.id)"
          class="w-full text-left px-3 py-2.5 transition-all group relative border-b border-slate-100 dark:border-slate-800/50 last:border-b-0"
          :class="noteStore.selectedNoteId === note.id 
            ? 'bg-indigo-50 dark:bg-indigo-900/30 border-l-2 border-l-indigo-500' 
            : 'hover:bg-white dark:hover:bg-slate-800 border-l-2 border-l-transparent'"
        >
          <div class="flex items-start justify-between gap-1">
            <p class="text-xs font-semibold text-slate-700 dark:text-slate-200 truncate leading-tight">
              {{ note.title || $t('notepad.untitled') }}
            </p>
            <button
              @click.stop="deleteNote(note.id)"
              class="opacity-0 group-hover:opacity-100 text-slate-300 hover:text-rose-500 dark:text-slate-600 dark:hover:text-rose-400 transition-all shrink-0 mt-0.5"
            >
              <Trash2 :size="11" />
            </button>
          </div>
          <p class="text-[10px] text-slate-400 dark:text-slate-500 mt-0.5 truncate leading-tight">
            {{ stripHtml(note.content) || $t('notepad.no_content') }}
          </p>
          <div class="flex items-center gap-1 mt-1">
            <Clock :size="9" class="text-slate-300 dark:text-slate-600" />
            <span class="text-[10px] text-slate-300 dark:text-slate-600">{{ formatDate(note.updatedAt) }}</span>
          </div>
        </button>
      </div>
    </aside>

    <!-- Glavni urednik -->
    <div class="flex-1 flex flex-col min-w-0 overflow-hidden">
      <div v-if="!selectedNote" class="flex-1 flex flex-col items-center justify-center text-slate-400 dark:text-slate-600 gap-3">
        <FileText :size="48" class="opacity-20" />
        <p class="text-sm">{{ $t('notepad.select_or_create') }}</p>
        <button @click="createNote" class="btn-primary py-2! px-4! text-sm">
          <Plus :size="16" class="mr-1.5" />
          {{ $t('notepad.new_note') }}
        </button>
      </div>

      <template v-else>
        <!-- Toolbar -->
        <div class="flex items-center gap-1 px-4 py-2 border-b border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-900/30 flex-wrap">
          <button @click="formatText('bold')" class="p-1.5 rounded hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 transition-colors" :title="$t('notepad.bold')">
            <Bold :size="14" />
          </button>
          <button @click="formatText('italic')" class="p-1.5 rounded hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 transition-colors" :title="$t('notepad.italic')">
            <Italic :size="14" />
          </button>
          <button @click="formatText('underline')" class="p-1.5 rounded hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 transition-colors" :title="$t('notepad.underline')">
            <UnderlineIcon :size="14" />
          </button>
          <div class="w-px h-4 bg-slate-200 dark:bg-slate-700 mx-1"></div>
          <button @click="formatText('h1')" class="p-1.5 rounded hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 transition-colors" :title="$t('notepad.heading1')">
            <Heading1 :size="14" />
          </button>
          <button @click="formatText('h2')" class="p-1.5 rounded hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 transition-colors" :title="$t('notepad.heading2')">
            <Heading2 :size="14" />
          </button>
          <button @click="formatText('p')" class="p-1.5 rounded hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 transition-colors" :title="$t('notepad.paragraph')">
            <AlignLeft :size="14" />
          </button>
          <div class="w-px h-4 bg-slate-200 dark:bg-slate-700 mx-1"></div>
          <button @click="formatText('ul')" class="p-1.5 rounded hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 transition-colors" :title="$t('notepad.bullet_list')">
            <List :size="14" />
          </button>
          <button @click="formatText('ol')" class="p-1.5 rounded hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 transition-colors" :title="$t('notepad.ordered_list')">
            <ListOrdered :size="14" />
          </button>
          <div class="flex-1"></div>
          <div class="flex items-center gap-1.5 text-[10px] text-slate-400 dark:text-slate-500">
            <div v-if="noteStore.saving" class="w-3 h-3 border-2 border-indigo-400 border-t-transparent rounded-full animate-spin"></div>
            <Save v-else :size="11" class="text-slate-300" />
            <span>{{ noteStore.saving ? $t('notepad.saving') : $t('notepad.autosave') }}</span>
          </div>
          <button
            @click="deleteNote(selectedNote.id)"
            class="ml-2 p-1.5 rounded hover:bg-rose-50 dark:hover:bg-rose-900/30 text-slate-400 hover:text-rose-500 dark:hover:text-rose-400 transition-colors"
            :title="$t('notepad.delete')"
          >
            <Trash2 :size="14" />
          </button>
        </div>

        <!-- Naslov -->
        <div class="px-6 pt-5 pb-2">
          <input
            v-if="editingTitle"
            ref="titleInput"
            v-model="localTitle"
            @blur="finishEditTitle"
            @keydown.enter="finishEditTitle"
            @input="onTitleChange"
            class="w-full text-2xl font-bold text-slate-800 dark:text-slate-100 bg-transparent outline-none border-b-2 border-indigo-400 pb-1"
          />
          <h1
            v-else
            @click="startEditTitle"
            class="text-2xl font-bold text-slate-800 dark:text-slate-100 cursor-text hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors border-b-2 border-transparent pb-1"
          >
            {{ localTitle || $t('notepad.untitled') }}
          </h1>
          <p class="text-xs text-slate-400 dark:text-slate-600 mt-1 flex items-center gap-1">
            <Clock :size="10" />
            {{ $t('notepad.last_saved') }}: {{ formatDate(selectedNote.updatedAt) }}
          </p>
        </div>

        <!-- Vsebina — rich text editor -->
        <div
          ref="editorRef"
          contenteditable="true"
          @input="onContentInput"
          class="flex-1 overflow-y-auto custom-scrollbar px-6 py-3 text-slate-700 dark:text-slate-200 text-sm leading-relaxed outline-none note-editor"
          :data-placeholder="$t('notepad.start_writing')"
        ></div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.note-editor:empty::before {
  content: attr(data-placeholder);
  color: #94a3b8;
  pointer-events: none;
}

.note-editor :deep(h1) {
  font-size: 1.5rem;
  font-weight: 700;
  margin: 1rem 0 0.5rem;
  color: inherit;
}

.note-editor :deep(h2) {
  font-size: 1.2rem;
  font-weight: 600;
  margin: 0.75rem 0 0.25rem;
  color: inherit;
}

.note-editor :deep(ul) {
  list-style: disc;
  margin-left: 1.5rem;
  margin-top: 0.25rem;
  margin-bottom: 0.25rem;
}

.note-editor :deep(ol) {
  list-style: decimal;
  margin-left: 1.5rem;
  margin-top: 0.25rem;
  margin-bottom: 0.25rem;
}

.note-editor :deep(b), .note-editor :deep(strong) {
  font-weight: 700;
}

.note-editor :deep(i), .note-editor :deep(em) {
  font-style: italic;
}

.note-editor :deep(u) {
  text-decoration: underline;
}

.note-editor :deep(p) {
  margin: 0.25rem 0;
}
</style>
