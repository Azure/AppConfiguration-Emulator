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
export declare const KeyValueMapper: Mapper;
export declare const KeyValueRevisionMapper: Mapper;
export declare const KeyValueRequestMapper: Mapper;
export {};
