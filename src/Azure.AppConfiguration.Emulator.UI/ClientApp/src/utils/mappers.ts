type MapperTypeName = "String" | "Boolean" | "Date" | "Object";

interface PropertyMapper {
  serializedName: string;
  type: {
    name: MapperTypeName;
  };
}

export interface Mapper {
    modelName: string;
    modelProperties: Record<string, PropertyMapper>;
}

export const KeyValueMapper: Mapper = {
  modelName: "KeyValue",
  modelProperties: {
    key: {
      serializedName: "key",
      type: { name: "String" }
    },
    label: {
      serializedName: "label",
      type: { name: "String" }
    },
    value: {
      serializedName: "value",
      type: { name: "String" }
    },
    contentType: {
      serializedName: "content_type",
      type: { name: "String" }
    },
    etag: {
      serializedName: "etag",
      type: { name: "String" }
    },
    lastModified: {
      serializedName: "last_modified",
      type: { name: "Date" }
    },
    locked: {
      serializedName: "locked",
      type: { name: "Boolean" }
    },
    tags: {
      serializedName: "tags",
      type: { name: "Object" }
    }
  }
};

export const KeyValueRevisionMapper: Mapper = {
  modelName: "KeyValueRevision", 
  modelProperties: {
    key: {
      serializedName: "key",
      type: { name: "String" }
    },
    label: {
      serializedName: "label", 
      type: { name: "String" }
    },
    value: {
      serializedName: "value",
      type: { name: "String" }
    },
    contentType: {
      serializedName: "content_type",
      type: { name: "String" }
    },
    etag: {
      serializedName: "etag",
      type: { name: "String" }
    },
    lastModified: {
      serializedName: "last_modified",
      type: { name: "Date" }
    },
    locked: {
      serializedName: "locked",
      type: { name: "Boolean" }
    },
    tags: {
      serializedName: "tags",
      type: { name: "Object" }
    }
  }
};

export const KeyValueRequestMapper: Mapper = {
  modelName: "KeyValueRequest",
  modelProperties: {
    value: {
      serializedName: "value",
      type: { name: "String" }
    },
    contentType: {
      serializedName: "content_type", 
      type: { name: "String" }
    },
    tags: {
      serializedName: "tags",
      type: { name: "Object" }
    }
  }
};