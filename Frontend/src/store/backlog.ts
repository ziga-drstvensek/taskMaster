import { defineStore } from 'pinia';
import api from '../services/api';
import type { BacklogItem, Board } from '../types';
import { signalRService } from '../services/signalr';

export const useBacklogStore = defineStore('backlog', {
    state: () => ({
        items: [] as BacklogItem[],
        boards: [] as Board[],
        selectedBoardId: localStorage.getItem('selectedBoardId') !== null ? (localStorage.getItem('selectedBoardId') === 'null' ? null : Number(localStorage.getItem('selectedBoardId'))) : null as number | null,
        loading: false,
        error: null as string | null,
        selectedSprintId: null as number | null,
        selectedDashboardId: localStorage.getItem('selectedDashboardId') || 'all',
        sprints: [] as Sprint[],
        searchQuery: ''
    }),
    actions: {
        setSearchQuery(query: string) {
            this.searchQuery = query;
        },
        setSelectedDashboardId(id: string) {
            this.selectedDashboardId = id;
            localStorage.setItem('selectedDashboardId', id);
        },
        async fetchSprints() {
            try {
                const params: any = {};
                if (this.selectedBoardId !== null && this.selectedBoardId !== -1) {
                    params.boardId = this.selectedBoardId;
                }
                const response = await api.get('/sprints', { params });
                this.sprints = response.data;
            } catch (err) {
                console.error('Failed to fetch sprints');
            }
        },
        async fetchBoards() {
            try {
                const response = await api.get('/boards');
                this.boards = response.data;
                
                if (this.selectedBoardId !== null && this.selectedBoardId !== -1) {
                    const exists = this.boards.some(b => b.id === this.selectedBoardId);
                    if (!exists) {
                        this.selectedBoardId = this.boards.length > 0 ? this.boards[this.boards.length - 1].id : null;
                    }
                } else if (this.boards.length > 0 && this.selectedBoardId === null) {
                    this.selectedBoardId = this.boards[this.boards.length - 1].id;
                }

                if (this.selectedBoardId === null) {
                    localStorage.removeItem('selectedBoardId');
                } else {
                    localStorage.setItem('selectedBoardId', this.selectedBoardId.toString());
                }
            } catch (err) {
                console.error('Failed to fetch boards');
            }
        },
        setSelectedBoardId(id: number | null) {
            this.selectedBoardId = id;
            if (id === null) {
                localStorage.removeItem('selectedBoardId');
            } else {
                localStorage.setItem('selectedBoardId', id.toString());
            }
            this.fetchItems();
            this.fetchSprints();
        },
        async fetchItems() {
            this.loading = true;
            try {
                const params: any = {};
                if (this.selectedBoardId !== null && this.selectedBoardId !== -1) {
                    params.boardId = this.selectedBoardId;
                }
                const response = await api.get('/backlog', { params });
                this.items = response.data as BacklogItem[];
            } catch (err: any) {
                this.error = 'Failed to fetch items';
            } finally {
                this.loading = false;
            }
        },
        async fetchItem(id: number) {
            try {
                const response = await api.get(`/backlog/${id}`);
                const item = response.data as BacklogItem;
                const index = this.items.findIndex(i => i.id === id);
                if (index !== -1) {
                    this.items[index] = item;
                }
                return item;
            } catch (err: any) {
                console.error(`Failed to fetch item ${id}`);
                throw err;
            }
        },
        initSignalR() {
            signalRService.start();
            signalRService.onItemsUpdated(() => {
                this.fetchItems();
            });
            signalRService.onSprintsUpdated(() => {
                this.fetchSprints();
            });
        },
        setSelectedSprintId(id: number | null) {
            this.selectedSprintId = id;
        },
        async addItem(item: any) {
            try {
                if (this.selectedBoardId !== null && this.selectedBoardId !== -1 && !item.boardId) {
                    item.boardId = this.selectedBoardId;
                }
                
                // Če manjka columnId, poskusimo dobiti prvi stolpec iz columnStore
                if (!item.columnId || item.columnId === 0) {
                    const columnStore = (await import('./column')).useColumnStore();
                    if (columnStore.columns.length > 0) {
                        item.columnId = columnStore.columns[0].id;
                    }
                }

                const response = await api.post('/backlog', item);
                this.items.push(response.data);
                this.items.sort((a, b) => a.order - b.order);
            } catch (err: any) {
                this.error = 'Failed to add item';
                throw err;
            }
        },
        async updateItem(id: number, item: any) {
            try {
                const response = await api.put(`/backlog/${id}`, item);
                const index = this.items.findIndex(i => i.id === id);
                if (index !== -1) {
                    await this.fetchItems();
                }
            } catch (err: any) {
                this.error = 'Failed to update item';
                throw err;
            }
        },
        async deleteItem(id: number) {
            try {
                await api.delete(`/backlog/${id}`);
                this.items = this.items.filter(i => i.id !== id);
            } catch (err: any) {
                this.error = 'Failed to delete item';
                throw err;
            }
        },
        async reorderItems(newItems: BacklogItem[]) {
            const oldItems = [...this.items];
            this.items = newItems.map((item, index) => ({ ...item, order: index + 1 }));
            
            try {
                const reorderData = this.items.map(item => ({
                    id: item.id, 
                    order: item.order,
                    columnId: item.columnId
                }));
                await api.post('/backlog/reorder', reorderData);
            } catch (err: any) {
                this.items = oldItems;
                this.error = 'Failed to reorder items';
            }
        }
    }
});
