import axios from 'axios';
import { KeyValue, KeyValueRequest } from '../models/keyValue';

const API_BASE_URL = '';

interface KeyValueResponse {
  items: KeyValue[];
  '@nextLink'?: string;
}

export const keyValueService = {
  getKeyValues: async (keyFilter?: string, labelFilter?: string, nextLink?: string): Promise<KeyValueResponse> => {
    try {
      let url = nextLink ? `${API_BASE_URL}${nextLink}` : `${API_BASE_URL}/kv`;
      let params: Record<string, string> = {};
      // Only add filters if we're not using a nextLink
      if (!nextLink) {
        params = { key: keyFilter, label: labelFilter };
      }
      
      const response = await axios.get(url, { params });
      return {
        items: response.data.items || [],
        '@nextLink': response.data['@nextLink']
      };
    } catch (error) {
      console.error('Error fetching key values:', error);
      return { items: [] };
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
  }
};
