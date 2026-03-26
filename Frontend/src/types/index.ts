export enum BacklogItemPriority {
    Low = 0,
    Medium = 1,
    High = 2
}

export enum BacklogItemStatus {
    Backlog = 0,
    InProgress = 1,
    Done = 2
}

export interface BacklogItem {
    id: number;
    title: string;
    description?: string;
    columnId: number;
    boardId?: number;
    priority: BacklogItemPriority;
    order: number;
    createdAt: string;
    updatedAt: string;
    sprintId?: number;
    sprintName?: string;
    createdBy: string;
    assignedTo?: string;
    commentsCount: number;
    dueDate?: string;
    parentId?: number;
    subtasks?: BacklogItem[];
    attachments?: Attachment[];
    history?: HistoryEntry[];
}

export interface HistoryEntry {
    id: number;
    backlogItemId: number;
    changeType: string;
    description?: string;
    changedBy: string;
    changedAt: string;
}

export interface Attachment {
    id: number;
    backlogItemId: number;
    fileName: string;
    contentType: string;
    size: number;
    createdAt: string;
}

export interface Board {
    id: number;
    name: string;
    description?: string;
    createdAt: string;
    usernames: string[];
    columns: BoardColumn[];
}

export interface BoardColumn {
    id: number;
    name: string;
    order: number;
    color: string;
    boardId?: number;
}

export interface Sprint {
    id: number;
    name: string;
    startDate: string;
    endDate: string;
    isActive: boolean;
    boardId?: number | null;
}

export interface Comment {
    id: number;
    backlogItemId: number;
    content: string;
    author: string;
    createdAt: string;
}

export interface User {
    username: string;
    role: string;
    token: string;
    profilePicture?: string;
    teamsWebhookUrl?: string;
}

export interface UserDetails {
    username: string;
    email: string;
    role: string;
    tags?: string;
    profilePicture?: string;
    teamsWebhookUrl?: string;
}

export interface Notification {
    id: number;
    title: string;
    message: string;
    link?: string;
    isRead: boolean;
    createdAt: string;
    type?: string;
}
