import { KeyValue, KeyValueRequest } from '../models/keyValue';
export declare const keyValueService: {
    getKeyValues: (keyFilter?: string, labelFilter?: string) => Promise<KeyValue[]>;
    getKeyValue: (key: string, label?: string) => Promise<KeyValue | null>;
    createOrUpdateKeyValue: (key: string, request: KeyValueRequest, label?: string) => Promise<KeyValue | null>;
    deleteKeyValue: (kv: KeyValue) => Promise<boolean>;
    getKeys: (nameFilter?: string) => Promise<string[]>;
};
