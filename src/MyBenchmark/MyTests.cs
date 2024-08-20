using BenchmarkDotNet.Attributes;
using System.Dynamic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;

namespace MyBenchmark;

[RankColumn]
[MemoryDiagnoser]
public class MyTests
{
    private const int Timeout = 60;
    private const string GetSchemaEndpoint = "http://localhost:5021/schema";
    private const string PostDataEndpoint = "http://localhost:5021/data?count=10000";

    [Benchmark]
    public async Task Deserialize_ExpandoObject()
    {
        var http = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(Timeout)
        };
        await GetSchema(http);
        var request = new HttpRequestMessage(HttpMethod.Post, PostDataEndpoint);

        var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        var content = await response.Content.ReadAsStreamAsync();

        var deserialized = await JsonSerializer.DeserializeAsync<IEnumerable<ExpandoObject>>(content);
    }


    [Benchmark]
    public async Task Deserialize_Dictionary()
    {
        var http = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(Timeout)
        };
        await GetSchema(http);
        var request = new HttpRequestMessage(HttpMethod.Post, PostDataEndpoint);

        var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        var content = await response.Content.ReadAsStreamAsync();

        var deserialized = await JsonSerializer.DeserializeAsync<IEnumerable<IDictionary<string, object>>>(content);
    }

    [Benchmark]
    public async Task Deserialize_Typed()
    {
        var http = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(Timeout)
        };
        await GetSchema(http);

        var request = new HttpRequestMessage(HttpMethod.Post, PostDataEndpoint);

        var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        var content = await response.Content.ReadAsStreamAsync();

        var deserialized = await JsonSerializer.DeserializeAsync<IEnumerable<Funcionario>>(content);
    }

    [Benchmark]
    public async Task Deserialize_DynamicTyped()
    {
        var http = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(Timeout)
        };
        var schema = await GetSchema(http);

        var type = BuildType(schema);

        var request = new HttpRequestMessage(HttpMethod.Post, PostDataEndpoint);

        var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        var content = await response.Content.ReadAsStreamAsync();

        var enumerableType = typeof(IEnumerable<>).MakeGenericType(type);
        var deserialized = await JsonSerializer.DeserializeAsync(content, enumerableType);
    }

    private static Type BuildType(Schema schema)
    {
        var dynamicAssembly = new AssemblyName("DynamicAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(dynamicAssembly, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(dynamicAssembly.Name ?? "DynamicAssemblyExample");
        var typeBuilder = moduleBuilder.DefineType(schema.Name, TypeAttributes.Public);

        foreach (var prop in schema.Properties)
        {
            var type = prop.Type switch
            {
                PropertyType.String => typeof(string),
                PropertyType.Number => typeof(double),
                PropertyType.Bool => typeof(bool),
                PropertyType.Date => typeof(DateTime),
                PropertyType.Array => BuildType(moduleBuilder, schema.Name, prop),

                _ => throw new NotImplementedException()
            };

            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + prop.Name, type, FieldAttributes.Private);

            var propertyBuilder = typeBuilder.DefineProperty(prop.Name, PropertyAttributes.HasDefault, type, Type.EmptyTypes);

            MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            MethodBuilder methodBuilderGetAccessor = typeBuilder.DefineMethod("get_" + prop.Name, getSetAttr, type, Type.EmptyTypes);

            ILGenerator numberGetIL = methodBuilderGetAccessor.GetILGenerator();
            numberGetIL.Emit(OpCodes.Ldarg_0);
            numberGetIL.Emit(OpCodes.Ldfld, fieldBuilder);
            numberGetIL.Emit(OpCodes.Ret);

            MethodBuilder methodBuilderSetAccessor = typeBuilder.DefineMethod("set_" + prop.Name, getSetAttr, null, new Type[] { type });

            ILGenerator numberSetIL = methodBuilderSetAccessor.GetILGenerator();
            numberSetIL.Emit(OpCodes.Ldarg_0);
            numberSetIL.Emit(OpCodes.Ldarg_1);
            numberSetIL.Emit(OpCodes.Stfld, fieldBuilder);
            numberSetIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(methodBuilderGetAccessor);
            propertyBuilder.SetSetMethod(methodBuilderSetAccessor);
        }

        return typeBuilder.CreateType();
    }

    private static Type BuildType(ModuleBuilder moduleBuilder, string name, SchemaProperty prop)
    {
        var className = name + "_" + prop.Name;
        var typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public);
        foreach (var p in prop.Properties)
        {
            var type = p.Type switch
            {
                PropertyType.String => typeof(string),
                PropertyType.Number => typeof(double),
                PropertyType.Bool => typeof(bool),
                PropertyType.Date => typeof(DateTime),
                PropertyType.Array => BuildType(moduleBuilder, className, p),

                _ => throw new NotImplementedException()
            };

            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + p.Name, type, FieldAttributes.Private);

            var propertyBuilder = typeBuilder.DefineProperty(p.Name, PropertyAttributes.HasDefault, type, Type.EmptyTypes);

            MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            MethodBuilder methodBuilderGetAccessor = typeBuilder.DefineMethod("get_" + p.Name, getSetAttr, type, Type.EmptyTypes);

            ILGenerator numberGetIL = methodBuilderGetAccessor.GetILGenerator();
            numberGetIL.Emit(OpCodes.Ldarg_0);
            numberGetIL.Emit(OpCodes.Ldfld, fieldBuilder);
            numberGetIL.Emit(OpCodes.Ret);

            MethodBuilder methodBuilderSetAccessor = typeBuilder.DefineMethod("set_" + p.Name, getSetAttr, null, new Type[] { type });

            ILGenerator numberSetIL = methodBuilderSetAccessor.GetILGenerator();
            numberSetIL.Emit(OpCodes.Ldarg_0);
            numberSetIL.Emit(OpCodes.Ldarg_1);
            numberSetIL.Emit(OpCodes.Stfld, fieldBuilder);
            numberSetIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(methodBuilderGetAccessor);
            propertyBuilder.SetSetMethod(methodBuilderSetAccessor);
        }

        var createdType = typeBuilder.CreateType();
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(createdType);

        return enumerableType;
    }

    private static async Task<Schema> GetSchema(HttpClient http)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, GetSchemaEndpoint);
        var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        var content = await response.Content.ReadAsStreamAsync();

        return (await JsonSerializer.DeserializeAsync<Schema>(content))!;
    }
}

