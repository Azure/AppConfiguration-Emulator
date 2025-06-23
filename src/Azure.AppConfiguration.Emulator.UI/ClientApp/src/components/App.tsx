import React from 'react';
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { useState } from 'react';
import KeyValueList from './KeyValueList';
import { KeyValue } from '../models/keyValue';
import KeyValueEditor from './KeyValueEditor';

export default function App() {
  const [selectedKeyValue, setSelectedKeyValue] = useState<KeyValue | null>(null);

  // Handler for editing a key value
  const handleKeyValueEdit = (keyValue: KeyValue) => {
    setSelectedKeyValue(keyValue);

    //
    // Navigate to the edit route
    window.location.href = `/ui/edit/${encodeURIComponent(keyValue.key)}${keyValue.label ? `?label=${encodeURIComponent(keyValue.label)}` : ''}`;
  };

// Handler for going back to the list view
  const handleBack = () => {
    window.location.href = '/';
  };  

  return (
    <BrowserRouter>
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
            <Route path="/" element={<KeyValueList onEdit={handleKeyValueEdit} />} />
            <Route path="/ui/create" element={<KeyValueEditor mode="create" onBack={handleBack}  />} />
            <Route path="/ui/edit/:key" element={<KeyValueEditor mode="edit" onBack={handleBack}  />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}