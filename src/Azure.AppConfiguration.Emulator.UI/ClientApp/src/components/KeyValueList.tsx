import { useState, useEffect } from 'react';
import { keyValueService } from '../services/keyValueService';
import { KeyValue } from '../models/keyValue';

interface Props {
  onEdit: (keyValue: KeyValue) => void;
  onViewRevisions: (keyValue: KeyValue) => void;
}

export default function KeyValueList({ onEdit, onViewRevisions }: Props) {
  const [keyValues, setKeyValues] = useState<KeyValue[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [keyFilter, setKeyFilter] = useState('');
  const [labelFilter, setLabelFilter] = useState('');
  const [nextLink, setNextLink] = useState<string | null>(null);

  const fetchKeyValues = async (reset: boolean = true) => {
    if (reset) {
      setLoading(true);
      setKeyValues([]);
      setNextLink(null);
    } else {
      setLoadingMore(true);
    }
    
    try {
      const response = await keyValueService.getKeyValues(
        keyFilter || undefined, 
        labelFilter || undefined,
        reset ? undefined : nextLink || undefined
      );
      
      if (reset) {
        setKeyValues(response.items);
      } else {
        setKeyValues(prev => [...prev, ...response.items]);
      }
      
      setNextLink(response['@nextLink'] || null);
      setError(null);
    } catch (err) {
      setError('Failed to load key values');
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  };

  const loadMore = () => {
    if (nextLink && !loadingMore) {
      fetchKeyValues(false);
    }
  };

  useEffect(() => {
    fetchKeyValues(true);
  }, []);

  const handleDelete = async (kv: KeyValue) => {
    if (window.confirm('Are you sure you want to delete this key value?')) {
      const success = await keyValueService.deleteKeyValue(kv);
      if (success) {
        fetchKeyValues(true);
      }
    }
  };

  const handleApplyFilters = () => {
    fetchKeyValues(true);
  };

  const renderTags = (tags: Record<string, string> | undefined) => {
    if (!tags || Object.keys(tags).length === 0) {
      return <span className="no-tags">No tags</span>;
    }

    const tagEntries = Object.entries(tags);
    const maxDisplayTags = 3; // Show max 3 tags initially
    const visibleTags = tagEntries.slice(0, maxDisplayTags);
    const remainingCount = tagEntries.length - maxDisplayTags;

    return (
      <div className="tags-display" title={tagEntries.map(([k, v]) => `${k}: ${v}`).join(', ')}>
        {visibleTags.map(([k, v]) => (
          <span key={k} className="tag-display">
            {k}: {v}
          </span>
        ))}
        {remainingCount > 0 && (
          <span className="tag-display more-tags">
            +{remainingCount} more
          </span>
        )}
      </div>
    );
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
        
        <button onClick={handleApplyFilters} className="filter-button">
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
              <th>Tags</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {keyValues.length === 0 ? (
              <tr>
                <td colSpan={6}>No key values found</td>
              </tr>
            ) : (
              keyValues.map((kv) => (
                <tr key={`${kv.key}-${kv.label || 'null'}`}>
                  <td>{kv.key}</td>
                  <td>{kv.label || '<null>'}</td>
                  <td className="value-cell" title={kv.value || ''}>
                    {kv.value && kv.value.length > 50 
                      ? `${kv.value.substring(0, 50)}...` 
                      : kv.value || ''
                    }
                  </td>
                  <td className="content-type-cell" title={kv.contentType || ''}>
                    {kv.contentType && kv.contentType.length > 30 
                      ? `${kv.contentType.substring(0, 30)}...` 
                      : kv.contentType || ''
                    }
                  </td>
                  <td>
                    {renderTags(kv.tags)}
                  </td>
                  <td>
                    <button onClick={() => onEdit(kv)} className="edit-button">
                      Edit
                    </button>
                    <button 
                      onClick={() => handleDelete(kv)}
                      className="delete-button"
                    >
                      Delete
                    </button>
                    <button 
                      onClick={() => onViewRevisions(kv)}
                      className="view-revisions-button"
                    >
                      View Revisions
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      )}
      
      {nextLink && !loading && (
        <div className="load-more-container">
          <button 
            onClick={loadMore} 
            disabled={loadingMore} 
            className="load-more-button"
          >
            {loadingMore ? 'Loading...' : 'Load More'}
          </button>
        </div>
      )}
    </div>
  );
}
