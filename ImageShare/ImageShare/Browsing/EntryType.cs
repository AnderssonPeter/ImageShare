using System.Text.Json.Serialization;

namespace ImageShare.Browsing;

[JsonConverter(typeof(JsonStringEnumConverter<EntryType>))]
public enum EntryType
{
    Folder,
    File,
}
