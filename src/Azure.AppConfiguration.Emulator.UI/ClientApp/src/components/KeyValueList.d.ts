import { KeyValue } from '../models/keyValue';
interface Props {
    onEdit: (keyValue: KeyValue) => void;
    onViewRevisions: (keyValue: KeyValue) => void;
}
export default function KeyValueList({ onEdit, onViewRevisions }: Props): import("react/jsx-runtime").JSX.Element;
export {};
