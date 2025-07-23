export interface FeatureFlagValidationResult {
  isValid: boolean;
  error?: string;
  parsedJson?: any;
}

export const validateFeatureFlagJson = (
  jsonString: string, 
  mode?: 'create' | 'edit', 
  currentName?: string
): FeatureFlagValidationResult => {
  try {
    const parsed = JSON.parse(jsonString);
    
    // Check for required fields
    if (!parsed.id) {
      return {
        isValid: false,
        error: 'Field "id" is required in feature flag JSON'
      };
    }
    
    if (parsed.enabled === undefined || parsed.enabled === null) {
      return {
        isValid: false,
        error: 'Field "enabled" is required in feature flag JSON'
      };
    }
    
    // When editing an existing feature flag, prevent changing the id
    if (mode === 'edit' && currentName && parsed.id !== currentName) {
      return {
        isValid: false,
        error: 'Cannot change the feature flag ID when editing an existing feature flag'
      };
    }
    
    return {
      isValid: true,
      parsedJson: parsed
    };
  } catch (err) {
    if (err instanceof SyntaxError) {
      return {
        isValid: false,
        error: 'Invalid JSON syntax'
      };
    }
    
    return {
      isValid: false,
      error: 'Invalid JSON format'
    };
  }
};
