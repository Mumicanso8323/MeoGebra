using System.IO;
using System.Text.Json;
using MeoGebra.Models;

namespace MeoGebra.Services;

public static class DocumentPersistence {
    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true
    };

    public static void Save(Document document, string path) {
        var json = JsonSerializer.Serialize(document, Options);
        File.WriteAllText(path, json);
    }

    public static Document Load(string path) {
        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<Document>(json, Options) ?? new Document();
        document.Symbols.Restore(document.Functions);
        return document;
    }
}
