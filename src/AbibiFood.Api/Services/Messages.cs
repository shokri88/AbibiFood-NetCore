namespace AbibiFood.Api.Services;

public sealed class Messages(IConfiguration configuration)
{
    public string this[string key] => configuration[$"Messages:{key}"] ?? key;
}
