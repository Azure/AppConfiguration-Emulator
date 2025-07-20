import { Mapper } from "./mappers";

export function deserialize<T>(mapper: Mapper, input: any): T {
  const output: T = {} as T;
  for (const [propName, propDef] of Object.entries(mapper.modelProperties)) {
    const serializedKey = propDef.serializedName;
    if (input.hasOwnProperty(serializedKey)) {
      const value = input[serializedKey];
      switch (propDef.type.name) {
        case "Date":
          output[propName] = value ? new Date(value) : value;
          break;
        case "Boolean":
          output[propName] = Boolean(value);
          break;
        default:
          output[propName] = value;
          break;
      }
    }
  }
  return output;
}

export function serialize<T>(mapper: Mapper, input: T): any {
  const output: any = {};
  for (const [propName, propDef] of Object.entries(mapper.modelProperties)) {
    const inputValue = (input as any)[propName];
    if (inputValue !== undefined && inputValue !== null) {
      const serializedKey = propDef.serializedName;
      switch (propDef.type.name) {
        case "Date":
          output[serializedKey] = inputValue instanceof Date ? inputValue.toISOString() : inputValue;
          break;
        case "Boolean":
          output[serializedKey] = Boolean(inputValue);
          break;
        default:
          output[serializedKey] = inputValue;
          break;
      }
    }
  }
  return output;
}