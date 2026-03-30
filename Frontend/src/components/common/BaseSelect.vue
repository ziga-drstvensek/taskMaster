<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue';
import { ChevronDown, Check, Search } from 'lucide-vue-next';

const props = defineProps<{
  modelValue: string | number;
  label?: string;
  options: { value: string | number; label: string }[];
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  error?: string;
  searchable?: boolean;
}>();

const emit = defineEmits(['update:modelValue']);

const isOpen = ref(false);
const searchQuery = ref('');
const dropdownRef = ref<HTMLElement | null>(null);
const searchInputRef = ref<HTMLInputElement | null>(null);

const filteredOptions = computed(() => {
  if (!props.searchable || !searchQuery.value) return props.options;
  const query = searchQuery.value.toLowerCase();
  return props.options.filter(opt => 
    opt.label.toLowerCase().includes(query)
  );
});

const selectedOption = computed(() => {
  return props.options.find(opt => String(opt.value) === String(props.modelValue));
});

const displayValue = computed(() => {
  return selectedOption.value?.label || props.placeholder || '';
});

const selectOption = (opt: { value: string | number; label: string }) => {
  emit('update:modelValue', opt.value);
  isOpen.value = false;
  searchQuery.value = '';
};

const toggleDropdown = async () => {
  if (!props.disabled) {
    isOpen.value = !isOpen.value;
    if (isOpen.value && props.searchable) {
      searchQuery.value = '';
      await nextTick();
      searchInputRef.value?.focus();
    }
  }
};

const handleClickOutside = (event: MouseEvent) => {
  if (isOpen.value && dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
    isOpen.value = false;
  }
};

onMounted(() => {
  document.addEventListener('mousedown', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('mousedown', handleClickOutside);
});
</script>

<template>
  <div class="w-full" ref="dropdownRef">
    <label v-if="label" class="label-caps">
      {{ label }} <span v-if="required" class="text-rose-500">*</span>
    </label>
    
    <div class="relative">
      <!-- Trigger button -->
      <button
        type="button"
        @click="toggleDropdown"
        :disabled="disabled"
        class="input-field text-left flex items-center justify-between gap-2"
        :class="[
          error 
            ? 'border-rose-400 focus:ring-2 focus:ring-rose-500/20 focus:border-rose-500' 
            : isOpen 
              ? 'border-indigo-400 dark:border-indigo-500 ring-2 ring-indigo-500/20' 
              : '',
          disabled ? 'bg-slate-50 dark:bg-slate-900 text-slate-400 dark:text-slate-600 cursor-not-allowed' : 'cursor-pointer'
        ]"
      >
        <span 
          class="truncate text-sm"
          :class="selectedOption ? 'text-slate-700 dark:text-slate-200 font-medium' : 'text-slate-400 dark:text-slate-500'"
        >
          {{ displayValue }}
        </span>
        <ChevronDown 
          :size="18" 
          class="shrink-0 text-slate-400 transition-transform duration-200"
          :class="{ 'rotate-180': isOpen }"
        />
      </button>
      
      <!-- Dropdown menu -->
      <Transition
        enter-active-class="transition duration-150 ease-out"
        enter-from-class="opacity-0 scale-95 -translate-y-1"
        enter-to-class="opacity-100 scale-100 translate-y-0"
        leave-active-class="transition duration-100 ease-in"
        leave-from-class="opacity-100 scale-100 translate-y-0"
        leave-to-class="opacity-0 scale-95 -translate-y-1"
      >
        <div 
          v-if="isOpen"
          class="absolute z-50 w-full mt-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg shadow-lg overflow-hidden flex flex-col"
        >
          <!-- Search input -->
          <div v-if="searchable" class="p-2 border-b border-slate-100 dark:border-slate-700">
            <div class="relative">
              <Search :size="14" class="absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                ref="searchInputRef"
                v-model="searchQuery"
                type="text"
                class="w-full pl-8 pr-3 py-1.5 text-sm bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-md focus:outline-none focus:ring-1 focus:ring-indigo-500/50 focus:border-indigo-500 placeholder:text-slate-400 dark:placeholder:text-slate-600"
                :placeholder="$t('common.search_placeholder')"
                @click.stop
              />
            </div>
          </div>

          <ul class="py-1 max-h-60 overflow-auto custom-scrollbar">
            <!-- Empty state -->
            <li
              v-if="filteredOptions.length === 0"
              class="px-4 py-3 text-center text-sm text-slate-400 dark:text-slate-500 italic"
            >
              {{ $t('common.no_items') }}
            </li>
            <li
              v-for="opt in filteredOptions"
              :key="opt.value"
              @click="selectOption(opt)"
              class="px-4 py-2.5 cursor-pointer flex items-center justify-between gap-2 transition-colors duration-100"
              :class="[
                String(opt.value) === String(modelValue)
                  ? 'bg-indigo-50 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-300'
                  : 'text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700'
              ]"
            >
              <span class="text-sm font-medium truncate">{{ opt.label }}</span>
              <Check 
                v-if="String(opt.value) === String(modelValue)" 
                :size="16" 
                class="shrink-0 text-indigo-600 dark:text-indigo-400"
              />
            </li>
          </ul>
        </div>
      </Transition>
    </div>
    
    <p v-if="error" class="mt-1 text-xs text-rose-500 ml-1">{{ error }}</p>
  </div>
</template>
