using Example;
using Hexed;
using Hexed.AspNetCore;
using Hexed.AspNetCore.OpenApi;

return await new Application().RunAsync();

public sealed class Application :
    Use<OpenApiModule>,
    Glob<HelloWorld>
;
