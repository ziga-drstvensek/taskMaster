<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue';
import { X } from 'lucide-vue-next';
import { useUIStore } from '../../store/ui';

const props = defineProps<{
  show: boolean;
  title?: string;
  maxWidth?: string;
  maxHeight?: string;
  persistent?: boolean;
}>();

const emit = defineEmits(['close']);
const uiStore = useUIStore();

const close = () => {
  emit('close');
};

const handleBackdropClick = () => {
  if (!props.persistent) {
    close();
  }
};

const handleEscape = (e: KeyboardEvent) => {
  if (e.key === 'Escape' && props.show && !props.persistent) {
    close();
  }
};

onMounted(() => {
  window.addEventListener('keydown', handleEscape);
  if (props.show) {
    document.body.style.overflow = 'hidden';
    uiStore.registerModal();
  }
});

onUnmounted(() => {
  window.removeEventListener('keydown', handleEscape);
  document.body.style.overflow = '';
  if (props.show) {
    uiStore.unregisterModal();
  }
});

watch(() => props.show, (newVal, oldVal) => {
  if (newVal) {
    document.body.style.overflow = 'hidden';
    uiStore.registerModal();
  } else if (oldVal) {
    document.body.style.overflow = '';
    uiStore.unregisterModal();
  }
});
</script>

<template>
  <Transition
    enter-active-class="duration-300 ease-out"
    enter-from-class="opacity-0"
    enter-to-class="opacity-100"
    leave-active-class="duration-200 ease-in"
    leave-from-class="opacity-100"
    leave-to-class="opacity-0"
  >
    <div v-if="show" class="fixed inset-0 z-[100] flex items-center justify-center p-4 sm:p-6" aria-modal="true" role="dialog">
      <!-- Backdrop -->
      <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-md transition-opacity" @click="handleBackdropClick"></div>

      <!-- Modal Content -->
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0 scale-95 translate-y-4"
        enter-to-class="opacity-100 scale-100 translate-y-0"
        leave-active-class="duration-200 ease-in"
        leave-from-class="opacity-100 scale-100 translate-y-0"
        leave-to-class="opacity-0 scale-95 translate-y-4"
      >
        <div 
          v-if="show"
          class="relative bg-white dark:bg-slate-900 rounded-3xl shadow-2xl ring-1 ring-black/5 flex flex-col overflow-hidden w-full transition-all border border-slate-200 dark:border-slate-800"
          :style="{ maxWidth: maxWidth || '600px', maxHeight: maxHeight || '90vh' }"
          @click.stop
        >
          <!-- Header -->
          <div class="flex items-center justify-between px-6 py-4 border-b border-slate-100/50 dark:border-slate-800/50 bg-slate-50/50 dark:bg-slate-800/50 flex-shrink-0">
            <slot name="header">
              <h3 class="text-xl font-bold text-slate-800 dark:text-slate-100 tracking-tight">{{ title }}</h3>
            </slot>
            <button 
              @click="close"
              class="p-2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-200/50 dark:hover:bg-slate-700/50 rounded-xl transition-all"
              aria-label="Close"
            >
              <X :size="20" />
            </button>
          </div>

          <!-- Body -->
          <div class="flex-1 overflow-y-auto p-6 custom-scrollbar dark:text-slate-300 min-h-0">
            <slot></slot>
          </div>

          <!-- Footer -->
          <div v-if="$slots.footer" class="px-6 py-4 border-t border-slate-100/50 dark:border-slate-800/50 bg-slate-50/50 dark:bg-slate-800/50 flex justify-end gap-3 flex-shrink-0">
            <slot name="footer"></slot>
          </div>
        </div>
      </Transition>
    </div>
  </Transition>
</template>

<style scoped>
.custom-scrollbar::-webkit-scrollbar {
  width: 6px;
}
.custom-scrollbar::-webkit-scrollbar-track {
  background: transparent;
}
.custom-scrollbar::-webkit-scrollbar-thumb {
  background: #e2e8f0;
  border-radius: 10px;
}
.dark .custom-scrollbar::-webkit-scrollbar-thumb {
  background: #334155;
}
.custom-scrollbar::-webkit-scrollbar-thumb:hover {
  background: #cbd5e1;
}
.dark .custom-scrollbar::-webkit-scrollbar-thumb:hover {
  background: #475569;
}
</style>
