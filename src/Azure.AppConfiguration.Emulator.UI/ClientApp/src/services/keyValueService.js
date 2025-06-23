import axios from 'axios';
const API_BASE_URL = '';
export const keyValueService = {
    getKeyValues: async (keyFilter, labelFilter) => {
        try {
            const response = await axios.get(`${API_BASE_URL}/kv`, {
                params: { key: keyFilter, label: labelFilter }
            });
            return response.data.items;
        }
        catch (error) {
            console.error('Error fetching key values:', error);
            return [];
        }
    },
    getKeyValue: async (key, label) => {
        try {
            const response = await axios.get(`${API_BASE_URL}/kv/${encodeURIComponent(key)}`, {
                params: { label }
            });
            return response.data;
        }
        catch (error) {
            console.error('Error fetching key value:', error);
            return null;
        }
    },
    createOrUpdateKeyValue: async (key, request, label) => {
        try {
            const response = await axios.put(`${API_BASE_URL}/kv/${encodeURIComponent(key)}`, request, {
                params: { label }
            });
            return response.data;
        }
        catch (error) {
            console.error('Error saving key value:', error);
            return null;
        }
    },
    deleteKeyValue: async (key, label) => {
        try {
            await axios.delete(`${API_BASE_URL}/kv/${encodeURIComponent(key)}`, {
                params: { label }
            });
            return true;
        }
        catch (error) {
            console.error('Error deleting key value:', error);
            return false;
        }
    },
    getKeys: async (nameFilter) => {
        try {
            const response = await axios.get(`${API_BASE_URL}/keys`, {
                params: { name: nameFilter }
            });
            return response.data;
        }
        catch (error) {
            console.error('Error fetching keys:', error);
            return [];
        }
    }
};
