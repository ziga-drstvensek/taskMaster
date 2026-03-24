import { defineStore } from 'pinia';

export const useUIStore = defineStore('ui', {
    state: () => ({
        activeModals: 0,
        isDarkMode: localStorage.getItem('dark-mode') === 'true',
        viewMode: (localStorage.getItem('view-mode') as 'table' | 'kanban') || 'kanban',
        fontSize: (localStorage.getItem('font-size') as 'small' | 'medium' | 'large') || 'medium'
    }),
    getters: {
        isModalOpen: (state) => state.activeModals > 0
    },
    actions: {
        setFontSize(size: 'small' | 'medium' | 'large') {
            this.fontSize = size;
            localStorage.setItem('font-size', size);
            this.applyFontSize();
        },
        applyFontSize() {
            const htmlElement = document.documentElement;
            htmlElement.classList.remove('text-small', 'text-medium', 'text-large');
            htmlElement.classList.add(`text-${this.fontSize}`);
        },
        registerModal() {
            this.activeModals++;
        },
        unregisterModal() {
            this.activeModals = Math.max(0, this.activeModals - 1);
        },
        setViewMode(mode: 'table' | 'kanban') {
            this.viewMode = mode;
            localStorage.setItem('view-mode', mode);
        },
        toggleDarkMode() {
            this.isDarkMode = !this.isDarkMode;
            localStorage.setItem('dark-mode', this.isDarkMode.toString());
            this.applyTheme();
        },
        applyTheme() {
            const htmlElement = document.documentElement;
            if (this.isDarkMode) {
                htmlElement.classList.add('dark');
                htmlElement.style.colorScheme = 'dark';
            } else {
                htmlElement.classList.remove('dark');
                htmlElement.style.colorScheme = 'light';
            }
        }
    }
});
