import React from 'react';
import { Routes, Route, Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import KeyValueList from './KeyValueList';
import { KeyValue } from '../models/keyValue';
import KeyValueEditor from './KeyValueEditor';
import KeyValueRevisions from './KeyValueRevisions';
import FeatureFlagList from './FeatureFlagList';
import FeatureFlagEditor from './FeatureFlagEditor';

export default function App() {
  const [selectedKeyValue, setSelectedKeyValue] = useState<KeyValue | null>(null);
  const navigate = useNavigate();

  //  
  // Handler for editing a key value
  const handleKeyValueEdit = (keyValue: KeyValue) => {
    setSelectedKeyValue(keyValue);

    //
    // Navigate to the edit route
    const url = `/ui/edit/${encodeURIComponent(keyValue.key)}${keyValue.label ? `?label=${encodeURIComponent(keyValue.label)}` : ''}`;
    navigate(url);
  };

  // 
  // Handler for viewing revisions of a key value
  const handleViewRevisions = (keyValue: KeyValue) => {
    const url = `/ui/revisions/${encodeURIComponent(keyValue.key)}${keyValue.label ? `?label=${encodeURIComponent(keyValue.label)}` : ''}`;
    navigate(url);
  };

  // 
  // Handler for editing a feature flag
  const handleFeatureFlagEdit = (keyValue: KeyValue) => {
    setSelectedKeyValue(keyValue);
    const url = `/ui/featureflags/edit/${encodeURIComponent(keyValue.key)}${keyValue.label ? `?label=${encodeURIComponent(keyValue.label)}` : ''}`;
    navigate(url);
  };

  // 
  // Handler for creating a feature flag
  const handleFeatureFlagCreate = () => {
    navigate('/ui/featureflags/create');
  };

  // 
  // Handler for going back to the list view
  const handleBack = () => {
    navigate('/');
  };

  // 
  // Handler for going back to the feature flag list
  const handleFeatureFlagBack = () => {
    navigate('/ui/featureflags');
  };

  return (
      <div className="app-container">
        <header>
          <h1>Azure App Configuration Emulator</h1>
          <nav>
            <Link to="/ui/create">Create</Link>
            <Link to="/">Configuration explorer</Link>
            <Link to="/ui/featureflags">Feature management</Link>
          </nav>
        </header>
        
        <main>
          <Routes>
            <Route path="/" element={<KeyValueList onEdit={handleKeyValueEdit} onViewRevisions={handleViewRevisions} />} />
            <Route path="/ui/create" element={<KeyValueEditor mode="create" onBack={handleBack}  />} />
            <Route path="/ui/edit/:key" element={<KeyValueEditor mode="edit" keyValue={selectedKeyValue} onBack={handleBack}  />} />
            <Route path="/ui/revisions/:key" element={<KeyValueRevisions />} />
            <Route path="/ui/featureflags" element={<FeatureFlagList onCreate={handleFeatureFlagCreate} onEdit={handleFeatureFlagEdit} />} />
            <Route path="/ui/featureflags/create" element={<FeatureFlagEditor mode="create" onBack={handleFeatureFlagBack} />} />
            <Route path="/ui/featureflags/edit/:key" element={<FeatureFlagEditor mode="edit" keyValue={selectedKeyValue} onBack={handleFeatureFlagBack} />} />
          </Routes>
        </main>
      </div>
  );
}