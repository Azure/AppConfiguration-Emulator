import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import { useState } from 'react';
import KeyValueList from './KeyValueList';
import KeyValueEditor from './KeyValueEditor';
export default function App() {
    const [selectedKeyValue, setSelectedKeyValue] = useState(null);
    // Handler for editing a key value
    const handleKeyValueEdit = (keyValue) => {
        setSelectedKeyValue(keyValue);
        //
        // Navigate to the edit route
        window.location.href = `/ui/edit/${encodeURIComponent(keyValue.key)}${keyValue.label ? `?label=${encodeURIComponent(keyValue.label)}` : ''}`;
    };
    // Handler for going back to the list view
    const handleBack = () => {
        window.location.href = '/';
    };
    return (_jsx(BrowserRouter, { children: _jsxs("div", { className: "app-container", children: [_jsxs("header", { children: [_jsx("h1", { children: "Azure App Configuration Emulator" }), _jsxs("nav", { children: [_jsx(Link, { to: "/ui/create", children: "Create" }), _jsx(Link, { to: "/", children: "Configuration explorer" })] })] }), _jsx("main", { children: _jsxs(Routes, { children: [_jsx(Route, { path: "/", element: _jsx(KeyValueList, { onEdit: handleKeyValueEdit }) }), _jsx(Route, { path: "/ui/create", element: _jsx(KeyValueEditor, { mode: "create", onBack: handleBack }) }), _jsx(Route, { path: "/ui/edit/:key", element: _jsx(KeyValueEditor, { mode: "edit", onBack: handleBack }) })] }) })] }) }));
}
