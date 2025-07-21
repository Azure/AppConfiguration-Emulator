export interface FeatureFlagValidationResult {
    isValid: boolean;
    error?: string;
    parsedJson?: any;
}
export declare const validateFeatureFlagJson: (jsonString: string, mode?: "create" | "edit", currentName?: string) => FeatureFlagValidationResult;
