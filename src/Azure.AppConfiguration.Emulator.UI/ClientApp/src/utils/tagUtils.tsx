import React from 'react';

export const renderTags = (tags: Record<string, string> | undefined, maxDisplayTags: number = 3): React.ReactElement => {
  if (!tags || Object.keys(tags).length === 0) {
    return <span className="no-tags">No tags</span>;
  }

  const tagEntries = Object.entries(tags);
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