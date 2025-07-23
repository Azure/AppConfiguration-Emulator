import { KeyValue } from '../models/keyValue';

export const FEATURE_FLAG_PREFIX = '.appconfig.featureflag/';
export const FEATURE_FLAG_CONTENT_TYPE = 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8';

// Check if a key-value is a feature flag
export function isFeatureFlag(keyValue: KeyValue): boolean {
  return keyValue.key?.startsWith(FEATURE_FLAG_PREFIX) && 
         keyValue.contentType === FEATURE_FLAG_CONTENT_TYPE;
}

// Get feature flag name from key (remove prefix)
export function getFeatureFlagName(key: string): string {
  return key.startsWith(FEATURE_FLAG_PREFIX) ? key.substring(FEATURE_FLAG_PREFIX.length) : key;
}

// Create feature flag key (add prefix)
export function createFeatureFlagKey(name: string): string {
  return FEATURE_FLAG_PREFIX + name;
}

// Parse feature flag value to get enabled state
export function parseFeatureFlagEnabled(value: string): boolean {
  try {
    const parsed = JSON.parse(value);
    return parsed.enabled === true;
  } catch {
    return false;
  }
}

// Create feature flag value JSON
export function createFeatureFlagValue(name: string, enabled: boolean): string {
  return JSON.stringify({
    id: name,
    enabled: enabled
  });
}
