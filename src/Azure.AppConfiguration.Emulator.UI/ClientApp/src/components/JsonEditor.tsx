import { useState, useEffect } from 'react';
import { validateFeatureFlagJson } from '../utils/featureFlagValidation';

interface Props {
  isOpen: boolean;
  jsonValue: string;
  onSave: (jsonValue: string) => void;
  onClose: () => void;
  mode?: 'create' | 'edit';
  currentName?: string;
}

export default function JsonEditor({ isOpen, jsonValue, onSave, onClose, mode, currentName }: Props) {
  const [editedJson, setEditedJson] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isValid, setIsValid] = useState(true);

  useEffect(() => {
    if (isOpen) {
      try {
        // Pretty format the JSON for better readability
        const parsed = JSON.parse(jsonValue);
        const formatted = JSON.stringify(parsed, null, 2);
        setEditedJson(formatted);
        
        // Validate the initial JSON using shared validation
        const validation = validateFeatureFlagJson(jsonValue, mode, currentName);
        setIsValid(validation.isValid);
        setError(validation.error || null);
      } catch {
        setEditedJson(jsonValue);
        setError('Invalid JSON format');
        setIsValid(false);
      }
    }
  }, [isOpen, jsonValue, mode, currentName]);

  const handleJsonChange = (value: string) => {
    setEditedJson(value);
    
    // Use shared validation utility
    const validation = validateFeatureFlagJson(value, mode, currentName);
    setIsValid(validation.isValid);
    setError(validation.error || null);
  };

  const handleSave = () => {
    if (isValid) {
      try {
        // Validate and minify the JSON before saving
        const parsed = JSON.parse(editedJson);
        const minified = JSON.stringify(parsed);
        onSave(minified);
        onClose();
      } catch (err) {
        setError('Failed to parse JSON');
      }
    }
  };

  const handleCancel = () => {
    setError(null);
    onClose();
  };

  if (!isOpen) {
    return null;
  }

  return (
    <div className="json-editor-overlay">
      <div className="json-editor-modal">
        <div className="json-editor-header">
          <h3>Advanced Feature Flag Editor</h3>
          <button onClick={handleCancel} className="close-button">×</button>
        </div>
        
        <div className="json-editor-content">
          <p className="json-editor-description">
            Edit the feature flag configuration JSON directly. Required fields: <strong>id</strong> and <strong>enabled</strong>.
          </p>
          
          <div className={`validation-status ${isValid ? 'valid' : 'invalid'}`}>
            {isValid ? (
              <span className="validation-message valid">✓ JSON is valid</span>
            ) : (
              <span className="validation-message invalid">✗ {error || 'Invalid JSON'}</span>
            )}
          </div>
          
          <div className="json-editor-input-container">
            <textarea
              className={`json-editor-textarea ${!isValid ? 'invalid' : ''}`}
              value={editedJson}
              onChange={(e) => handleJsonChange(e.target.value)}
              rows={15}
            />
          </div>
        </div>
        
        <div className="json-editor-actions">
          <button onClick={handleCancel} className="cancel-button">
            Cancel
          </button>
          <button 
            onClick={handleSave} 
            className="save-button"
            disabled={!isValid}
          >
            Save
          </button>
        </div>
      </div>
    </div>
  );
}
