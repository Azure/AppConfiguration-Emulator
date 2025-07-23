import { useState, useEffect } from 'react';
import { keyValueService } from '../services/keyValueService';
import { KeyValue } from '../models/keyValue';
import { 
  FEATURE_FLAG_PREFIX, 
  isFeatureFlag, 
  getFeatureFlagName, 
  parseFeatureFlagEnabled,
  createFeatureFlagValue,
  FEATURE_FLAG_CONTENT_TYPE
} from '../utils/featureFlagUtils';
import { renderTags } from '../utils/tagUtils';

interface Props {
  onCreate: () => void;
  onEdit: (keyValue: KeyValue) => void;
}

export default function FeatureFlagList({ onCreate, onEdit }: Props) {
  const [featureFlags, setFeatureFlags] = useState<KeyValue[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchFeatureFlags = async () => {
    setLoading(true);
    try {
      // Get all key-values and filter for feature flags client-side
      const response = await keyValueService.getKeyValues();
      const flags = response.items.filter(isFeatureFlag);
      setFeatureFlags(flags);
      setError(null);
    } catch (err) {
      setError('Failed to load feature flags');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchFeatureFlags();
  }, []);

  const handleToggleEnabled = async (flag: KeyValue) => {
    try {
      const currentEnabled = parseFeatureFlagEnabled(flag.value || '');
      const newEnabled = !currentEnabled;
      const flagName = getFeatureFlagName(flag.key);
      
      const newValue = createFeatureFlagValue(flagName, newEnabled);
      
      const result = await keyValueService.createOrUpdateKeyValue(
        flag.key, 
        { 
          value: newValue, 
          contentType: FEATURE_FLAG_CONTENT_TYPE 
        }, 
        flag.label
      );
      
      if (result) {
        // Update local state
        setFeatureFlags(prev => 
          prev.map(f => f.key === flag.key && f.label === flag.label ? result : f)
        );
      } else {
        setError('Failed to update feature flag');
      }
    } catch (err) {
      setError('Failed to toggle feature flag');
    }
  };

  const handleDelete = async (flag: KeyValue) => {
    const flagName = getFeatureFlagName(flag.key);
    if (window.confirm(`Are you sure you want to delete feature flag "${flagName}"?`)) {
      const success = await keyValueService.deleteKeyValue(flag);
      if (success) {
        fetchFeatureFlags();
      } else {
        setError('Failed to delete feature flag');
      }
    }
  };

  if (loading) {
    return <div>Loading feature flags...</div>;
  }

  return (
    <div className="key-value-list">
      <div className="feature-flag-header">
        <h2>Feature Flags</h2>
        <button onClick={onCreate} className="create-button">
          Create Feature Flag
        </button>
      </div>

      {error && <div className="error-message">{error}</div>}

      {featureFlags.length === 0 ? (
        <div className="no-feature-flags">
          <p>No feature flags found.</p>
        </div>
      ) : (
        <table className="key-value-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Label</th>
              <th>Tags</th>
              <th>Enabled</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {featureFlags.map((flag) => {
              const flagName = getFeatureFlagName(flag.key);
              const enabled = parseFeatureFlagEnabled(flag.value || '');
              
              return (
                <tr key={`${flag.key}-${flag.label || 'null'}`}>
                  <td className="flag-name">{flagName}</td>
                  <td>{flag.label || '<null>'}</td>
                  <td>
                    {renderTags(flag.tags)}
                  </td>
                  <td>
                    <label className="toggle-switch">
                      <input
                        type="checkbox"
                        checked={enabled}
                        onChange={() => handleToggleEnabled(flag)}
                      />
                      <span className="toggle-slider"></span>
                    </label>
                  </td>
                  <td>
                    <button 
                      onClick={() => onEdit(flag)} 
                      className="edit-button"
                    >
                      Edit
                    </button>
                    <button 
                      onClick={() => handleDelete(flag)}
                      className="delete-button"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}
