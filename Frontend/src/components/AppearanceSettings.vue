<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import { useUIStore } from '../store/ui';
import { Type, Check } from 'lucide-vue-next';

const { t } = useI18n();
const uiStore = useUIStore();

const fontSizes = [
  { id: 'small', name: t('common.small'), class: 'text-xs' },
  { id: 'medium', name: t('common.medium'), class: 'text-base' },
  { id: 'large', name: t('common.large'), class: 'text-lg' }
] as const;

const setFontSize = (size: 'small' | 'medium' | 'large') => {
  uiStore.setFontSize(size);
};
</script>

<template>
  <div class="space-y-6">
    <div>
      <h3 class="text-lg font-bold text-slate-800 dark:text-slate-100 mb-4 flex items-center gap-2">
        <Type :size="20" class="text-indigo-600" />
        {{ $t('common.font_size') }}
      </h3>
      
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <button
          v-for="size in fontSizes"
          :key="size.id"
          @click="setFontSize(size.id)"
          class="flex items-center justify-between p-4 rounded-2xl border-2 transition-all text-left"
          :class="uiStore.fontSize === size.id
            ? 'border-indigo-600 bg-indigo-50 dark:bg-indigo-900/20'
            : 'border-slate-100 dark:border-slate-800 hover:border-indigo-200 dark:hover:border-indigo-800 bg-white dark:bg-slate-900'"
        >
          <div class="flex flex-col">
            <span class="text-xs font-black uppercase tracking-widest text-slate-400 dark:text-slate-500 mb-1">
              {{ size.name }}
            </span>
            <span :class="size.class" class="font-medium text-slate-700 dark:text-slate-200">
              Aa Bb Cc
            </span>
          </div>
          <div 
            v-if="uiStore.fontSize === size.id"
            class="w-6 h-6 rounded-full bg-indigo-600 flex items-center justify-center text-white"
          >
            <Check :size="14" />
          </div>
        </button>
      </div>
    </div>

    <div class="p-4 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-100 dark:border-slate-700">
      <p class="text-sm text-slate-500 dark:text-slate-400 italic">
        {{ $t('common.font_size') }}: {{ $t(`common.${uiStore.fontSize}`) }}
      </p>
    </div>
  </div>
</template>
