import { defineStore } from 'pinia';
import api from '../services/api';

export interface Note {
    id: number;
    title: string;
    content: string;
    username: string;
    createdAt: string;
    updatedAt: string;
    order: number;
}

export const useNoteStore = defineStore('notes', {
    state: () => ({
        notes: [] as Note[],
        selectedNoteId: null as number | null,
        loading: false,
        saving: false,
        error: null as string | null,
    }),
    getters: {
        selectedNote(state): Note | null {
            if (state.selectedNoteId === null) return null;
            return state.notes.find(n => n.id === state.selectedNoteId) ?? null;
        },
        sortedNotes(state): Note[] {
            return [...state.notes].sort((a, b) => {
                if (a.order !== b.order) return a.order - b.order;
                return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime();
            });
        }
    },
    actions: {
        async fetchNotes() {
            this.loading = true;
            try {
                const response = await api.get('/notes');
                this.notes = response.data as Note[];
                if (this.notes.length > 0 && this.selectedNoteId === null) {
                    this.selectedNoteId = this.notes[0].id;
                }
            } catch (err: any) {
                this.error = 'Failed to fetch notes';
            } finally {
                this.loading = false;
            }
        },
        selectNote(id: number | null) {
            this.selectedNoteId = id;
        },
        async createNote() {
            try {
                const response = await api.post('/notes', {
                    title: 'Nova beležka',
                    content: ''
                });
                const note = response.data as Note;
                this.notes.unshift(note);
                this.selectedNoteId = note.id;
                return note;
            } catch (err: any) {
                this.error = 'Failed to create note';
                throw err;
            }
        },
        async updateNote(id: number, title: string, content: string) {
            this.saving = true;
            try {
                await api.put(`/notes/${id}`, { title, content });
                const index = this.notes.findIndex(n => n.id === id);
                if (index !== -1) {
                    this.notes[index] = {
                        ...this.notes[index],
                        title,
                        content,
                        updatedAt: new Date().toISOString()
                    };
                }
            } catch (err: any) {
                this.error = 'Failed to update note';
            } finally {
                this.saving = false;
            }
        },
        async deleteNote(id: number) {
            try {
                await api.delete(`/notes/${id}`);
                this.notes = this.notes.filter(n => n.id !== id);
                if (this.selectedNoteId === id) {
                    this.selectedNoteId = this.notes.length > 0 ? this.notes[0].id : null;
                }
            } catch (err: any) {
                this.error = 'Failed to delete note';
                throw err;
            }
        }
    }
});
