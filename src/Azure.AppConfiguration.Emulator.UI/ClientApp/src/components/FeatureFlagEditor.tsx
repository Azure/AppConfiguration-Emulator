import { useState, useEffect } from 'react';
import { keyValueService } from '../services/keyValueService';
import { KeyValue } from '../models/keyValue';
import { 
  createFeatureFlagKey,
  getFeatureFlagName,
  parseFeatureFlagEnabled,
  createFeatureFlagValue,
  FEATURE_FLAG_CONTENT_TYPE
} from '../utils/featureFlagUtils';
import JsonEditor from './JsonEditor';

interface Props {
  mode: 'create' | 'edit';
  keyValue?: KeyValue | null;
  onBack: () => void;
}

export default function FeatureFlagEditor({ mode, keyValue, onBack }: Props) {
  const [name, setName] = useState('');
  const [label, setLabel] = useState('');
  const [enabled, setEnabled] = useState(false);
  const [tags, setTags] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [jsonValue, setJsonValue] = useState('');
  const [isJsonEditorOpen, setIsJsonEditorOpen] = useState(false);
  const [useAdvancedMode, setUseAdvancedMode] = useState(false);
  
  // For tag editing
  const [tagKey, setTagKey] = useState('');
  const [tagValue, setTagValue] = useState('');

  useEffect(() => {
    if (mode === 'edit' && keyValue) {
      setName(getFeatureFlagName(keyValue.key));
      setLabel(keyValue.label || '');
      setEnabled(parseFeatureFlagEnabled(keyValue.value || ''));
      setJsonValue(keyValue.value || '');
      
      // Fetch the full key value to get all properties including tags
      const fetchFullKeyValue = async () => {
        const fullKeyValue = await keyValueService.getKeyValue(keyValue.key, keyValue.label);
        if (fullKeyValue) {
          setTags(fullKeyValue.tags || {});
        }
      };
      
      fetchFullKeyValue();
      
      // Check if this is an advanced feature flag (has conditions)
      try {
        const parsed = JSON.parse(keyValue.value || '{}');
        setUseAdvancedMode(!!parsed.conditions);
      } catch {
        setUseAdvancedMode(false);
      }
    } else if (mode === 'create') {
      // Reset form for create mode
      setName('');
      setLabel('');
      setEnabled(false);
      setTags({});
      setJsonValue('');
      setUseAdvancedMode(false);
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
    
    if (!name.trim()) {
      setError('Feature flag name is required');
      return;
    }
    
    setSaving(true);
    setError(null);
    
    try {
      const key = mode === 'create' ? createFeatureFlagKey(name.trim()) : keyValue!.key;
      
      // Use JSON value if in advanced mode, otherwise create simple feature flag
      const value = useAdvancedMode ? jsonValue : createFeatureFlagValue(name.trim(), enabled);
      
      const result = await keyValueService.createOrUpdateKeyValue(
        key,
        {
          value,
          contentType: FEATURE_FLAG_CONTENT_TYPE,
          tags: Object.keys(tags).length > 0 ? tags : undefined
        },
        label.trim() || undefined
      );
      
      if (result) {
        onBack();
      } else {
        setError(`Failed to ${mode} feature flag`);
      }
    } catch (err) {
      setError('An error occurred while saving the feature flag');
    } finally {
      setSaving(false);
    }
  };

  const handleAdvancedEdit = () => {
    // If not in advanced mode, create a base JSON with current simple values
    if (!useAdvancedMode) {
      const baseJson = createFeatureFlagValue(name.trim(), enabled);
      setJsonValue(baseJson);
    }
    setIsJsonEditorOpen(true);
  };

  const handleJsonSave = (newJsonValue: string) => {
    // Validate JSON and required fields
    try {
      const parsed = JSON.parse(newJsonValue);
      
      // Check for required fields
      if (!parsed.id) {
        throw new Error('Field "id" is required in feature flag JSON');
      }
      
      if (parsed.enabled === undefined || parsed.enabled === null) {
        throw new Error('Field "enabled" is required in feature flag JSON');
      }
      
      // When editing an existing feature flag, prevent changing the id
      if (mode === 'edit' && parsed.id !== name) {
        throw new Error('Cannot change the feature flag ID when editing an existing feature flag');
      }
      
      // If validation passes, update the state
      setJsonValue(newJsonValue);
      setUseAdvancedMode(true);
      setEnabled(parsed.enabled === true);
      
      // If there's an id in the JSON and we're in create mode, set it as the name
      if (parsed.id && mode === 'create') {
        setName(parsed.id);
      }
      
      // Clear any previous errors
      setError(null);
    } catch (err) {
      // Set error message for invalid JSON or missing required fields
      if (err instanceof SyntaxError) {
        setError('Invalid JSON format. Please check your syntax.');
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Invalid JSON configuration');
      }
      // Don't update the state if validation fails
      return;
    }
  };

  return (
    <div className="key-value-form-container">
      <h2>{mode === 'create' ? 'Create New Feature Flag' : 'Edit Feature Flag'}</h2>
      
      {error && <div className="error-message">{error}</div>}
      
      <form onSubmit={handleSubmit} className="key-value-form">
        <div className="form-group">
          <label htmlFor="name">Name:</label>
          <input
            id="name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
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

        <div className="form-group">
          <label className="toggle-label">
            <span>Enabled State:</span>
            <label className="toggle-switch">
              <input
                type="checkbox"
                checked={enabled}
                onChange={(e) => setEnabled(e.target.checked)}
                disabled={useAdvancedMode}
              />
              <span className="toggle-slider"></span>
            </label>
          </label>
          {useAdvancedMode && (
            <small className="form-hint">
              Enabled state is controlled by the advanced JSON configuration
            </small>
          )}
        </div>

        <div className="form-group">
          <label>Advanced Configuration:</label>
          <button 
            type="button" 
            onClick={handleAdvancedEdit}
            className="advanced-edit-button"
          >
            Advanced Edit
          </button>
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

      <JsonEditor
        isOpen={isJsonEditorOpen}
        jsonValue={jsonValue}
        onSave={handleJsonSave}
        onClose={() => setIsJsonEditorOpen(false)}
      />
    </div>
  );
}
