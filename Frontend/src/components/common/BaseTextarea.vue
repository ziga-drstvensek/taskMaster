<script setup lang="ts">
defineProps<{
  modelValue: string;
  label?: string;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  rows?: number;
  error?: string;
}>();

defineEmits(['update:modelValue']);
</script>

<template>
  <div class="w-full">
    <label v-if="label" class="block text-[10px] font-black uppercase text-slate-400 dark:text-slate-500 mb-1.5 ml-1 tracking-widest">
      {{ label }} <span v-if="required" class="text-rose-500">*</span>
    </label>
    <textarea
      :value="modelValue"
      @input="$emit('update:modelValue', ($event.target as HTMLTextAreaElement).value)"
      :placeholder="placeholder"
      :required="required"
      :disabled="disabled"
      :rows="rows || 3"
      class="w-full px-4 py-2.5 border border-slate-200 dark:border-slate-700 rounded-2xl outline-none focus:ring-2 focus:ring-indigo-500/20 dark:focus:ring-indigo-600/20 focus:border-indigo-500 dark:focus:border-indigo-500 bg-white dark:bg-slate-800 dark:text-slate-100 transition-all disabled:bg-slate-50 dark:disabled:bg-slate-900 disabled:text-slate-400 dark:disabled:text-slate-600 resize-none"
      :class="{ 'border-rose-500 focus:ring-rose-500': error }"
    ></textarea>
    <p v-if="error" class="mt-1 text-xs text-rose-500 ml-1">{{ error }}</p>
  </div>
</template>
