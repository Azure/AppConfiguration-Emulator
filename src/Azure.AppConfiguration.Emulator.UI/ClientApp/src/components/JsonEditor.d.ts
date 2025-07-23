interface Props {
    isOpen: boolean;
    jsonValue: string;
    onSave: (jsonValue: string) => void;
    onClose: () => void;
    mode?: 'create' | 'edit';
    currentName?: string;
}
export default function JsonEditor({ isOpen, jsonValue, onSave, onClose, mode, currentName }: Props): import("react/jsx-runtime").JSX.Element;
export {};
