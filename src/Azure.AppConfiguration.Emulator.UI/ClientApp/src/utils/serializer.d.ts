import { Mapper } from "./mappers";
export declare function deserialize<T>(mapper: Mapper, input: any): T;
export declare function serialize<T>(mapper: Mapper, input: T): any;
