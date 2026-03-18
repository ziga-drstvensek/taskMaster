<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';
import { useBacklogStore } from '../store/backlog';
import { useColumnStore } from '../store/column';
import { useAuthStore } from '../store/auth';
import { useUIStore } from '../store/ui';
import BacklogItemComponent from './BacklogItem.vue';
import draggable from 'vuedraggable';

const backlogStore = useBacklogStore();
const columnStore = useColumnStore();
const uiStore = useUIStore();

onMounted(async () => {
  if (!backlogStore.boards.length) {
    await backlogStore.fetchBoards();
  }
  await columnStore.fetchColumns(backlogStore.selectedBoardId);
});

watch(() => backlogStore.selectedBoardId, async (newBoardId) => {
  await columnStore.fetchColumns(newBoardId);
});

const props = defineProps<{
  disabled?: boolean
}>();

const authStore = useAuthStore();

const isModalActive = computed(() => uiStore.isModalOpen);

const getItemsForColumn = (columnId: number) => {
  return computed({
    get: () => {
      let items = backlogStore.items.filter(i => i.columnId === columnId && !i.parentId);

      if (backlogStore.selectedSprintId !== null && backlogStore.selectedSprintId !== undefined) {
        items = items.filter(i => i.sprintId === backlogStore.selectedSprintId);
      }

      if (backlogStore.selectedDashboardId === 'me') {
        items = items.filter(i => i.assignedTo === authStore.user?.username);
      } else if (backlogStore.selectedDashboardId === 'unassigned') {
        items = items.filter(i => !i.assignedTo);
      }

      return items;
    },
    set: (val) => updateItems(val, columnId)
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
</script>

<template>
  <div class="flex flex-row gap-6 overflow-x-auto pb-4 custom-scrollbar items-stretch min-h-[calc(100vh-250px)]">
    <div v-for="column in columnStore.columns" :key="column.id" class="flex flex-col flex-shrink-0 w-[350px]">
      <div class="flex justify-between items-center mb-4 px-1">
        <div class="flex items-center gap-2">
          <div class="w-2 h-2 rounded-full" :style="{ backgroundColor: column.color }"></div>
          <h3 class="font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider text-sm">{{ column.name }}</h3>
        </div>
        <span class="bg-slate-200 dark:bg-slate-800 text-slate-600 dark:text-slate-400 px-2 py-0.5 rounded-full text-[10px] font-bold">
          {{ getItemsForColumn(column.id).value.length }}
        </span>
      </div>
      
      <div 
        class="bg-slate-100/50 dark:bg-slate-900/50 border border-slate-200 dark:border-slate-800 p-3 rounded-2xl min-h-[200px] md:min-h-[600px] flex flex-col flex-1 transition-colors"
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
</template>
