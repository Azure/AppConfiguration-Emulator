import { KeyValue } from '../models/keyValue';
interface Props {
    onCreate: () => void;
    onEdit: (keyValue: KeyValue) => void;
}
export default function FeatureFlagList({ onCreate, onEdit }: Props): import("react/jsx-runtime").JSX.Element;
export {};
