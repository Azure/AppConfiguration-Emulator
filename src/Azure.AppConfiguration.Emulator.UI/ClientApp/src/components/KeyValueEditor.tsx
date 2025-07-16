import { useState, useEffect } from 'react';
import { keyValueService } from '../services/keyValueService';
import { KeyValue, KeyValueRequest } from '../models/keyValue';

interface Props {
  mode: 'create' | 'edit';
  keyValue?: KeyValue | null;
  onBack: () => void;
}

export default function KeyValueEditor({ mode, keyValue, onBack }: Props) {
  const [key, setKey] = useState('');
  const [label, setLabel] = useState('');
  const [value, setValue] = useState('');
  const [contentType, setContentType] = useState('');
  const [tags, setTags] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // For tag editing
  const [tagKey, setTagKey] = useState('');
  const [tagValue, setTagValue] = useState('');

  useEffect(() => {
    if (mode === 'edit' && keyValue) {
      setKey(keyValue.key);
      setLabel(keyValue.label || '');
      
      // Fetch the full key value to get all properties
      const fetchFullKeyValue = async () => {
        setLoading(true);
        const fullKeyValue = await keyValueService.getKeyValue(keyValue.key, keyValue.label);
        if (fullKeyValue) {
          setValue(fullKeyValue.value || '');
          setContentType(fullKeyValue.content_type || '');
          setTags(fullKeyValue.tags || {});
        }
        setLoading(false);
      };
      
      fetchFullKeyValue();
    } else if (mode === 'create') {
      // Reset form for create mode
      setKey('');
      setLabel('');
      setValue('');
      setContentType('');
      setTags({});
    }
  }, [mode, keyValue]);

  const handleAddTag = () => {
    if (!tagKey.trim()) return;
    
    setTags(prev => ({
      ...prev,
      [tagKey]: tagValue
    }));
    
    setTagKey('');
    setTagValue('');
  };

  const handleRemoveTag = (key: string) => {
    setTags(prev => {
      const newTags = { ...prev };
      delete newTags[key];
      return newTags;
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!key) {
      setError('Key is required');
      return;
    }
    
    setSaving(true);
    
    const request: KeyValueRequest = {
      value,
      content_type: contentType || undefined,
      tags: Object.keys(tags).length > 0 ? tags : undefined
    };
    
    try {
      await keyValueService.createOrUpdateKeyValue(key, request, label || undefined);
      onBack();
    } catch (err) {
      setError('Failed to save key value');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return <div>Loading...</div>;
  }

  return (
    <div className="key-value-form-container">
      <h2>{mode === 'create' ? 'Create New Key Value' : 'Edit Key Value'}</h2>
      
      {error && <div className="error-message">{error}</div>}
      
      <form onSubmit={handleSubmit} className="key-value-form">
        <div className="form-group">
          <label htmlFor="key">Key:</label>
          <input
            id="key"
            type="text"
            value={key}
            onChange={(e) => setKey(e.target.value)}
            disabled={mode === 'edit'}
            required
          />
        </div>
        
        <div className="form-group">
          <label htmlFor="label">Label:</label>
          <input
            id="label"
            type="text"
            value={label}
            onChange={(e) => setLabel(e.target.value)}
            disabled={mode === 'edit'}
          />
        </div>
        
        <div className="form-group">
          <label htmlFor="value">Value:</label>
          <textarea
            id="value"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            rows={4}
          />
        </div>
        
        <div className="form-group">
          <label htmlFor="contentType">Content Type:</label>
          <input
            id="contentType"
            type="text"
            value={contentType}
            onChange={(e) => setContentType(e.target.value)}
          />
        </div>
        
        <div className="form-group">
          <label>Tags:</label>
          
          <div className="tags-container">
            {Object.entries(tags).map(([k, v]) => (
              <div key={k} className="tag">
                <span>{k}: {v}</span>
                <button
                  type="button"
                  onClick={() => handleRemoveTag(k)}
                  className="remove-tag"
                >
                  ×
                </button>
              </div>
            ))}
          </div>
          
          <div className="add-tag">
            <input
              type="text"
              placeholder="Tag Key"
              value={tagKey}
              onChange={(e) => setTagKey(e.target.value)}
            />
            <input
              type="text"
              placeholder="Tag Value"
              value={tagValue}
              onChange={(e) => setTagValue(e.target.value)}
            />
            <button
              type="button"
              onClick={handleAddTag}
              className="add-tag-button"
            >
              Add Tag
            </button>
          </div>
        </div>
        
        <div className="form-actions">
          <button type="button" onClick={onBack} className="cancel-button">
            Cancel
          </button>
          <button type="submit" disabled={saving} className="save-button">
            {saving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </form>
    </div>
  );
}
