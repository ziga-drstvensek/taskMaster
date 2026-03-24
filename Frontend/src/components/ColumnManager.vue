<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { useColumnStore } from '../store/column';
import { useBacklogStore } from '../store/backlog';
import i18n from '../i18n';
import { Plus, Trash2, Save, X, GripVertical, Check, LayoutGrid } from 'lucide-vue-next';

const columnStore = useColumnStore();
const backlogStore = useBacklogStore();
const showAddForm = ref(false);

const name = ref('');
const order = ref(1);
const color = ref('#cbd5e1');
const editingId = ref<number | null>(null);

onMounted(() => {
  columnStore.fetchColumns(backlogStore.selectedBoardId);
});

watch(() => backlogStore.selectedBoardId, (newBoardId) => {
  columnStore.fetchColumns(newBoardId);
});

const handleAdd = async () => {
  if (!name.value) return;
  await columnStore.addColumn({
    name: name.value,
    order: order.value,
    color: color.value,
    boardId: backlogStore.selectedBoardId
  });
  resetForm();
};

const handleUpdate = async (column: any) => {
  await columnStore.updateColumn(column.id, {
    ...column,
    boardId: backlogStore.selectedBoardId
  });
  editingId.value = null;
};

const handleDelete = async (id: number) => {
  if (confirm(i18n.global.t('columns.delete_confirm'))) {
    await columnStore.deleteColumn(id);
  }
};

const resetForm = () => {
  name.value = '';
  order.value = columnStore.columns.length + 1;
  color.value = '#cbd5e1';
  showAddForm.value = false;
};

const startEdit = (column: any) => {
  editingId.value = column.id;
};
</script>

<template>
  <div>
    <!-- Add Form -->
    <div v-if="showAddForm" class="mb-8 p-6 bg-slate-50 rounded-3xl border border-slate-100 space-y-4 animate-in fade-in slide-in-from-top-2 shadow-sm">
      <div class="flex items-center gap-2 mb-2">
        <Plus :size="18" class="text-indigo-600" />
        <h4 class="font-bold text-slate-800">{{ $t('columns.add') }}</h4>
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div>
          <label class="label-caps text-slate-400 mb-1.5 ml-1 tracking-widest">{{ $t('columns.name') }}</label>
          <input v-model="name" type="text" class="w-full px-4 py-2.5 border border-slate-200 rounded-2xl outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-white transition-all" :placeholder="$t('columns.name')">
        </div>
        <div>
          <label class="label-caps text-slate-400 mb-1.5 ml-1 tracking-widest">{{ $t('columns.order') }}</label>
          <input v-model="order" type="number" class="w-full px-4 py-2.5 border border-slate-200 rounded-2xl outline-none focus:ring-2 focus:ring-indigo-500/20 focus:border-indigo-500 bg-white transition-all">
        </div>
        <div>
          <label class="label-caps text-slate-400 mb-1.5 ml-1 tracking-widest">{{ $t('columns.color') }}</label>
          <div class="flex gap-2">
            <input v-model="color" type="color" class="h-10 w-12 border border-slate-200 rounded-xl cursor-pointer p-1 bg-white">
            <button @click="handleAdd" class="bg-indigo-600 text-white px-6 rounded-2xl font-black text-xs uppercase tracking-widest hover:bg-indigo-700 transition-all flex-1 shadow-lg shadow-indigo-100 active:scale-95">{{ $t('common.save') }}</button>
            <button @click="resetForm" class="bg-slate-100 text-slate-500 px-3 rounded-2xl hover:bg-slate-200 transition-all"><X :size="18" /></button>
          </div>
        </div>
      </div>
    </div>

    <!-- List Header -->
    <div class="flex justify-between items-center mb-4">
      <div class="flex items-center gap-2">
        <LayoutGrid class="text-slate-400" :size="18" />
        <span class="text-xs font-black text-slate-400 uppercase tracking-widest">{{ $t('common.columns') }} ({{ columnStore.columns.length }})</span>
      </div>
      <button 
        v-if="!showAddForm"
        @click="showAddForm = true" 
        class="bg-white border border-slate-200 text-indigo-600 px-4 py-1.5 rounded-xl text-xs font-black uppercase tracking-widest hover:bg-indigo-50 hover:border-indigo-200 transition-all shadow-sm active:scale-95"
      >
        <Plus :size="14" class="inline mr-1" /> {{ $t('columns.add') }}
      </button>
    </div>

    <!-- Column List -->
    <div class="space-y-3">
      <div v-for="column in columnStore.columns" :key="column.id" class="group flex items-center gap-4 p-4 bg-white border border-slate-100 rounded-3xl hover:shadow-xl hover:border-indigo-100 transition-all">
        <div class="cursor-grab text-slate-300 hover:text-indigo-400 transition-colors">
          <GripVertical :size="20" />
        </div>
        
        <div class="w-10 h-10 rounded-2xl shadow-inner flex items-center justify-center border-4 border-white" :style="{ backgroundColor: column.color }"></div>
        
        <div class="flex-1">
          <div v-if="editingId === column.id" class="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <input 
              v-model="column.name"
              class="font-black text-slate-700 bg-slate-50 border border-indigo-200 rounded-xl px-3 py-1.5 outline-none focus:ring-2 focus:ring-indigo-500 shadow-inner w-full"
              :placeholder="$t('columns.name')"
            >
            <input 
              v-model="column.order"
              type="number"
              class="font-black text-slate-700 bg-slate-50 border border-indigo-200 rounded-xl px-3 py-1.5 outline-none focus:ring-2 focus:ring-indigo-500 shadow-inner w-full"
              :placeholder="$t('columns.order')"
            >
            <div class="flex gap-2">
              <input 
                v-model="column.color"
                type="color"
                class="h-10 w-12 border border-slate-200 rounded-xl cursor-pointer p-1 bg-white"
              >
              <button @click="handleUpdate(column)" class="p-2 bg-emerald-500 text-white rounded-xl hover:bg-emerald-600 transition-colors">
                <Check :size="18" />
              </button>
              <button @click="editingId = null; columnStore.fetchColumns(backlogStore.selectedBoardId)" class="p-2 bg-slate-100 text-slate-500 rounded-xl hover:bg-slate-200 transition-colors">
                <X :size="18" />
              </button>
            </div>
          </div>
          <span v-else @click="startEdit(column)" class="font-black text-slate-700 cursor-pointer hover:text-indigo-600 tracking-tight transition-colors">
            {{ column.name }}
          </span>
        </div>

        <div v-if="editingId !== column.id" class="flex items-center gap-4">
          <div class="flex flex-col items-end">
             <span class="text-xxs font-black text-slate-300 uppercase tracking-widest">{{ $t('columns.order') }}</span>
             <span class="text-xs font-black text-slate-500">{{ column.order }}</span>
          </div>
          <button @click="handleDelete(column.id)" class="p-2 text-slate-300 hover:text-rose-500 hover:bg-rose-50 rounded-xl transition-all opacity-0 group-hover:opacity-100 transform translate-x-2 group-hover:translate-x-0" :title="$t('common.delete')">
            <Trash2 :size="18" />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
