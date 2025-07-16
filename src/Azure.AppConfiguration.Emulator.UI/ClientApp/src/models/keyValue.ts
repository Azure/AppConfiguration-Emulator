export interface KeyValue {
  key: string;
  label?: string;
  value?: string;
  content_type?: string;
  etag?: string;
  lastModified?: string;
  locked?: boolean;
  tags?: Record<string, string>;
}

export interface KeyValueRevision {
  key: string;
  label?: string;
  value?: string;
  content_type?: string;
  etag?: string;
  last_modified?: string;
  locked?: boolean;
  tags?: Record<string, string>;
}

export interface KeyValueRequest {
  value?: string;
  content_type?: string;
  tags?: Record<string, string>;
}