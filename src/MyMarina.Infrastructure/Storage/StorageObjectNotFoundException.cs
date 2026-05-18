namespace MyMarina.Infrastructure.Storage;

public sealed class StorageObjectNotFoundException(string key)
    : Exception($"Storage object not found: {key}");
