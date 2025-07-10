import { KeyValue, KeyValueRequest } from '../models/keyValue';
interface KeyValueResponse {
    items: KeyValue[];
    '@nextLink'?: string;
}
export declare const keyValueService: {
    getKeyValues: (keyFilter?: string, labelFilter?: string, nextLink?: string) => Promise<KeyValueResponse>;
    getKeyValue: (key: string, label?: string) => Promise<KeyValue | null>;
    createOrUpdateKeyValue: (key: string, request: KeyValueRequest, label?: string) => Promise<KeyValue | null>;
    deleteKeyValue: (kv: KeyValue) => Promise<boolean>;
    getKeys: (nameFilter?: string) => Promise<string[]>;
};
export {};
