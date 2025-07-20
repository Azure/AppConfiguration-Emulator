import { KeyValue } from '../models/keyValue';
interface Props {
    mode: 'create' | 'edit';
    keyValue?: KeyValue | null;
    onBack: () => void;
}
export default function FeatureFlagEditor({ mode, keyValue, onBack }: Props): import("react/jsx-runtime").JSX.Element;
export {};
