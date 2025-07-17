import { useState, useEffect } from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
import { keyValueService } from '../services/keyValueService';
import { KeyValueRevision } from '../models/keyValue';

export default function KeyValueRevisions() {
  const { key } = useParams<{ key: string }>();
  const [searchParams] = useSearchParams();
  const label = searchParams.get('label');
  
  const [revisions, setRevisions] = useState<KeyValueRevision[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchRevisions = async () => {
      if (!key) return;
      
      setLoading(true);
      try {
        const data = await keyValueService.getKeyValueRevisions(
          decodeURIComponent(key), 
          label || undefined
        );
        setRevisions(data);
        setError(null);
      } catch (err) {
        setError('Failed to load revisions');
      } finally {
        setLoading(false);
      }
    };

    fetchRevisions();
  }, [key, label]);

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  return (
    <div className="key-value-revisions">
      <div className="revisions-header">
        <h2>Revision History</h2>
        <div className="key-info-large">
          <strong>Key:</strong> {key && decodeURIComponent(key)} | <strong>Label:</strong> {label || '<null>'}
        </div>
      </div>

      {error && <div className="error-message">{error}</div>}

      {loading ? (
        <div>Loading revisions...</div>
      ) : (
        <div className="revisions-content">
          {revisions.length === 0 ? (
            <div className="no-revisions">No revisions found for this key value.</div>
          ) : (
            <table className="key-value-table">
              <thead>
                <tr>
                  <th>Value</th>
                  <th>Content Type</th>
                  <th>Last Modified</th>
                  <th>ETag</th>
                  <th>Tags</th>
                </tr>
              </thead>
              <tbody>
                {revisions.map((revision, index) => (
                  <tr key={revision.etag || index}>
                    <td>
                      {revision.value || '<empty>'}
                    </td>
                    <td>{revision.contentType || '<none>'}</td>
                    <td>{revision.lastModified?.toISOString() || '<unknown>'}</td>
                    <td>{revision.etag || '<none>'}</td>
                    <td>
                      {revision.tags && Object.keys(revision.tags).length > 0 ? (
                        <div className="tags-display">
                          {Object.entries(revision.tags).map(([tagKey, tagValue]) => (
                            <span key={tagKey} className="tag-display">
                              {tagKey}: {String(tagValue)}
                            </span>
                          ))}
                        </div>
                      ) : (
                        '<none>'
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  );
}
