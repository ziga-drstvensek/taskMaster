<script setup lang="ts">
import { computed, onMounted, watch, ref } from 'vue';
import { useBacklogStore } from '../store/backlog';
import { useColumnStore } from '../store/column';
import { useAuthStore } from '../store/auth';
import { useUIStore } from '../store/ui';
import { useI18n } from 'vue-i18n';
import BacklogItemComponent from './BacklogItem.vue';
import BacklogForm from './BacklogForm.vue';
import draggable from 'vuedraggable';
import { 
  LayoutGrid, 
  Trello, 
  User, 
  UserCheck, 
  Calendar, 
  Paperclip, 
  MessageSquare,
  Clock,
  MoreVertical,
  Edit2,
  Trash2,
  ChevronDown,
  ChevronRight,
  Columns,
  List
} from 'lucide-vue-next';

const backlogStore = useBacklogStore();
const columnStore = useColumnStore();
const uiStore = useUIStore();
const authStore = useAuthStore();
const { t, locale } = useI18n();

const itemToEdit = ref<any>(null);

onMounted(async () => {
  if (!backlogStore.boards.length) {
    await backlogStore.fetchBoards();
  }
  if (backlogStore.selectedBoardId !== -1) {
    await columnStore.fetchColumns(backlogStore.selectedBoardId);
  }
});

watch(() => backlogStore.selectedBoardId, async (newBoardId) => {
  if (newBoardId !== -1) {
    await columnStore.fetchColumns(newBoardId);
  }
});

const props = defineProps<{
  disabled?: boolean
}>();

const isModalActive = computed(() => uiStore.isModalOpen);

const priorityClass = {
  0: 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-700',
  1: 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border-amber-200 dark:border-amber-800',
  2: 'bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-400 border-rose-200 dark:border-rose-800',
};

const priorityLabel = computed(() => ({
  0: t('common.low'),
  1: t('common.medium'),
  2: t('common.high'),
}));

const allItems = computed(() => {
  let items = backlogStore.items.filter(i => !i.parentId);

  // Pripravimo zemljevid stolpcev za hitrejši dostop
  const columnsMap = new Map();
  backlogStore.boards.forEach(board => {
    board.columns.forEach(col => {
      columnsMap.set(col.id, col);
    });
  });

  items = items.map(item => {
    const column = columnsMap.get(item.columnId);
    return {
      ...item,
      columnName: column?.name || t('common.unknown'),
      columnColor: column?.color || '#cbd5e1'
    };
  });

  if (backlogStore.selectedSprintId !== null && backlogStore.selectedSprintId !== undefined) {
    items = items.filter(i => i.sprintId === backlogStore.selectedSprintId);
  }

  if (backlogStore.selectedDashboardId === 'me') {
    items = items.filter(i => i.assignedTo === authStore.user?.username);
  } else if (backlogStore.selectedDashboardId === 'unassigned') {
    items = items.filter(i => !i.assignedTo);
  }

  return items;
});

const getItemsForColumn = (columnId: any) => {
  return computed({
    get: () => {
      return allItems.value.filter(i => i.columnId === columnId);
    },
    set: (val) => {
      updateItems(val, columnId);
    }
  });
};


const updateItems = (newItems: any[], columnId: number) => {
  const newItemIds = new Set(newItems.map(i => i.id));
  const otherItems = backlogStore.items.filter(i => !newItemIds.has(i.id));
  const updatedItemsInColumn = newItems.map((item, index) => ({ 
    ...item, 
    columnId,
    order: index + 1
  }));

  const finalItems = [...otherItems, ...updatedItemsInColumn].map(item => {
    if (item.parentId) {
      const parent = updatedItemsInColumn.find(p => p.id === item.parentId);
      if (parent) {
        return { ...item, columnId: parent.columnId };
      }
    }
    return item;
  });
  
  backlogStore.reorderItems(finalItems);
};

const deleteItem = async (id: number) => {
  if (confirm(t('backlog.delete_confirm'))) {
    await backlogStore.deleteItem(id);
  }
};
</script>

<template>
  <div v-if="backlogStore.selectedBoardId === -1 || uiStore.viewMode === 'table'" class="h-full flex flex-col">
    <!-- View Mode Switcher (only for individual boards) -->
    <div v-if="backlogStore.selectedBoardId !== -1" class="flex justify-end mb-4">
      <div class="inline-flex bg-slate-100 dark:bg-slate-800 p-1 rounded-xl border border-slate-200 dark:border-slate-700">
        <button 
          @click="uiStore.setViewMode('kanban')"
          class="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-bold transition-all"
          :class="uiStore.viewMode === 'kanban' ? 'bg-white dark:bg-slate-700 text-indigo-600 dark:text-indigo-400 shadow-sm' : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'"
        >
          <Columns :size="14" />
          {{ t('common.kanban_view') }}
        </button>
        <button 
          @click="uiStore.setViewMode('table')"
          class="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-bold transition-all"
          :class="uiStore.viewMode === 'table' ? 'bg-white dark:bg-slate-700 text-indigo-600 dark:text-indigo-400 shadow-sm' : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'"
        >
          <List :size="14" />
          {{ t('common.table_view') }}
        </button>
      </div>
    </div>

    <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl overflow-hidden shadow-sm flex-1 flex flex-col">
      <div class="overflow-x-auto overflow-y-auto flex-1 custom-scrollbar">
        <!-- Desktop Table View (visible on medium and larger screens) -->
        <table class="hidden md:table w-full text-left border-collapse min-w-[1000px]">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/50 border-b border-slate-200 dark:border-slate-700 sticky top-0 z-10">
              <th class="px-6 py-4 text-[10px] font-black uppercase tracking-widest text-slate-500 dark:text-slate-400">{{ t('common.title') }}</th>
              <th class="px-6 py-4 text-[10px] font-black uppercase tracking-widest text-slate-500 dark:text-slate-400">{{ t('common.column') }}</th>
              <th class="px-6 py-4 text-[10px] font-black uppercase tracking-widest text-slate-500 dark:text-slate-400">{{ t('common.priority') }}</th>
              <th class="px-6 py-4 text-[10px] font-black uppercase tracking-widest text-slate-500 dark:text-slate-400">{{ t('common.boards') }}</th>
              <th class="px-6 py-4 text-[10px] font-black uppercase tracking-widest text-slate-500 dark:text-slate-400">{{ t('common.assignee') }}</th>
              <th class="px-6 py-4 text-[10px] font-black uppercase tracking-widest text-slate-500 dark:text-slate-400">{{ t('common.due_date') }}</th>
              <th class="px-6 py-4 text-[10px] font-black uppercase tracking-widest text-slate-500 dark:text-slate-400 text-right">{{ t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
            <tr v-if="allItems.length === 0">
              <td colspan="7" class="px-6 py-12 text-center text-slate-400 dark:text-slate-500 font-bold uppercase tracking-widest text-xs">
                {{ t('common.no_items') }}
              </td>
            </tr>
            <tr v-for="item in allItems" :key="item.id" class="hover:bg-slate-50/50 dark:hover:bg-slate-800/30 transition-colors group">
              <td class="px-6 py-4">
                <div class="flex items-center gap-3">
                  <div class="w-1 h-6 rounded-full" :style="{ backgroundColor: (item as any).columnColor }"></div>
                  <div>
                    <div class="font-bold text-slate-800 dark:text-slate-200 text-sm hover:text-indigo-600 dark:hover:text-indigo-400 cursor-pointer transition-colors" @click="itemToEdit = item">
                      {{ item.title }}
                    </div>
                    <div class="text-[10px] text-slate-400 dark:text-slate-500 font-medium truncate max-w-[200px]" v-if="item.description">
                      {{ item.description }}
                    </div>
                  </div>
                </div>
              </td>
              <td class="px-6 py-4">
                <span class="px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-widest border"
                      :style="{ color: (item as any).columnColor, borderColor: (item as any).columnColor + '40', backgroundColor: (item as any).columnColor + '10' }">
                  {{ (item as any).columnName }}
                </span>
              </td>
              <td class="px-6 py-4">
                <span class="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wide border"
                      :class="priorityClass[item.priority as 0 | 1 | 2]">
                  {{ priorityLabel[item.priority as 0 | 1 | 2] }}
                </span>
              </td>
              <td class="px-6 py-4">
                <div class="flex items-center gap-1.5 text-xs font-bold text-slate-600 dark:text-slate-300">
                  <LayoutGrid :size="14" class="text-slate-400" />
                  {{ backlogStore.boards.find(b => b.id === item.boardId)?.name || '' }}
                </div>
              </td>
              <td class="px-6 py-4">
                <div v-if="item.assignedTo" class="flex items-center gap-2">
                  <div class="w-6 h-6 rounded-lg bg-emerald-100 dark:bg-emerald-900/30 flex items-center justify-center text-emerald-600 dark:text-emerald-400">
                    <UserCheck :size="12" />
                  </div>
                  <span class="text-xs font-bold text-slate-700 dark:text-slate-200">{{ item.assignedTo }}</span>
                </div>
                <span v-else class="text-[10px] font-black uppercase text-slate-400 dark:text-slate-600 tracking-widest italic">{{ t('common.none') }}</span>
              </td>
              <td class="px-6 py-4">
                <div v-if="item.dueDate" class="flex items-center gap-1.5 text-xs font-bold text-slate-500 dark:text-slate-400">
                  <Calendar :size="14" />
                  {{ new Date(item.dueDate).toLocaleDateString(locale === 'sl' ? 'sl-SI' : 'en-US') }}
                </div>
                <span v-else class="text-[10px] font-black uppercase text-slate-300 dark:text-slate-700 tracking-widest">-</span>
              </td>
              <td class="px-6 py-4 text-right">
                <div class="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  <button @click="itemToEdit = item" class="p-2 text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-xl transition-all">
                    <Edit2 :size="16" />
                  </button>
                  <button @click="deleteItem(item.id)" class="p-2 text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-xl transition-all">
                    <Trash2 :size="16" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>

        <!-- Mobile View (visible on small screens) -->
        <div class="md:hidden divide-y divide-slate-100 dark:divide-slate-800">
          <div v-if="allItems.length === 0" class="px-6 py-12 text-center text-slate-400 dark:text-slate-500 font-bold uppercase tracking-widest text-xs">
            {{ t('common.no_items') }}
          </div>
          <div v-for="item in allItems" :key="item.id" class="p-5 md:p-4 hover:bg-slate-50/50 dark:hover:bg-slate-800/30 transition-colors">
            <div class="flex items-start justify-between gap-4">
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-3 mb-2">
                  <div class="w-2 h-2 rounded-full flex-shrink-0" :style="{ backgroundColor: (item as any).columnColor }"></div>
                  <div class="font-bold text-slate-800 dark:text-slate-200 text-base md:text-sm hover:text-indigo-600 dark:hover:text-indigo-400 cursor-pointer transition-colors leading-tight" @click="itemToEdit = item">
                    {{ item.title }}
                  </div>
                </div>
                
                <div class="flex flex-wrap items-center gap-3 mt-3">
                  <span class="px-2.5 py-1 rounded-lg text-[10px] md:text-[9px] font-black uppercase tracking-widest border"
                        :style="{ color: (item as any).columnColor, borderColor: (item as any).columnColor + '40', backgroundColor: (item as any).columnColor + '10' }">
                    {{ (item as any).columnName }}
                  </span>
                  <span class="inline-flex items-center px-2 py-0.5 rounded text-[10px] md:text-[9px] font-bold uppercase tracking-wider border"
                        :class="priorityClass[item.priority as 0 | 1 | 2]">
                    {{ priorityLabel[item.priority as 0 | 1 | 2] }}
                  </span>
                  <div v-if="item.assignedTo" class="flex items-center gap-1.5 text-xs md:text-[10px] font-bold text-slate-600 dark:text-slate-400">
                    <User :size="12" />
                    {{ item.assignedTo }}
                  </div>
                  <div v-if="item.dueDate" class="flex items-center gap-1.5 text-xs md:text-[10px] font-bold text-slate-500 dark:text-slate-400">
                    <Calendar :size="12" />
                    {{ new Date(item.dueDate).toLocaleDateString(locale === 'sl' ? 'sl-SI' : 'en-US') }}
                  </div>
                </div>
              </div>

              <div class="flex items-center gap-1 flex-shrink-0">
                <button @click="itemToEdit = item" class="p-2.5 text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/30 rounded-xl transition-all">
                  <Edit2 :size="18" />
                </button>
                <button @click="deleteItem(item.id)" class="p-2.5 text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-xl transition-all">
                  <Trash2 :size="18" />
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <BacklogForm v-if="itemToEdit" :item="itemToEdit" @close="itemToEdit = null" />
  </div>

  <div v-else class="h-full flex flex-col">
    <!-- View Mode Switcher -->
    <div class="flex justify-end mb-4">
      <div class="inline-flex bg-slate-100 dark:bg-slate-800 p-1 rounded-xl border border-slate-200 dark:border-slate-700">
        <button 
          @click="uiStore.setViewMode('kanban')"
          class="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-bold transition-all"
          :class="uiStore.viewMode === 'kanban' ? 'bg-white dark:bg-slate-700 text-indigo-600 dark:text-indigo-400 shadow-sm' : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'"
        >
          <Columns :size="14" />
          {{ t('common.kanban_view') }}
        </button>
        <button 
          @click="uiStore.setViewMode('table')"
          class="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-bold transition-all"
          :class="uiStore.viewMode === 'table' ? 'bg-white dark:bg-slate-700 text-indigo-600 dark:text-indigo-400 shadow-sm' : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'"
        >
          <List :size="14" />
          {{ t('common.table_view') }}
        </button>
      </div>
    </div>

    <div class="flex flex-row gap-4 md:gap-6 overflow-x-auto pb-4 custom-scrollbar items-stretch min-h-[calc(100vh-250px)]">
      <div v-for="column in columnStore.columns" :key="column.id" class="flex flex-col flex-shrink-0 w-[280px] md:w-[350px]">
        <div class="flex justify-between items-center mb-4 px-1">
          <div class="flex items-center gap-2">
            <div class="w-2 h-2 rounded-full" :style="{ backgroundColor: column.color }"></div>
            <h3 class="font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider text-xs md:text-sm">{{ column.name }}</h3>
          </div>
          <span class="bg-slate-200 dark:bg-slate-800 text-slate-600 dark:text-slate-400 px-2 py-0.5 rounded-full text-[10px] font-bold">
            {{ getItemsForColumn(column.id).value.length }}
          </span>
        </div>
        
        <div 
          class="bg-slate-100/50 dark:bg-slate-900/50 border border-slate-200 dark:border-slate-800 p-2 md:p-3 rounded-2xl min-h-[200px] md:min-h-[600px] flex flex-col flex-1 transition-colors"
          :style="{ borderColor: column.color + '40' }"
        >
          <draggable 
            v-model="getItemsForColumn(column.id).value" 
            group="tasks" 
            item-key="id"
            class="space-y-4 flex-1"
            ghost-class="sortable-ghost"
            drag-class="sortable-drag"
            animation="200"
            :disabled="disabled || isModalActive"
          >
            <template #item="{ element }">
              <BacklogItemComponent :item="element" />
            </template>
            <template #footer>
              <div v-if="getItemsForColumn(column.id).value.length === 0" class="py-12 flex flex-col items-center justify-center text-slate-400 dark:text-slate-500 border-2 border-dashed border-slate-200 dark:border-slate-800 rounded-xl pointer-events-none">
                <span class="text-xs">{{ $t('common.povlecite_sem') }}</span>
              </div>
            </template>
          </draggable>
        </div>
      </div>
    </div>
  </div>
</template>
