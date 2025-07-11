import axios from 'axios';
import { KeyValue, KeyValueRequest, KeyValueRevision } from '../models/keyValue';

const API_BASE_URL = '';

export const keyValueService = {
  getKeyValues: async (keyFilter?: string, labelFilter?: string): Promise<KeyValue[]> => {
    try {
      const response = await axios.get(`${API_BASE_URL}/kv`, {
        params: { key: keyFilter, label: labelFilter }
      });
      return response.data.items;
    } catch (error) {
      console.error('Error fetching key values:', error);
      return [];
    }
  },

  getKeyValue: async (key: string, label?: string): Promise<KeyValue | null> => {
    try {
      const response = await axios.get(`${API_BASE_URL}/kv/${encodeURIComponent(key)}`, {
        params: { label }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching key value:', error);
      return null;
    }
  },

  createOrUpdateKeyValue: async (key: string, request: KeyValueRequest, label?: string): Promise<KeyValue | null> => {
    try {
      const response = await axios.put(`${API_BASE_URL}/kv/${encodeURIComponent(key)}`, request, {
        params: { label }
      });
      return response.data;
    } catch (error) {
      console.error('Error saving key value:', error);
      return null;
    }
  },

  deleteKeyValue: async (kv: KeyValue): Promise<boolean> => {
    try {
      await axios.delete(`${API_BASE_URL}/kv/${encodeURIComponent(kv.key)}`, {
        params: { label: kv.label }
      });
      return true;
    } catch (error) {
      console.error('Error deleting key value:', error);
      return false;
    }
  },

  getKeys: async (nameFilter?: string): Promise<string[]> => {
    try {
      const response = await axios.get(`${API_BASE_URL}/keys`, {
        params: { name: nameFilter }
      });
      return response.data;
    } catch (error) {
      console.error('Error fetching keys:', error);
      return [];
    }
  },

  getKeyValueRevisions: async (key: string, label?: string): Promise<KeyValueRevision[]> => {
    try {
      const response = await axios.get(`${API_BASE_URL}/revisions`, {
        params: { key, label }
      });
      return response.data.items;
    } catch (error) {
      console.error('Error fetching key value revisions:', error);
      return [];
    }
  }
};
