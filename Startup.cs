using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;
using TalkToApi.Database;
using TalkToApi.Helpers;
using TalkToApi.Helpers.Constants;
using TalkToApi.V1.Helpers.Swagger;
using TalkToApi.V1.Models;
using TalkToApi.V1.Repositories;
using TalkToApi.V1.Repositories.Contracts;

namespace TalkToApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            #region AutoMapper  - Configuração
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new DTOMapperProfile());
            });
         

            IMapper mapper = config.CreateMapper();
            services.AddSingleton(mapper);
            #endregion

            services.Configure<ApiBehaviorOptions>(op =>{

                op.SuppressModelStateInvalidFilter = true;
            });

            #region Reposotorios
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<ITokenRepository, TokenRepository>();
            services.AddScoped<IMensagemRepository, MensagemRepository>();
            #endregion



            #region  Add o banco de dados como serviço
            services.AddDbContext<TalkToContext>(cf_ =>{

                cf_.UseSqlite("Data Source=Database\\TalkTo.db");
            });
            #endregion

            #region   CORS

            services.AddCors( cfg => {

                /*
                    Origin: Domínio(sub) : api.site.com.br  != www.site.com.br != web.site.com.br != www.empresa.com.br
                    Dominio(Protocolo) : http//www.site.com.br  != https://www.site.com.br
                    Dominio(Porta) : http//www.site.com.br:80 != https://www.site.com.br:367
                 */
                cfg.AddDefaultPolicy(policy => {

                    policy.WithOrigins("https://localhost:44376", "http://localhost:44376")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowedToAllowWildcardSubdomains();   // habilitanto todos os sub-dominios
                    // .WithMethods("GET")
                    //.WithHeaders("Accept", "Autorization");
                });

                //Habilitar todos os sites, com restrição
                cfg.AddPolicy("AnyOrigin", policy=>{

                    policy.AllowAnyOrigin()
                    .WithMethods("GET")
                    .AllowAnyHeader();
                    
                });




            });


            #endregion

            services.AddMvc(cfg => {
                cfg.ReturnHttpNotAcceptable = true;
                cfg.InputFormatters.Add(new XmlSerializerInputFormatter(cfg));
                cfg.OutputFormatters.Add(new XmlSerializerOutputFormatter());

                var jsonOutPutFormater =cfg.OutputFormatters.OfType<JsonOutputFormatter>().FirstOrDefault();
                if(jsonOutPutFormater != null)
                {
                    jsonOutPutFormater.SupportedMediaTypes.Add(CustomMediaType.Hateoas);
                }

            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);



            services.AddApiVersioning(cfg => {
                cfg.ReportApiVersions = true;

                //cfg.ApiVersionReader = new HeaderApiVersionReader("api-version");
                cfg.AssumeDefaultVersionWhenUnspecified = true;  // parâmetro complementar ao ".DefaultApiVersion"
                cfg.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0); // versão default - padrão ou sugerida.

            });

            services.AddSwaggerGen(cfg => {
                cfg.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                {
                    In = "header",
                    Type = "apiKey",
                    Description = "Adicione o JSON Web Token(JWT) para autenticar.",
                    Name = "Authorization"
                });

                var security = new Dictionary<string, IEnumerable<string>>() {
                    { "Bearer", new string[] { } }
                };
                cfg.AddSecurityRequirement(security);

                cfg.ResolveConflictingActions(apiDescription => apiDescription.First());// para resolver conflitos de versões da API no Swagger, com mesmo nome.
                cfg.SwaggerDoc("v1.0", new Swashbuckle.AspNetCore.Swagger.Info()
                {
                    Title = "MinhasTarefas API - V1.0",
                    Version = "v1.0"
                });

                var CaminhoProjeto = PlatformServices.Default.Application.ApplicationBasePath;//recupera o caminho do projeto...
                var NomeProjeto = $"{PlatformServices.Default.Application.ApplicationName}.xml"; //recupera o nome do projeto...
                var CaminhoArquivoXMLComentario = Path.Combine(CaminhoProjeto, NomeProjeto);

                cfg.IncludeXmlComments(CaminhoArquivoXMLComentario);

                cfg.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var actionApiVersionModel = apiDesc.ActionDescriptor?.GetApiVersion();
                    // would mean this action is unversioned and should be included everywhere
                    if (actionApiVersionModel == null)
                    {
                        return true;
                    }
                    if (actionApiVersionModel.DeclaredApiVersions.Any())
                    {
                        return actionApiVersionModel.DeclaredApiVersions.Any(v => $"v{v.ToString()}" == docName);
                    }
                    return actionApiVersionModel.ImplementedApiVersions.Any(v => $"v{v.ToString()}" == docName);
                });

                cfg.OperationFilter<ApiVersionOperationFilter>();

            });

            services.AddIdentity<ApplicationUser, IdentityRole>(options => {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 5;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false; // são a-A-0-9
            })
                    .AddEntityFrameworkStores<TalkToContext>()
                    .AddDefaultTokenProviders();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("chave-api-jwt-minhas-tarefas"))
                };
            });

            services.AddAuthorization(auth => {
                auth.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
                                             .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                                             .RequireAuthenticatedUser()
                                             .Build()
                );
            });


            services.ConfigureApplicationCookie(options => {
                options.Events.OnRedirectToLogin = context => {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
            });
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePages();
            app.UseAuthentication();
            app.UseHttpsRedirection();
            //app.UseCors("AnyOrigin"); DESABILITE QUANDO FOR USAR O ATTRIBUTOS ENABLECORS /DISABLECORS
            app.UseMvc();

            app.UseSwagger(); // /swagger/v1/swagger.json
            app.UseSwaggerUI(cfg => {
                cfg.SwaggerEndpoint("/swagger/v1.0/swagger.json", "Talk TO  API - V1.0");
                cfg.RoutePrefix = String.Empty;
            });
        }
    }
}
