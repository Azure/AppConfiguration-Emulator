import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState, useEffect } from 'react';
import { keyValueService } from '../services/keyValueService';
export default function KeyValueList({ onEdit }) {
    const [keyValues, setKeyValues] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [keyFilter, setKeyFilter] = useState('');
    const [labelFilter, setLabelFilter] = useState('');
    const fetchKeyValues = async () => {
        setLoading(true);
        try {
            const data = await keyValueService.getKeyValues(keyFilter || undefined, labelFilter || undefined);
            setKeyValues(data);
            setError(null);
        }
        catch (err) {
            setError('Failed to load key values');
        }
        finally {
            setLoading(false);
        }
    };
    useEffect(() => {
        fetchKeyValues();
    }, []);
    const handleDelete = async (key, label) => {
        if (window.confirm('Are you sure you want to delete this key value?')) {
            const success = await keyValueService.deleteKeyValue(key, label);
            if (success) {
                fetchKeyValues();
            }
        }
    };
    return (_jsxs("div", { className: "key-value-list", children: [_jsxs("div", { className: "filters", children: [_jsxs("div", { className: "filter-group", children: [_jsx("label", { children: "Key Filter:" }), _jsx("input", { type: "text", value: keyFilter, onChange: (e) => setKeyFilter(e.target.value) })] }), _jsxs("div", { className: "filter-group", children: [_jsx("label", { children: "Label Filter:" }), _jsx("input", { type: "text", value: labelFilter, onChange: (e) => setLabelFilter(e.target.value) })] }), _jsx("button", { onClick: fetchKeyValues, className: "filter-button", children: "Apply Filters" })] }), error && _jsx("div", { className: "error-message", children: error }), loading ? (_jsx("div", { children: "Loading..." })) : (_jsxs("table", { className: "key-value-table", children: [_jsx("thead", { children: _jsxs("tr", { children: [_jsx("th", { children: "Key" }), _jsx("th", { children: "Label" }), _jsx("th", { children: "Value" }), _jsx("th", { children: "Content Type" }), _jsx("th", { children: "Actions" })] }) }), _jsx("tbody", { children: keyValues.length === 0 ? (_jsx("tr", { children: _jsx("td", { colSpan: 5, children: "No key values found" }) })) : (keyValues.map((kv) => (_jsxs("tr", { children: [_jsx("td", { children: kv.key }), _jsx("td", { children: kv.label || '<null>' }), _jsx("td", { children: kv.value || '' }), _jsx("td", { children: kv.contentType || '' }), _jsxs("td", { children: [_jsx("button", { onClick: () => onEdit(kv), className: "edit-button", children: "Edit" }), _jsx("button", { onClick: () => handleDelete(kv.key, kv.label), className: "delete-button", children: "Delete" })] })] }, `${kv.key}-${kv.label || 'null'}`)))) })] }))] }));
}
