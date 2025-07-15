import React from 'react';
import { Routes, Route, Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import KeyValueList from './KeyValueList';
import { KeyValue } from '../models/keyValue';
import KeyValueEditor from './KeyValueEditor';
import KeyValueRevisions from './KeyValueRevisions';

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
  // Handler for going back to the list view
  const handleBack = () => {
    navigate('/');
  };

  return (
      <div className="app-container">
        <header>
          <h1>Azure App Configuration Emulator</h1>
          <nav>
            <Link to="/ui/create">Create</Link>
            <Link to="/">Configuration explorer</Link>
          </nav>
        </header>
        
        <main>
          <Routes>
            <Route path="/" element={<KeyValueList onEdit={handleKeyValueEdit} onViewRevisions={handleViewRevisions} />} />
            <Route path="/ui/create" element={<KeyValueEditor mode="create" onBack={handleBack}  />} />
            <Route path="/ui/edit/:key" element={<KeyValueEditor mode="edit" keyValue={selectedKeyValue} onBack={handleBack}  />} />
            <Route path="/ui/revisions/:key" element={<KeyValueRevisions />} />
          </Routes>
        </main>
      </div>
  );
}