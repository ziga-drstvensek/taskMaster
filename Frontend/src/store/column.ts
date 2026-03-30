import { defineStore } from 'pinia';
import api from '../services/api';
import type { BoardColumn } from '@/types';

export const useColumnStore = defineStore('column', {
    state: () => ({
        columns: [] as BoardColumn[],
        loading: false,
        error: null as string | null
    }),
    actions: {
        async fetchColumns(boardId?: number | null) {
            this.loading = true;
            try {
                const params: any = {};
                if (boardId !== undefined && boardId !== null) {
                    params.boardId = boardId;
                }
                const response = await api.get('/columns', { params });
                this.columns = response.data;
                this.columns.sort((a, b) => a.order - b.order);
            } catch (err: any) {
                this.error = 'Failed to fetch columns';
            } finally {
                this.loading = false;
            }
        },
        async addColumn(column: any) {
            try {
                const response = await api.post('/columns', column);
                this.columns.push(response.data);
                this.columns.sort((a, b) => a.order - b.order);
            } catch (err: any) {
                this.error = 'Failed to add column';
                throw err;
            }
        },
        async updateColumn(id: number, column: any) {
            try {
                await api.put(`/columns/${id}`, column);
                await this.fetchColumns(column?.boardId);
            } catch (err: any) {
                this.error = 'Failed to update column';
                throw err;
            }
        },
        async deleteColumn(id: number) {
            try {
                await api.delete(`/columns/${id}`);
                this.columns = this.columns.filter(c => c.id !== id);
            } catch (err: any) {
                this.error = 'Failed to delete column';
                throw err;
            }
        }
    }
});
