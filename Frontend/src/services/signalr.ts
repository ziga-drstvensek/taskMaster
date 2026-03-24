import * as signalR from '@microsoft/signalr';

class SignalRService {
    private connection: signalR.HubConnection | null = null;
    private itemsCallbacks: (() => void)[] = [];
    private sprintsCallbacks: (() => void)[] = [];

    constructor() {
        const isElectron = !!(window && (window as any).electron);
        const hubUrl = isElectron 
            ? (window as any).electron.env.BACKEND_URL + '/backlogHub' 
            : (import.meta.env.VITE_HUB_URL || '/backlogHub');

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl, {
                transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
                skipNegotiation: false
            })
            .withAutomaticReconnect()
            .build();

        this.connection.on('ItemsUpdated', () => {
            console.log('SignalR: ItemsUpdated received');
            this.itemsCallbacks.forEach(cb => cb());
        });

        this.connection.on('SprintsUpdated', () => {
            console.log('SignalR: SprintsUpdated received');
            this.sprintsCallbacks.forEach(cb => cb());
        });
    }

    async start() {
        try {
            if (this.connection?.state === signalR.HubConnectionState.Disconnected) {
                await this.connection.start();
                console.log('SignalR connected');
            }
        } catch (err) {
            console.error('SignalR connection error: ', err);
            setTimeout(() => this.start(), 5000);
        }
    }

    onItemsUpdated(callback: () => void) {
        this.itemsCallbacks.push(callback);
    }

    onSprintsUpdated(callback: () => void) {
        this.sprintsCallbacks.push(callback);
    }
}

export const signalRService = new SignalRService();
