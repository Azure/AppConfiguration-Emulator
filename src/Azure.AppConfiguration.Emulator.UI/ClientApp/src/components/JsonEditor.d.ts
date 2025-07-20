interface Props {
    isOpen: boolean;
    jsonValue: string;
    onSave: (jsonValue: string) => void;
    onClose: () => void;
}
export default function JsonEditor({ isOpen, jsonValue, onSave, onClose }: Props): import("react/jsx-runtime").JSX.Element;
export {};
