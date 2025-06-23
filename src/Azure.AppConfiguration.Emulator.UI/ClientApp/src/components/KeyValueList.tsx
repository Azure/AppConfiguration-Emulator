import { useState, useEffect } from 'react';
import { keyValueService } from '../services/keyValueService';
import { KeyValue } from '../models/keyValue';

interface Props {
  onEdit: (keyValue: KeyValue) => void;
}

export default function KeyValueList({ onEdit }: Props) {
  const [keyValues, setKeyValues] = useState<KeyValue[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [keyFilter, setKeyFilter] = useState('');
  const [labelFilter, setLabelFilter] = useState('');

  const fetchKeyValues = async () => {
    setLoading(true);
    try {
      const data = await keyValueService.getKeyValues(keyFilter || undefined, labelFilter || undefined);
      setKeyValues(data);
      setError(null);
    } catch (err) {
      setError('Failed to load key values');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchKeyValues();
  }, []);

  const handleDelete = async (key: string, label?: string) => {
    if (window.confirm('Are you sure you want to delete this key value?')) {
      const success = await keyValueService.deleteKeyValue(key, label);
      if (success) {
        fetchKeyValues();
      }
    }
  };

  return (
    <div className="key-value-list">
      <div className="filters">
        <div className="filter-group">
          <label>Key Filter:</label>
          <input 
            type="text" 
            value={keyFilter} 
            onChange={(e) => setKeyFilter(e.target.value)} 
          />
        </div>
        
        <div className="filter-group">
          <label>Label Filter:</label>
          <input 
            type="text" 
            value={labelFilter} 
            onChange={(e) => setLabelFilter(e.target.value)} 
          />
        </div>
        
        <button onClick={fetchKeyValues} className="filter-button">
          Apply Filters
        </button>
      </div>
      
      {error && <div className="error-message">{error}</div>}
      
      {loading ? (
        <div>Loading...</div>
      ) : (
        <table className="key-value-table">
          <thead>
            <tr>
              <th>Key</th>
              <th>Label</th>
              <th>Value</th>
              <th>Content Type</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {keyValues.length === 0 ? (
              <tr>
                <td colSpan={5}>No key values found</td>
              </tr>
            ) : (
              keyValues.map((kv) => (
                <tr key={`${kv.key}-${kv.label || 'null'}`}>
                  <td>{kv.key}</td>
                  <td>{kv.label || '<null>'}</td>
                  <td>{kv.value || ''}</td>
                  <td>{kv.contentType || ''}</td>
                  <td>
                    <button onClick={() => onEdit(kv)} className="edit-button">
                      Edit
                    </button>
                    <button 
                      onClick={() => handleDelete(kv.key, kv.label)}
                      className="delete-button"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      )}
    </div>
  );
}
