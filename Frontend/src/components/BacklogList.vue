<script setup lang="ts">
import { computed, onMounted, watch, ref } from 'vue';
import { useBacklogStore } from '@/store/backlog';
import { useColumnStore } from '@/store/column';
import { useAuthStore } from '@/store/auth';
import { useUIStore } from '@/store/ui';
import { useI18n } from 'vue-i18n';
import BacklogItemComponent from './BacklogItem.vue';
import BacklogForm from './BacklogForm.vue';
import draggable from 'vuedraggable';
import { 
  LayoutGrid,
  User, 
  UserCheck, 
  Calendar,
  Clock,
  Edit2,
  Trash2,
  Zap,
  AlertCircle
} from 'lucide-vue-next';

const backlogStore = useBacklogStore();
const columnStore = useColumnStore();
const uiStore = useUIStore();
const authStore = useAuthStore();
const { t, locale } = useI18n();

const itemToEdit = ref<any>(null);
const activeQuickFilters = ref<string[]>([]);

const toggleQuickFilter = (filter: string) => {
  const index = activeQuickFilters.value.indexOf(filter);
  if (index === -1) {
    activeQuickFilters.value.push(filter);
  } else {
    activeQuickFilters.value.splice(index, 1);
  }
};

const clearQuickFilters = () => {
  activeQuickFilters.value = [];
};

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

  if (backlogStore.searchQuery) {
    const query = backlogStore.searchQuery.toLowerCase();
    items = items.filter(i => 
      i.title.toLowerCase().includes(query) || 
      (i.description && i.description.toLowerCase().includes(query))
    );
  }

  // Quick Filters
  if (activeQuickFilters.value.length > 0) {
    if (activeQuickFilters.value.includes('high_priority')) {
      items = items.filter(i => i.priority === 2);
    }
    if (activeQuickFilters.value.includes('my_tasks')) {
      items = items.filter(i => i.assignedTo === authStore.user?.username);
    }
    if (activeQuickFilters.value.includes('due_soon')) {
      const now = new Date();
      const in3Days = new Date();
      in3Days.setDate(now.getDate() + 3);
      items = items.filter(i => i.dueDate && new Date(i.dueDate) <= in3Days && new Date(i.dueDate) >= now);
    }
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
  <div v-if="backlogStore.selectedBoardId === -1 || uiStore.viewMode === 'table' || backlogStore.selectedDashboardId === 'personal'" class="h-full flex flex-col">
    <!-- View Mode Switcher and Quick Filters -->
    <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
      <div class="flex flex-wrap items-center gap-2">
        <div class="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-500 dark:text-slate-400">
          <Zap :size="14" class="text-amber-500" />
          <span class="text-xs font-black uppercase tracking-widest">{{ t('common.quick_filters') }}</span>
        </div>
        
        <button 
          @click="toggleQuickFilter('my_tasks')"
          class="flex items-center gap-2 px-3 py-1.5 rounded-xl text-xs font-bold transition-all border"
          :class="activeQuickFilters.includes('my_tasks') 
            ? 'bg-indigo-600 text-white border-indigo-600 shadow-md shadow-indigo-200 dark:shadow-none' 
            : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:border-indigo-300 dark:hover:border-indigo-700'"
        >
          <UserCheck :size="14" />
          {{ t('common.filter_my_tasks') }}
        </button>

        <button 
          @click="toggleQuickFilter('high_priority')"
          class="flex items-center gap-2 px-3 py-1.5 rounded-xl text-xs font-bold transition-all border"
          :class="activeQuickFilters.includes('high_priority') 
            ? 'bg-rose-600 text-white border-rose-600 shadow-md shadow-rose-200 dark:shadow-none' 
            : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:border-rose-300 dark:hover:border-rose-700'"
        >
          <AlertCircle :size="14" />
          {{ t('common.filter_priority_high') }}
        </button>

        <button 
          @click="toggleQuickFilter('due_soon')"
          class="flex items-center gap-2 px-3 py-1.5 rounded-xl text-xs font-bold transition-all border"
          :class="activeQuickFilters.includes('due_soon') 
            ? 'bg-amber-600 text-white border-amber-600 shadow-md shadow-amber-200 dark:shadow-none' 
            : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:border-amber-300 dark:hover:border-amber-700'"
        >
          <Clock :size="14" />
          {{ t('common.filter_due_soon') }}
        </button>

        <button 
          v-if="activeQuickFilters.length > 0"
          @click="clearQuickFilters"
          class="flex items-center gap-2 px-3 py-1.5 rounded-xl text-xs font-bold text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-all"
        >
          <Trash2 :size="14" />
          {{ t('common.clear_filters') }}
        </button>
      </div>
    </div>

    <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl overflow-hidden shadow-sm flex-1 flex flex-col">
      <div class="overflow-x-auto overflow-y-auto flex-1 custom-scrollbar">
        <!-- Desktop Table View (visible on medium and larger screens) -->
        <table class="hidden md:table w-full text-left border-collapse min-w-250 relative z-0">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/50 border-b border-slate-200 dark:border-slate-700 sticky top-0 z-30">
              <th class="table-header-cell">{{ t('common.title') }}</th>
              <th class="table-header-cell">{{ t('common.column') }}</th>
              <th class="table-header-cell">{{ t('common.priority') }}</th>
              <th class="table-header-cell">{{ t('common.boards') }}</th>
              <th class="table-header-cell">{{ t('common.assignee') }}</th>
              <th class="table-header-cell">{{ t('common.due_date') }}</th>
              <th class="table-header-cell text-right">{{ t('common.actions') }}</th>
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
                    <div class="text-xxs text-slate-400 dark:text-slate-500 font-medium truncate max-w-50" v-if="item.description">
                      {{ item.description }}
                    </div>
                  </div>
                </div>
              </td>
              <td class="px-6 py-4">
                <span class="badge-outline"
                      :style="{ color: (item as any).columnColor, borderColor: (item as any).columnColor + '40', backgroundColor: (item as any).columnColor + '10' }">
                  {{ (item as any).columnName }}
                </span>
              </td>
              <td class="px-6 py-4">
                <span class="badge"
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
                <span v-else class="text-xxs font-black uppercase text-slate-400 dark:text-slate-600 tracking-widest italic">{{ t('common.none') }}</span>
              </td>
              <td class="px-6 py-4">
                <div v-if="item.dueDate" class="flex items-center gap-1.5 text-xs font-bold text-slate-500 dark:text-slate-400">
                  <Calendar :size="14" />
                  {{ new Date(item.dueDate).toLocaleDateString(locale === 'sl' ? 'sl-SI' : 'en-US') }}
                </div>
                <span v-else class="text-xxs font-black uppercase text-slate-300 dark:text-slate-700 tracking-widest">-</span>
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
                  <div class="w-2 h-2 rounded-full shrink-0" :style="{ backgroundColor: (item as any).columnColor }"></div>
                  <div class="font-bold text-slate-800 dark:text-slate-200 text-base md:text-sm hover:text-indigo-600 dark:hover:text-indigo-400 cursor-pointer transition-colors leading-tight" @click="itemToEdit = item">
                    {{ item.title }}
                  </div>
                </div>
                
                <div class="flex flex-wrap items-center gap-3 mt-3">
                  <span class="badge-outline"
                        :style="{ color: (item as any).columnColor, borderColor: (item as any).columnColor + '40', backgroundColor: (item as any).columnColor + '10' }">
                    {{ (item as any).columnName }}
                  </span>
                  <span class="badge"
                        :class="priorityClass[item.priority as 0 | 1 | 2]">
                    {{ priorityLabel[item.priority as 0 | 1 | 2] }}
                  </span>
                  <div v-if="item.assignedTo" class="flex items-center gap-1.5 text-xs md:text-xxs font-bold text-slate-600 dark:text-slate-400">
                    <User :size="12" />
                    {{ item.assignedTo }}
                  </div>
                  <div v-if="item.dueDate" class="flex items-center gap-1.5 text-xs md:text-xxs font-bold text-slate-500 dark:text-slate-400">
                    <Calendar :size="12" />
                    {{ new Date(item.dueDate).toLocaleDateString(locale === 'sl' ? 'sl-SI' : 'en-US') }}
                  </div>
                </div>
              </div>

              <div class="flex items-center gap-1 shrink-0">
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
    <div class="flex flex-row gap-4 md:gap-6 overflow-x-auto pb-4 custom-scrollbar items-stretch min-h-[calc(100vh-250px)]">
      <div v-for="column in columnStore.columns" :key="column.id" class="flex flex-col shrink-0 w-70 md:w-87.5">
        <div class="flex justify-between items-center mb-4 px-1">
          <div class="flex items-center gap-2">
            <div class="w-2 h-2 rounded-full" :style="{ backgroundColor: column.color }"></div>
            <h3 class="font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider text-xs md:text-sm">{{ column.name }}</h3>
          </div>
          <span class="bg-slate-200 dark:bg-slate-800 text-slate-600 dark:text-slate-400 px-2 py-0.5 rounded-full text-xxs font-bold">
            {{ getItemsForColumn(column.id).value.length }}
          </span>
        </div>
        
        <div 
          class="bg-slate-100/50 dark:bg-slate-900/50 border border-slate-200 dark:border-slate-800 p-2 md:p-3 rounded-2xl min-h-50 md:min-h-150 flex flex-col flex-1 transition-colors"
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
