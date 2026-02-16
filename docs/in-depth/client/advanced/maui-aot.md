# Native AOT Support for MAUI

When using [Native AOT with .NET MAUI](https://learn.microsoft.com/dotnet/maui/deployment/nativeaot), several requirements will need to be met to ensure the application works in release mode, explicitly on iOS and Mac Catalyst.

1. [Implement compiled models for Entity Framework Core](https://learn.microsoft.com/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#compiled-models).
2. [Implement source generation in System.Text.Json](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation).
3. [Enable the Interpreter for iOS and Mac Catalyst](https://learn.microsoft.com/dotnet/maui/macios/interpreter?view=net-maui-10.0#enable-the-interpreter).

The following are basic instructions on how to fulfill these requirements.  However, you should consult the official documentation (linked above) for each requirement.

!!! info
    Thanks to [Richard Perry](https://github.com/richard-einfinity) for providing the detailed instructions for enabling Native AOT support.

## Compiled models

To enable compiled models in your MAUI project:

1. Move your `DbContext` and models to a separate library that targets `net10.0`.
2. Create a dummy project with a project reference to the library.  This will act as a startup project for the EF Core Tools we will run later.  This can be as simple as a console application.
3. Implement an `IDesignTimeDbContextFactory` for your context.  This can be placed in your dummy project.  For example:

        internal class MyContextFactory : IDesignTimeDbContextFactory<MyContext>
        {
          public MyContext CreateDbContext(string[] args)
          {
            var optionsBuilder = new DbContextOptionsBuilder<MyContext>();
            optionsBuilder.UseSqlite("Data Source=:memory:");
            return new MyContext(optionsBuilder.Options, new HttpClientOptions()); 
          }
        }

4. Ensure you have the [EF Core Tools](https://learn.microsoft.com/ef/core/cli/dotnet) installed.
5. Run the following command (appropriately adjusted for your situation):

        Optimize-DbContext \
          -OutputDir Models \
          -Context MyContext \
          -StartupProject MyDummyProject \
          -Project MyLibraryProject \
          -Namespace MyLibraryProject.Models

   Type this command on a single line. This should generate the compiled models in the specified output directory.  We will be interested in the class named `MyContextModel` later.

6. In your `MauiProgram.cs` file, add the following statement as the first statement in the `CreateMauiApp()` method:

        AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", true);

   This addresses a deadlock issue in the compiled model initialization.

7. In your `DbContext` configuration, use the following statement:

        options.UseModel(MyContextModel.Instance);

   Your `DbContext` initialization should look something like this:

        mauiAppBuilder.Services.AddDbContext<MyContext>((sp, options) => 
        {
            options.UseSqlite(sp.GetRequiredService<SqliteConnection>());
            options.UseModel(MyContextModel.Instance);
        });

## Source generation for System.Text.Json

Create a new partial class that inherits from `JsonSerializerContext`.  It should look like this:

    [JsonSerializable(typeof(Page<TodoItem>))]
    [JsonSerializable(typeof(Page<Category>))]
    [JsonSourceGenerationOptions(
      PropertyNameCaseInsensitive = true,
      PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
      DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
      UseStringEnumConverter = true,
      WriteIndented = false,
      GenerationMode = JsonSourceGenerationMode.Metadata,
      AllowTrailingCommas = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
      Converters = [
        typeof(DateTimeConverter),
        typeof(DateTimeOffsetConverter),
        typeof(TimeOnlyConverter),
        typeof(SpatialGeoJsonConverter)
      ]
    )]
    public partial class MySerializerContext : JsonSerializerContext
    {
    }

Enumerations (enum) are transmitted as strings.  If you use an enum in any of your models, you will additionally have to add `typeof(JsonStringEnumConverter<TEnum>)` to the list of converters, where `TEnum` is the type of the enum you are using.  Repeat this for each enum you are using.  For example, let's say you have multiple models.  One of your models is using the enum `CategoryType` and another model is using `CustomerType`, your Converters section would look like this:

    Converters = [
      typeof(JsonStringEnumConverter<CategoryType>)
      typeof(JsonStringEnumConverter<CustomerType>)
      typeof(DateTimeConverter),
      typeof(DateTimeOffsetConverter),
      typeof(TimeOnlyConverter),
      typeof(SpatialGeoJsonConverter)
    ]

Add a `JsonSerializable` attribute for each entity that is stored in your `DbContext` similar to the ones in the example above.  Finally, install the modified `JsonSerializerContext` in the `MauiProgram.cs` as the second statment in the `CreateMauiApp` function:

    DatasyncSerializer.JsonSerializerOptions.TypeInfoResolver = MySerializerContext.Default;

## Enable the Interpreter

Add the following property to the iOS release configuration in your MAUI app project file:

    <MtouchInterpreter>all</MtouchInterpreter>

The property group should look something like the following:

    <PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Release|net10.0-ios|AnyCPU'">
      <OptimizePNGs>true</OptimizePNGs>
      <MtouchInterpreter>all</MtouchInterpreter>
    </PropertyGroup>

## Having problems?

Unfortunately, the development team does not have much experience with releasing with Native AOT, so is of limited help.  While you can [add a discussion](https://github.com/CommunityToolkit/Datasync/discussions), you will probably get more assistance in the [MAUI](https://github.com/dotnet/maui) and [Entity Framework Core](https://github.com/dotnet/efcore) projects.
