import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState, useEffect } from 'react';
import { keyValueService } from '../services/keyValueService';
export default function KeyValueEditor({ mode, keyValue, onBack }) {
    const [key, setKey] = useState('');
    const [label, setLabel] = useState('');
    const [value, setValue] = useState('');
    const [contentType, setContentType] = useState('');
    const [tags, setTags] = useState({});
    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState(null);
    // For tag editing
    const [tagKey, setTagKey] = useState('');
    const [tagValue, setTagValue] = useState('');
    useEffect(() => {
        if (mode === 'edit' && keyValue) {
            setKey(keyValue.key);
            setLabel(keyValue.label || '');
            // Fetch the full key value to get all properties
            const fetchFullKeyValue = async () => {
                setLoading(true);
                const fullKeyValue = await keyValueService.getKeyValue(keyValue.key, keyValue.label);
                if (fullKeyValue) {
                    setValue(fullKeyValue.value || '');
                    setContentType(fullKeyValue.contentType || '');
                    setTags(fullKeyValue.tags || {});
                }
                setLoading(false);
            };
            fetchFullKeyValue();
        }
        else if (mode === 'create') {
            // Reset form for create mode
            setKey('');
            setLabel('');
            setValue('');
            setContentType('');
            setTags({});
        }
    }, [mode, keyValue]);
    const handleAddTag = () => {
        if (!tagKey.trim())
            return;
        setTags(prev => ({
            ...prev,
            [tagKey]: tagValue
        }));
        setTagKey('');
        setTagValue('');
    };
    const handleRemoveTag = (key) => {
        setTags(prev => {
            const newTags = { ...prev };
            delete newTags[key];
            return newTags;
        });
    };
    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!key) {
            setError('Key is required');
            return;
        }
        setSaving(true);
        const request = {
            value,
            contentType: contentType || undefined,
            tags: Object.keys(tags).length > 0 ? tags : undefined
        };
        try {
            await keyValueService.createOrUpdateKeyValue(key, request, label || undefined);
            onBack();
        }
        catch (err) {
            setError('Failed to save key value');
        }
        finally {
            setSaving(false);
        }
    };
    if (loading) {
        return _jsx("div", { children: "Loading..." });
    }
    return (_jsxs("div", { className: "key-value-form-container", children: [_jsx("h2", { children: mode === 'create' ? 'Create New Key Value' : 'Edit Key Value' }), error && _jsx("div", { className: "error-message", children: error }), _jsxs("form", { onSubmit: handleSubmit, className: "key-value-form", children: [_jsxs("div", { className: "form-group", children: [_jsx("label", { htmlFor: "key", children: "Key:" }), _jsx("input", { id: "key", type: "text", value: key, onChange: (e) => setKey(e.target.value), disabled: mode === 'edit', required: true })] }), _jsxs("div", { className: "form-group", children: [_jsx("label", { htmlFor: "label", children: "Label:" }), _jsx("input", { id: "label", type: "text", value: label, onChange: (e) => setLabel(e.target.value), disabled: mode === 'edit' })] }), _jsxs("div", { className: "form-group", children: [_jsx("label", { htmlFor: "value", children: "Value:" }), _jsx("textarea", { id: "value", value: value, onChange: (e) => setValue(e.target.value), rows: 4 })] }), _jsxs("div", { className: "form-group", children: [_jsx("label", { htmlFor: "contentType", children: "Content Type:" }), _jsx("input", { id: "contentType", type: "text", value: contentType, onChange: (e) => setContentType(e.target.value) })] }), _jsxs("div", { className: "form-group", children: [_jsx("label", { children: "Tags:" }), _jsx("div", { className: "tags-container", children: Object.entries(tags).map(([k, v]) => (_jsxs("div", { className: "tag", children: [_jsxs("span", { children: [k, ": ", v] }), _jsx("button", { type: "button", onClick: () => handleRemoveTag(k), className: "remove-tag", children: "\u00D7" })] }, k))) }), _jsxs("div", { className: "add-tag", children: [_jsx("input", { type: "text", placeholder: "Tag Key", value: tagKey, onChange: (e) => setTagKey(e.target.value) }), _jsx("input", { type: "text", placeholder: "Tag Value", value: tagValue, onChange: (e) => setTagValue(e.target.value) }), _jsx("button", { type: "button", onClick: handleAddTag, className: "add-tag-button", children: "Add Tag" })] })] }), _jsxs("div", { className: "form-actions", children: [_jsx("button", { type: "button", onClick: onBack, className: "cancel-button", children: "Cancel" }), _jsx("button", { type: "submit", disabled: saving, className: "save-button", children: saving ? 'Saving...' : 'Save' })] })] })] }));
}
