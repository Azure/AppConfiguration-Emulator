import { KeyValue } from '../models/keyValue';
export declare const FEATURE_FLAG_PREFIX = ".appconfig.featureflag/";
export declare const FEATURE_FLAG_CONTENT_TYPE = "application/vnd.microsoft.appconfig.ff+json;charset=utf-8";
export declare function isFeatureFlag(keyValue: KeyValue): boolean;
export declare function getFeatureFlagName(key: string): string;
export declare function createFeatureFlagKey(name: string): string;
export declare function parseFeatureFlagEnabled(value: string): boolean;
export declare function createFeatureFlagValue(name: string, enabled: boolean): string;
