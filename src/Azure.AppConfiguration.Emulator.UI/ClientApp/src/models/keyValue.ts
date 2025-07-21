export interface KeyValue {
  key: string;
  label?: string;
  value?: string;
  contentType?: string;
  etag?: string;
  lastModified?: Date;
  locked?: boolean;
  tags?: Record<string, string>;
}

export interface KeyValueRevision {
  key: string;
  label?: string;
  value?: string;
  contentType?: string;
  etag?: string;
  lastModified?: Date;
  locked?: boolean;
  tags?: Record<string, string>;
}

export interface KeyValueRequest {
  value?: string;
  contentType?: string;
  tags?: Record<string, string>;
}