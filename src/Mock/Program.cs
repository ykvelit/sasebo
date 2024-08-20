using Bogus;
using Microsoft.AspNetCore.Mvc;
using MyBenchmark;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/schema", () =>
{
    return Results.Ok(new Schema
    {
        Name = "BusinessObjectName",
        Properties = [
            new SchemaProperty()
            {
               Name = "id",
               Type = PropertyType.Number,
            },
            new SchemaProperty()
            {
                Name = "nome",
                Type = PropertyType.String,
            },
            new SchemaProperty()
            {
                Name = "sobrenome",
                Type = PropertyType.String,
            },
            new SchemaProperty()
            {
                Name = "cargo",
                Type = PropertyType.String,
            },
            new SchemaProperty()
            {
                Name = "dataDeNascimento",
                Type = PropertyType.Date,
            },
            new SchemaProperty()
            {
                Name = "dependentes",
                Type = PropertyType.Array,
                Properties = [
                    new SchemaProperty()
                    {
                        Name = "id",
                        Type = PropertyType.String,
                    },
                    new SchemaProperty()
                    {
                        Name = "nome",
                        Type = PropertyType.String,
                    },
                ]
            }
        ]
    });
})
.WithName("GetSchema")
.WithOpenApi();

app.MapPost("/data", ([FromQuery] int count) =>
{
    Dependente GetDependente()
    {
        var faker = new Faker();

        return new Dependente
        {
            Id = faker.Random.Guid().ToString(),
            Nome = faker.Person.FullName
        };
    }

    Funcionario GetFuncionario()
    {
        var faker = new Faker();

        var dependentes = new List<Dependente>();

        for (int i = 0; i < 10; i++)
        {
            dependentes.Add(GetDependente());
        }

        return new Funcionario
        {
            Id = faker.Random.Int(),
            Nome = faker.Person.FirstName,
            Sobrenome = faker.Person.LastName,
            Cargo = faker.Person.Website,
            DataDeNascimento = faker.Person.DateOfBirth,
            Dependentes = dependentes
        };
    }

    async IAsyncEnumerable<Funcionario> GetData()
    {
        for (int i = 0; i < count; i++)
        {
            yield return GetFuncionario();
        }
    }

    return Results.Ok(GetData());
})
.WithName("GetData")
.WithOpenApi();

await app.RunAsync();