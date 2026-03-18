const { contextBridge } = require('electron');

contextBridge.exposeInMainWorld('electron', {
  env: {
    BACKEND_URL: 'https://localhost:7008' // Privzeti URL za backend
  }
});
