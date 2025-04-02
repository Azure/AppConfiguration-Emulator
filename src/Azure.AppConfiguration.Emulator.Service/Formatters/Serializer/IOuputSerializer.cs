using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service.Formatters.Serializer
{
    interface IOuputSerializer<T>
    {
        Task WriteContent(JsonWriter jw, T obj, long fields);

        void WriteResponseHeaders(HttpResponse response, T obj);
    }
}
